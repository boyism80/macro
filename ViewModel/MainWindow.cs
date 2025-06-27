using macro.Command;
using macro.Dialog;
using macro.Extension;
using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace macro.ViewModel
{
    public partial class MainWindow : INotifyPropertyChanged, IDisposable
    {
        #region Constants

        private const float DEFAULT_SPRITE_THRESHOLD = 0.8f;
        private const float DEFAULT_SPRITE_FACTOR = 1.0f;
        private const int TIMER_INTERVAL_MS = 100;
        private const int BITMAP_DPI = 96;

        #endregion

        #region Static Resources

        public static readonly SolidColorBrush INACTIVE_FRAME_BACKGROUND = new(Colors.White);
        public static readonly SolidColorBrush ACTIVE_FRAME_BACKGROUND = new(System.Windows.Media.Color.FromRgb(222, 222, 222));

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler Started;
        public event EventHandler Stopped;

        #endregion

        #region Fields

        private DateTime _startDateTime;
        private OptionDialog _optionDialog;
        public EditResourceDialog EditResourceDialog { get; set; }

        // Performance: Channel consumption for frame processing
        private CancellationTokenSource _frameConsumerCancellation;

        private Model.MainWindow _model;
        public Model.MainWindow Model
        {
            get => _model;
            set
            {
                _model = value;
                if (_model != null)
                {
                    Option = new Option(_model.Option);
                }
            }
        }

        private Model.App _target;

        private Option _option;
        public Option Option
        {
            get => _option;
            set
            {
                _option = value;
                Model.Option = value?.Model;

                if (value != null && _target != null)
                {
                    _target.Fps = value.RenderFrame;
                }
            }
        }

        public bool Running { get; private set; }
        public TimeSpan ElapsedTime
        {
            get
            {
                if (Running == false)
                    return TimeSpan.Zero;

                return DateTime.Now - _startDateTime;
            }
        }
        public string RunButtonText => Running ? "Stop" : "Run";
        public string RunStateText => Running ? "Running" : "Waiting";
        public string ExceptionText { get; set; } = string.Empty;
        public string Title => "macro";
        public string StatusName => string.Empty;
        public Visibility DarkBackgroundVisibility { get; private set; } = Visibility.Hidden;

        private readonly Stopwatch _elapsedStopwatch = new Stopwatch();
        private readonly ReaderWriterLockSlim _frameLock = new ReaderWriterLockSlim(); // Reader-Writer lock for Frame access
        private PooledMat _frame;
        public PooledMat Frame
        {
            get
            {
                _frameLock.EnterReadLock();
                try
                {
                    return _frame;
                }
                finally
                {
                    _frameLock.ExitReadLock();
                }
            }
            private set
            {
                _frameLock.EnterWriteLock();
                try
                {
                    // Dispose previous frame (auto-returns to pool if from pool)
                    if (_frame != null && !_frame.IsDisposed)
                    {
                        _frame.Dispose();
                    }
                    _frame = value;
                }
                finally
                {
                    _frameLock.ExitWriteLock();
                }
            }
        }
        public PooledMat StaticFrame { get; private set; }
        public WriteableBitmap Bitmap { get; private set; }

        public ObservableCollection<ViewModel.Sprite> Sprites
        {
            get => [.. Model.Sprites.Values.Select(x => new Sprite(x))];
            set => Model.Sprites = value.ToDictionary(x => x.Name, x => x.Model);
        }
        public ObservableCollection<Log> Logs { get; private set; } = new ObservableCollection<Log>();

        public SolidColorBrush FrameBackgroundBrush => Bitmap != null ? ACTIVE_FRAME_BACKGROUND : INACTIVE_FRAME_BACKGROUND;

        public ICommand SetMinimizeCommand { get; set; }
        public ICommand SetMaximizeCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand RunCommand { get; private set; }
        public ICommand OptionCommand { get; private set; }
        public ICommand EditSpriteCommand { get; private set; }
        public ICommand SetPictureCommand { get; private set; }
        public ICommand ResetScreenCommand { get; private set; }
        public ICommand SelectedRectCommand { get; private set; }

        public MainWindow(Model.MainWindow model)
        {
            Model = model;
            Sprites = [.. Model.Sprites.Values.Select(x => new Sprite(x))];

            RunCommand = new RelayCommand(OnStart);
            OptionCommand = new RelayCommand(OnOption);
            EditSpriteCommand = new RelayCommand(OnEditSprite);
            SetPictureCommand = new RelayCommand(OnSetPicture);
            ResetScreenCommand = new RelayCommand(OnResetScreen);
            SelectedRectCommand = new RelayCommand(OnSelectedRect);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TIMER_INTERVAL_MS)
            };
            timer.Tick += OnTimer;
            timer.Start();

            // Performance: Force frame rendering every frame to prevent WPF rendering optimization
            // that causes frame drops when mouse is not moving (fixes stuttering in Release mode)
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// Force WPF rendering every frame to maintain consistent frame rate
        /// Performance: Prevents WPF from optimizing rendering when mouse is idle
        /// </summary>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (Running && Bitmap != null)
            {
                // Force screen update every frame
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bitmap)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FrameBackgroundBrush)));
            }
        }

        /// <summary>
        /// Handles rectangle selection on the screen
        /// </summary>
        /// <param name="obj">Command parameter containing selection data</param>
        private void OnSelectedRect(object obj)
        {
            if (!TryParseSelectionParameters(obj, out var selectedRect, out var mouseButton))
                return;

            switch (mouseButton)
            {
                case MouseButton.Left:
                    CreateSpriteFromSelection(selectedRect);
                    break;

                case MouseButton.Right:
                    CopyAreaToClipboard(selectedRect);
                    break;
            }
        }

        /// <summary>
        /// Parses selection parameters from command argument
        /// </summary>
        /// <param name="obj">Command parameter</param>
        /// <param name="selectedRect">Parsed rectangle</param>
        /// <param name="mouseButton">Parsed mouse button</param>
        /// <returns>True if parsing successful</returns>
        private bool TryParseSelectionParameters(object obj, out System.Windows.Rect selectedRect, out MouseButton mouseButton)
        {
            selectedRect = default;
            mouseButton = default;

            if (obj is not object[] parameters || parameters.Length < 3)
                return false;

            try
            {
                selectedRect = (System.Windows.Rect)parameters[1];
                mouseButton = (MouseButton)parameters[2];
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new sprite from the selected area
        /// </summary>
        /// <param name="selectedRect">Selected rectangle area</param>
        private void CreateSpriteFromSelection(System.Windows.Rect selectedRect)
        {
            var rect = ConvertToOpenCvRect(selectedRect);
            using var frameClone = GetFrameClone();

            if (frameClone == null)
                return;

            // Create ROI and then clone it to make an independent copy
            using var selectedFrame = MatPool.GetClone(new Mat(frameClone, rect));

            var newSprite = CreateNewSprite(selectedFrame);
            var source = Sprites.Concat([newSprite]);

            ShowEditSpriteDialog(source, newSprite);
        }

        /// <summary>
        /// Creates a new sprite with default settings
        /// </summary>
        /// <param name="sourceFrame">Source frame for the sprite</param>
        /// <returns>New sprite instance</returns>
        private Sprite CreateNewSprite(PooledMat sourceFrame)
        {
            return new Sprite(new Model.Sprite
            {
                Name = "New Sprite",
                Source = sourceFrame,
                Threshold = DEFAULT_SPRITE_THRESHOLD,
                Extension = new Model.SpriteExtension
                {
                    Activated = false,
                    DetectColor = false,
                    Factor = DEFAULT_SPRITE_FACTOR,
                    Pivot = System.Drawing.Color.White
                }
            });
        }

        /// <summary>
        /// Copies area coordinates to clipboard as JSON
        /// </summary>
        /// <param name="selectedRect">Selected rectangle area</param>
        private void CopyAreaToClipboard(System.Windows.Rect selectedRect)
        {
            var area = new { x = (int)selectedRect.X, y = (int)selectedRect.Y, width = (int)selectedRect.Width, height = (int)selectedRect.Height };
            Clipboard.SetText(JsonConvert.SerializeObject(area));
            MessageBox.Show("Area saved to clipboard.");
        }

        /// <summary>
        /// Converts WPF Rect to OpenCV Rect
        /// </summary>
        /// <param name="wpfRect">WPF rectangle</param>
        /// <returns>OpenCV rectangle</returns>
        private OpenCvSharp.Rect ConvertToOpenCvRect(System.Windows.Rect wpfRect)
        {
            return new OpenCvSharp.Rect
            {
                X = (int)wpfRect.X,
                Y = (int)wpfRect.Y,
                Width = (int)wpfRect.Width,
                Height = (int)wpfRect.Height
            };
        }

        private void Run()
        {
            try
            {
                if (Running)
                    return;

                if (_target != null)
                {
                    _frameConsumerCancellation?.Cancel();
                }

                LoadPythonModules(Option.PythonDirectoryPath);
                _target = macro.Model.App.Find(Option.Class);
                if (_target == null)
                    throw new Exception($"cannot find {Option.Class}");

                // Performance: Set target FPS for frame capture
                _target.Fps = Option.RenderFrame;

                // Performance: Start frame consumer task
                _frameConsumerCancellation = new CancellationTokenSource();
                Task.Run(() => ConsumeFramesAsync(_frameConsumerCancellation.Token));

                _target.Start();
                _startDateTime = DateTime.Now;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElapsedTime)));
                ExceptionText = string.Empty;
                Running = true;

                Started?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Stop();
                ExceptionText = e.Message;
            }
        }

        private void Stop()
        {
            if (_target != null)
            {
                _target.Stop();
                _target = null;
            }

            // Performance: Cancel frame consumer task
            _frameConsumerCancellation?.Cancel();
            _frameConsumerCancellation = null;

            ReleasePythonModule();
            Bitmap = null;

            // Set Frame to null (thread-safe with ReaderWriterLockSlim)
            Frame = null;

            // Performance: Clear all Mat pools on application shutdown
            MatPool.Clear();

            Running = false;

            Stopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update WPF bitmap from OpenCV PooledMat with memory optimization
        /// Performance: Reuses existing WriteableBitmap when possible to avoid allocations
        /// </summary>
        private void UpdateBitmap(PooledMat frame)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Performance: Only create new WriteableBitmap if size changed
                if (Bitmap == null || Bitmap.Width != frame.Width || Bitmap.Height != frame.Height)
                    Bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);

                Bitmap.Lock();
                try
                {
                    // Direct memory copy from PooledMat to WriteableBitmap buffer
                    var bitmapMat = new Mat(frame.Rows, frame.Cols, MatType.CV_8UC3, Bitmap.BackBuffer);
                    frame.CopyTo(bitmapMat);
                    Bitmap.AddDirtyRect(new Int32Rect(0, 0, frame.Cols, frame.Rows));
                }
                finally
                {
                    Bitmap.Unlock();
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bitmap)));
            });
        }

        private void OnStart(object obj)
        {
            if (Running)
            {
                Stop();
            }
            else
            {
                Run();
            }
        }

        private void OnTimer(object sender, EventArgs e)
        {
            if (Running == false)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElapsedTime)));
        }

        private void OnOption(object obj)
        {
            if (_optionDialog != null)
                return;

            var cloned = JsonConvert.DeserializeObject<Model.Option>(JsonConvert.SerializeObject(Option.Model));
            var oldResourcePath = Option.ResourceFilePath;
            _optionDialog = new OptionDialog
            {
                DataContext = new Option(cloned)
            };
            _optionDialog.Show();
            _optionDialog.Closed += (sender, e) =>
            {
                Option = new Option(cloned);
                if (Option.ResourceFilePath != oldResourcePath)
                    Model.LoadSpriteList();

                _optionDialog = null;
            };
        }

        private void ShowEditSpriteDialog(IEnumerable<Sprite> source, Sprite selection = null)
        {
            if (EditResourceDialog != null)
                return;

            var cloned = source.Select(sprite => new Model.Sprite
            {
                Name = sprite.Name,
                Source = sprite.Source,
                Threshold = sprite.Threshold,
                Extension = sprite.Extension == null ? null : new Model.SpriteExtension
                {
                    Activated = sprite.Extension.Activated,
                    DetectColor = sprite.Extension.DetectColor,
                    Factor = sprite.Extension.Factor,
                    Pivot = sprite.Extension.Pivot
                }
            }).OrderBy(x => x.Name).ToList();

            var model = new EditResourceWindow(cloned)
            {
                CompleteCommand = new RelayCommand(x =>
                {
                    try
                    {
                        var dataContext = EditResourceDialog.DataContext as EditResourceWindow;
                        var duplicatedNameList = dataContext.Sprites.GroupBy(x => x.Name).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
                        if (duplicatedNameList.Count > 0)
                            throw new Exception($"Duplicate names: {string.Join(", ", duplicatedNameList)}");

                        Sprites = dataContext.Sprites;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sprites)));

                        EditResourceDialog.Close();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                }),
                CancelCommand = new RelayCommand(x =>
                {
                    EditResourceDialog.Close();
                })
            };
            model.Selected = selection;
            EditResourceDialog = new EditResourceDialog
            {
                DataContext = model
            };

            EditResourceDialog.Closed += (sender, e) =>
            {
                selection?.Dispose();
                EditResourceDialog = null;
            };
            EditResourceDialog.Show();
        }

        private void OnEditSprite(object obj)
        {
            ShowEditSpriteDialog(Sprites);
        }

        private void OnSetPicture(object obj)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                //InitialDirectory = Directory.GetCurrentDirectory(),
                DefaultExt = ".jpg",
                Filter = "Image (.jpg)|*.jpg"
            };
            if (dialog.ShowDialog() == false)
                return;

            StaticFrame = MatPool.GetClone(Cv2.ImRead(dialog.FileName));
        }

        private void OnResetScreen(object obj)
        {
            StaticFrame = null;
        }

        /// <summary>
        /// Consume frames from App's Channel and process them
        /// Performance: Direct channel consumption for better decoupling
        /// </summary>
        private async Task ConsumeFramesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var frame in _target.FrameReader.ReadAllAsync(cancellationToken))
                {
                    using (frame) // Auto-dispose PooledMat when done
                    {
                        try
                        {
                            // Process frame on UI thread
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (StaticFrame != null)
                                {
                                    Frame = StaticFrame; // StaticFrame is already PooledMat
                                    UpdateBitmap(StaticFrame);
                                }
                                else
                                {
                                    // Clone frame before storing to avoid use-after-return issues
                                    var frameClone = MatPool.GetClone(frame);
                                    Frame = frameClone; // Thread safety guaranteed by ReaderWriterLockSlim
                                    UpdateBitmap(frameClone);
                                }
                                _elapsedStopwatch.Stop();
                                _elapsedStopwatch.Restart();
                            });
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Frame processing error: {e.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        #endregion

        #region IDisposable 구현

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                    Stop(); // Clean up running tasks
                    _frameLock?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

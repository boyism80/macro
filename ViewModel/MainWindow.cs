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
    public partial class MainWindow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static SolidColorBrush INACTIVE_FRAME_BACKGROUND = new SolidColorBrush(Colors.White);
        public static SolidColorBrush ACTIVE_FRAME_BACKGROUND = new SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 222, 222));

        public event EventHandler Started;
        public event EventHandler Stopped;

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

        public Model.App target { get; private set; }

        private ViewModel.Option _option;
        public ViewModel.Option Option
        {
            get => _option;
            set
            {
                _option = value;
                Model.Option = value?.Model;

                if (value != null && target != null)
                {
                    target.Fps = value.RenderFrame;
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
        public string RunStateText => Running ? "진행중" : "대기중";
        public string ExceptionText { get; set; } = string.Empty;
        public string Title => "macro";
        public string StatusName => string.Empty;
        public Visibility DarkBackgroundVisibility { get; private set; } = Visibility.Hidden;

        private readonly Stopwatch _elapsedStopwatch = new Stopwatch();
        public Mat Frame { get; private set; }
        public Mat StaticFrame { get; private set; }
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
            Sprites = [.. Model.Sprites.Values.Select(x => new ViewModel.Sprite(x))];

            RunCommand = new RelayCommand(OnStart);
            OptionCommand = new RelayCommand(OnOption);
            EditSpriteCommand = new RelayCommand(OnEditSprite);
            SetPictureCommand = new RelayCommand(OnSetPicture);
            ResetScreenCommand = new RelayCommand(OnResetScreen);
            SelectedRectCommand = new RelayCommand(OnSelectedRect);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
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

        private void OnSelectedRect(object obj)
        {
            var parameters = obj as object[];
            var selectedRect = (System.Windows.Rect)parameters[1];
            var mouseButton = (MouseButton)parameters[2];

            switch (mouseButton)
            {
                case MouseButton.Left:
                    {
                        // Performance: Use MatPool for ROI operation instead of new Mat allocation
                        var rect = new OpenCvSharp.Rect
                        {
                            X = (int)selectedRect.X,
                            Y = (int)selectedRect.Y,
                            Width = (int)selectedRect.Width,
                            Height = (int)selectedRect.Height
                        };

                        var selectedFrame = MatPool.GetRoi(Frame, rect);

                        var newSprite = new Sprite(new Model.Sprite
                        {
                            Name = "New Sprite",
                            Source = selectedFrame, // MatPool object can be passed to external components
                            Threshold = 0.8f,
                            Extension = new Model.SpriteExtension
                            {
                                Activated = false,
                                DetectColor = false,
                                Factor = 1.0f,
                                Pivot = System.Drawing.Color.White
                            }
                        });
                        var source = Sprites.Concat([newSprite]);

                        ShowEditSpriteDialog(source, newSprite);
                    }
                    break;

                case MouseButton.Right:
                    {
                        Clipboard.SetText($"{{\"x\": {(int)selectedRect.X}, \"y\": {(int)selectedRect.Y}, \"width\": {(int)selectedRect.Width}, \"height\": {(int)selectedRect.Height}}}");
                        MessageBox.Show("Area saved to clipboard.");
                    }
                    break;
            }
        }

        private void Run()
        {
            try
            {
                if (Running)
                    return;

                if (target != null)
                {
                    _frameConsumerCancellation?.Cancel();
                }

                LoadPythonModules(Option.PythonDirectoryPath);
                target = macro.Model.App.Find(Option.Class);
                if (target == null)
                    throw new Exception($"cannot find {Option.Class}");

                // Performance: Set target FPS for frame capture
                target.Fps = Option.RenderFrame;

                // Performance: Start frame consumer task
                _frameConsumerCancellation = new CancellationTokenSource();
                Task.Run(() => ConsumeFramesAsync(_frameConsumerCancellation.Token));

                target.Start();
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
            if (target != null)
            {
                target.Stop();
                target = null;
            }

            // Performance: Cancel frame consumer task
            _frameConsumerCancellation?.Cancel();
            _frameConsumerCancellation = null;

            ReleasePythonModule();
            Bitmap = null;

            // Performance: Clear all Mat pools on application shutdown
            MatPool.Clear();

            Running = false;

            Stopped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update WPF bitmap from OpenCV Mat with memory optimization
        /// Performance: Reuses existing WriteableBitmap when possible to avoid allocations
        /// </summary>
        private void UpdateBitmap(Mat frame)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Performance: Only create new WriteableBitmap if size changed
                if (Bitmap == null || Bitmap.Width != frame.Width || Bitmap.Height != frame.Height)
                    Bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);

                Bitmap.Lock();
                // Direct memory copy from Mat to WriteableBitmap buffer
                frame.CopyTo(new Mat(frame.Rows, frame.Cols, MatType.CV_8UC3, Bitmap.BackBuffer));
                Bitmap.AddDirtyRect(new Int32Rect(0, 0, frame.Cols, frame.Rows));
                Bitmap.Unlock();
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
                DataContext = new ViewModel.Option(cloned)
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

            var model = new ViewModel.EditResourceWindow(cloned)
            {
                CompleteCommand = new RelayCommand(x =>
                {
                    try
                    {
                        var dataContext = EditResourceDialog.DataContext as ViewModel.EditResourceWindow;
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

            StaticFrame = Cv2.ImRead(dialog.FileName);
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
                await foreach (var frame in target.FrameReader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        // Process frame on UI thread
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ProcessFrame(frame);
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Frame processing error: {e.Message}");
                    }
                    finally
                    {
                        // Performance: Return frame to MatPool after processing
                        MatPool.Return(frame);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        /// <summary>
        /// Process individual frame with optimized memory management
        /// Performance: Processes frames with StaticFrame override support
        /// </summary>
        private void ProcessFrame(Mat frame)
        {
            if (StaticFrame != null)
                frame = StaticFrame;

            Frame = frame;
            UpdateBitmap(frame);
            _elapsedStopwatch.Stop();
            _elapsedStopwatch.Restart();
        }
    }
}

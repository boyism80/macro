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
                if (value == null)
                {
                    Model.Option = null;
                }
                else
                {
                    Model.Option = value.Model;
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
        private Mutex _sourceFrameLock = new Mutex();
        private Mat _sourceFrame = null;
        public Mat Frame
        {
            get
            {
                try
                {
                    _sourceFrameLock.WaitOne();
                    if (_sourceFrame == null)
                        return null;

                    return _sourceFrame.Clone();
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    _sourceFrameLock.ReleaseMutex();
                }
            }
            set
            {
                _sourceFrameLock.WaitOne();
                _sourceFrame?.Dispose();
                _sourceFrame = value.Clone();
                _sourceFrameLock.ReleaseMutex();
            }
        }
        public Mat StaticFrame { get; private set; }
        public BitmapImage Bitmap
        {
            get
            {
                if (Running == false)
                    return null;

                return Model.Bitmap;
            }
            set
            {
                Model.Bitmap = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FrameBackgroundBrush)));
            }
        }


        public ObservableCollection<ViewModel.Sprite> Sprites
        {
            get => new ObservableCollection<ViewModel.Sprite>(Model.Sprites.Values.Select(x => new Sprite(x)));
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
            Sprites = new ObservableCollection<Sprite>(Model.Sprites.Values.Select(x => new ViewModel.Sprite(x)));

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
        }

        private void OnSelectedRect(object obj)
        {
            var parameters = obj as object[];
            var selectedRect = (System.Windows.Rect)parameters[1];
            var selectedFrame = new Mat(Frame, new OpenCvSharp.Rect { X = (int)selectedRect.X, Y = (int)selectedRect.Y, Width = (int)selectedRect.Width, Height = (int)selectedRect.Height });
            var mouseButton = (MouseButton)parameters[2];

            switch (mouseButton)
            {
                case MouseButton.Left:
                    {
                        var newSprite = new Sprite(new Model.Sprite
                        {
                            Name = "New Sprite",
                            Source = selectedFrame,
                            Threshold = 0.8f,
                            Extension = new Model.SpriteExtension
                            {
                                Activated = false,
                                DetectColor = false,
                                Factor = 1.0f,
                                Pivot = System.Drawing.Color.White
                            }
                        });
                        var source = Sprites.Concat(new[] { newSprite });

                        ShowEditSpriteDialog(source, newSprite);
                    }
                    break;

                case MouseButton.Right:
                    {
                        Clipboard.SetText($"{{\"x\": {(int)selectedRect.X}, \"y\": {(int)selectedRect.Y}, \"width\": {(int)selectedRect.Width}, \"height\": {(int)selectedRect.Height}}}");
                        MessageBox.Show("클립보드에 영역이 저장되었습니다.");
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
                    target.Frame -= App_Frame;
                }
                
                LoadPythonModules(Option.PythonDirectoryPath);
                target = macro.Model.App.Find(Option.Class);
                if (target == null)
                    throw new Exception($"cannot find {Option.Class}");

                target.Frame += App_Frame;
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
                target.Frame -= App_Frame;
                target = null;
            }

            ReleasePythonModule();
            Bitmap = null;
            Running = false;

            Stopped?.Invoke(this, EventArgs.Empty);
        }

        private void App_Frame(OpenCvSharp.Mat frame)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            if (StaticFrame != null)
                frame = StaticFrame;

            Frame = frame;
            Bitmap = frame.ToBitmap();
            _elapsedStopwatch.Stop();
            _elapsedStopwatch.Restart();

            stopWatch.Stop();
            Thread.Sleep(Math.Max(0, 1000 / Option.RenderFrame - (int)stopWatch.ElapsedMilliseconds));
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
                            throw new Exception($"이름 중복 : {string.Join(", ", duplicatedNameList)}");

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

            EditResourceDialog.Closed += (sender, e) => EditResourceDialog = null;
            EditResourceDialog.Show();
        }

        private void OnEditSprite(object obj)
        {
            ShowEditSpriteDialog(Sprites);
        }

        private void OnSetPicture(object obj)
        {

        }
        
        private void OnResetScreen(object obj)
        {

        }
    }
}

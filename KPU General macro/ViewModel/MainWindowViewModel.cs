using IronPython.Hosting;
using IronPython.Runtime;
using KPU_General_macro.Dialog;
using KPU_General_macro.Extension;
using KPU_General_macro.Model;
using KPU_General_macro.ViewModel;
using Microsoft.Scripting.Hosting;
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KPU_General_macro
{
    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        public static SolidColorBrush INACTIVE_FRAME_BACKGROUND = new SolidColorBrush(Colors.White);
        public static SolidColorBrush ACTIVE_FRAME_BACKGROUND = new SolidColorBrush(Color.FromRgb(222, 222, 222));
        public const int MAXIMUM_IDLE_TIME = 1000 * 10;

        private ScriptRuntime _pythonRuntime;
        private Mutex _pythonRuntimeLock = new Mutex();
        private Stopwatch _elapsedStopwatch = new Stopwatch();
        private Stopwatch _idleStopwatch = new Stopwatch();
        private Sprite _sprite;
        private Status _status;
        private string _lastStatusName = string.Empty;
        private bool _handleFrameThreadExecutable = true;
        private Mutex _handleFrameThreadExecutableLock = new Mutex();

        public MainWindow MainWindow { get; private set; }

        public DestinationApp App
        {
            get
            {
                return DestinationApp.Instance;
            }
        }

        public OptionViewModel OptionViewModel { get; private set; } = new OptionViewModel();

        public OptionDialog OptionDialog { get; private set; }

        public TimeSpan ElapsedTime { get; private set; } = new TimeSpan();

        public bool IsRunning { get; private set; } = false;

        public string RunningStateText
        {
            get
            {
                return this.IsRunning ? "진행중" : "대기중";
            }
        }

        public SolidColorBrush FrameBackgroundBrush
        {
            get
            {
                return this.Frame != null ? ACTIVE_FRAME_BACKGROUND : INACTIVE_FRAME_BACKGROUND;
            }
        }

        public string ExceptionText { get; private set; } = string.Empty;
        public string StatusName { get; private set; } = "Unknown";

        public string RunButtonText
        {
            get
            {
                return this.IsRunning ? "Stop" : "Run";
            }
        }

        private BitmapImage _frame;
        public BitmapImage Frame
        {
            get { return this.IsRunning ? this._frame : null; }
            set { this._frame = value; this.OnPropertyChanged(nameof(this.FrameBackgroundBrush)); }
        }

        public Mutex SourceFrameLock { get; private set; } = new Mutex();
        public Mat SourceFrame { get; private set; }
        public Detector Detector { get; private set; }
        public bool InitStopWatch { get; set; } = false;

        public PythonDictionary Sprite { get; private set; } = new PythonDictionary();
        public PythonDictionary Status { get; private set; } = new PythonDictionary();
        public PythonDictionary Timers { get; private set; } = new PythonDictionary();
        public PythonDictionary State { get; private set; } = new PythonDictionary();

        public ObservableCollection<LogViewModel> LogItems { get; private set; } = new ObservableCollection<LogViewModel>();

        public Visibility DarkBackgroundVisibility { get; private set; } = Visibility.Hidden;

        public ICommand SetMinimizeCommand { get; private set; }
        public ICommand SetMaximizeCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand OptionCommand { get; private set; }
        public ICommand RunCommand { get; private set; }


        public ICommand CompleteCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SelectSpriteFileCommand { get; private set; }
        public ICommand SelectStatusFileCommand { get; private set; }
        public ICommand BrowsePythonDirectoryCommand { get; private set; }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.MainWindow = mainWindow;
            DestinationApp.Instance.Frame += this.App_Frame;

            this.OptionViewModel.Load();

            this.SetMinimizeCommand = new RelayCommand(this.OnSetMinimize);
            this.SetMaximizeCommand = new RelayCommand(this.OnSetMaximize);
            this.CloseCommand = new RelayCommand(this.OnClose);
            this.OptionCommand = new RelayCommand(this.OnOption);
            this.RunCommand = new RelayCommand(this.OnRun);

            this.CompleteCommand = new RelayCommand(this.OnComplete);
            this.CancelCommand = new RelayCommand(this.OnCancel);
            this.SelectSpriteFileCommand = new RelayCommand(this.OnSelectSpriteCommand);
            this.SelectStatusFileCommand = new RelayCommand(this.OnSelectStatusFile);
            this.BrowsePythonDirectoryCommand = new RelayCommand(this.OnBrowsePythonDirectory);
        }

        private void OnSelectStatusFile(object obj)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = this.OptionViewModel.StatusFile.Content;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            this.OptionViewModel.StatusFile.Content = dialog.FileName;
        }

        private void App_Frame(OpenCvSharp.Mat frame)
        {
this.SourceFrameLock.WaitOne();
            if (this.SourceFrame != null)
                this.SourceFrame.Dispose();

            this.SourceFrame = frame;
this.SourceFrameLock.ReleaseMutex();
            this.ExecPython(this.OptionViewModel.Model.RenderScriptName, frame);
            this.Frame = BitmapExtension.Parse(frame);
            this._elapsedStopwatch.Stop();
            this.ElapsedTime = this.ElapsedTime.Add(TimeSpan.FromMilliseconds(this._elapsedStopwatch.ElapsedMilliseconds));
            this._elapsedStopwatch.Restart();


this._handleFrameThreadExecutableLock.WaitOne();
            if (this._handleFrameThreadExecutable)
            {
                var thread = new Thread(new ThreadStart(this.FrameHandlerRoutine));
                thread.Start();
            }
this._handleFrameThreadExecutableLock.ReleaseMutex();
        }

        private void FrameHandlerRoutine()
        {
            var frame = this.SourceFrame.Clone();

            try
            {
this._handleFrameThreadExecutableLock.WaitOne();
                this._handleFrameThreadExecutable = false;
this._handleFrameThreadExecutableLock.ReleaseMutex();

                var points = new Dictionary<string, OpenCvSharp.Point>();
                var statusName = this.Detector.Detect(frame, out points);
                this.StatusName = statusName ?? throw new Exception();

                if (this._lastStatusName.Equals(statusName))
                {
                    this._idleStopwatch.Stop();
                    if (this._idleStopwatch.ElapsedMilliseconds > MAXIMUM_IDLE_TIME || this.InitStopWatch)
                    {
                        this._lastStatusName = string.Empty;
                        this._idleStopwatch.Reset();
                        this.InitStopWatch = false;
                    }

                    this._idleStopwatch.Start();
                }
                else
                {
                    this.ExecPython(this._status[statusName].Script, frame, points.ToDict(), true);
                    this._lastStatusName = statusName;
                    this._idleStopwatch.Reset();
                }
            }
            catch (Exception e)
            {
                this.StatusName = "Unknown";
            }
            finally
            {
                this.ExecPython(this.OptionViewModel.Model.FrameScriptName, frame, null, true);

this._handleFrameThreadExecutableLock.WaitOne();
                this._handleFrameThreadExecutable = true;
this._handleFrameThreadExecutableLock.ReleaseMutex();

                frame.Dispose();
            }
        }

        private void OnRun(object obj)
        {
            if (this.IsRunning)
            {
                this.Stop();
            }
            else
            {
                this.Run();
            }

            this.OnPropertyChanged(nameof(this.RunningStateText));
            this.OnPropertyChanged(nameof(this.RunButtonText));
        }

        public void Run()
        {
            try
            {
                if (this.IsRunning)
                    return;

                this.LoadPythonModules(this.OptionViewModel.Model.PythonDirectory);
                this.LoadResources(this.OptionViewModel.Model.SpriteFile, this.OptionViewModel.Model.StatusFile);
                this._elapsedStopwatch.Restart();
                this._idleStopwatch.Reset();
                DestinationApp.Instance.BindToApp(this.OptionViewModel.Model.ClassName);
                DestinationApp.Instance.Start();
                this.IsRunning = true;

                this.ExecPython(this.OptionViewModel.Model.InitializeScriptName);
                this.ExceptionText = string.Empty;
            }
            catch (Exception e)
            {
                DestinationApp.Instance.Stop();
                this.ReleasePythonModule();
                this.ReleaseResources();
                this._elapsedStopwatch.Reset();
                this.IsRunning = false;

                this.ExceptionText = e.Message;
            }
        }

        public void Stop()
        {
            if (this.IsRunning == false)
                return;

            this.ReleasePythonModule();
            DestinationApp.Instance.Stop();
            DestinationApp.Instance.Unbind();
            this.ElapsedTime = new TimeSpan();
            this._elapsedStopwatch.Reset();
            this._idleStopwatch.Reset();
            this._lastStatusName = string.Empty;
            this.ExecPython(this.OptionViewModel.Model.DisposeScriptName);
            this.IsRunning = false;
        }

        private void OnBrowsePythonDirectory(object obj)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = this.OptionViewModel.PythonDirectory.Content;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            this.OptionViewModel.PythonDirectory.Content = dialog.FileName;
        }

        private void OnSelectSpriteCommand(object obj)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = this.OptionViewModel.SpriteFile.Content;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            this.OptionViewModel.SpriteFile.Content = dialog.FileName;
        }

        private void OnCancel(object obj)
        {
            this.OptionDialog.Close();
        }

        private void OnComplete(object obj)
        {
            this.OptionViewModel.Apply();
            this.OptionViewModel.Save();
            this.OptionDialog.Close();
        }

        private void OnOption(object obj)
        {
            if (this.OptionDialog != null)
                return;

            this.OptionDialog = new OptionDialog()
            {
                Owner = this.MainWindow,
                DataContext = this,
            };
            this.OptionDialog.Closed += this.OptionDialog_Closed;
            this.DarkBackgroundVisibility = Visibility.Visible;
            this.OptionDialog.Show();
        }

        private void OptionDialog_Closed(object sender, EventArgs e)
        {
            this.OptionDialog = null;
            this.DarkBackgroundVisibility = Visibility.Hidden;
        }

        public void OnSetMinimize(object parameter)
        {
            this.MainWindow.WindowState = System.Windows.WindowState.Minimized;
        }

        public void OnSetMaximize(object parameter)
        {
            this.MainWindow.WindowState ^= System.Windows.WindowState.Maximized;
        }

        public void OnClose(object parameter)
        {
            this.MainWindow.Close();
        }

        private void LoadPythonModules(string path)
        {
            if (path.Length == 0)
                throw new Exception("You must set a valid python path.");

            if (Directory.Exists(path) == false)
                throw new Exception(string.Format("{0} path does not exist.", path));

            if (File.Exists(Path.Combine(path, "python.exe")) == false)
                throw new Exception(string.Format("Cannot find python.exe file in '{0}'.", path));

this._pythonRuntimeLock.WaitOne();
            if (this._pythonRuntime != null)
                this._pythonRuntime.Shutdown();

            this._pythonRuntime = Python.CreateRuntime();
            var engine = this._pythonRuntime.GetEngine("IronPython");
this._pythonRuntimeLock.ReleaseMutex();
            var paths = engine.GetSearchPaths();
            paths.Add(path);
            paths.Add(Path.Combine(path, @"DLLs"));
            paths.Add(Path.Combine(path, @"lib"));
            paths.Add(Path.Combine(path, @"lib\site-packages"));
            paths.Add(Path.Combine(Directory.GetCurrentDirectory(), "scripts"));
            engine.SetSearchPaths(paths);
        }

        private void ReleasePythonModule()
        {
this._pythonRuntimeLock.WaitOne();
            if(this._pythonRuntime != null)
                this._pythonRuntime.Shutdown();

            this._pythonRuntime = null;
this._pythonRuntimeLock.ReleaseMutex();
        }

        private void LoadResources(string spriteFileName, string statusFileName)
        {
            this._sprite = new Sprite();
            if (this._sprite.load(spriteFileName) == false)
                throw new Exception($"Cannot load sprite file : {Path.GetFileName(spriteFileName)}");

            this._status = new Status(this._sprite);
            if (this._status.Load(statusFileName) == false)
                throw new Exception($"Cannot load status file : {Path.GetFileName(statusFileName)}");

            this.Sprite = this._sprite.ToDict();
            this.Status = this._status.ToDict();
            this.Detector = new Detector(this._sprite, this._status);
        }

        private void ReleaseResources()
        {
            if (this._status != null)
                this._status.Dispose();
            this._status = null;

            if (this._sprite != null)
                this._sprite.Dispose();
            this._sprite = null;

            this.Sprite.Clear();
            this.Sprite = null;

            this.Status.Clear();
            this.Status = null;

            this.Detector = null;
        }

        private void randomTimer_Tick(object status)
        {
            var parameters = status as string[];
            var script = parameters[0];
            var name = parameters[1];

            this.UnsetTimer(name);
            this.ExecPython(script);
        }

        public System.Threading.Timer SetTimer(string name, int interval, string script)
        {
            if (this.Timers.ContainsKey(name))
                return null;

            var createdTimer = new System.Threading.Timer(this.randomTimer_Tick, new string[] { script, name }, interval, System.Threading.Timeout.Infinite);
            this.Timers.Add(name, createdTimer);

            return createdTimer;
        }

        public void UnsetTimer(string name)
        {
            if (this.Timers.ContainsKey(name) == false)
                return;

            var timer = this.Timers[name] as System.Threading.Timer;
            timer.Dispose();
            this.Timers.Remove(name);
        }

        private object ExecPython(string fname, Mat frame = null, object parameter = null, bool log = false)
        {
            try
            {
this._pythonRuntimeLock.WaitOne();
                var script = string.Format("scripts/{0}", fname);
                if (this._pythonRuntime == null)
                    return null;

                dynamic scope = this._pythonRuntime.UseFile(script);
                var ret = scope.callback(this, frame, parameter);
                if (log && ret != null)
                    this.AddHistory($"{script} Returns : {ret}");

                return ret;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
this._pythonRuntimeLock.ReleaseMutex();
            }
        }

        public void Dispose()
        {
            this.OptionViewModel.Save();
            this.Stop();
        }

        private Nullable<int> MatchNumber(Mat source)
        {
            var maximum = 0.0;
            var percentage = 0.0;
            var selected = new Nullable<int>();
            for (var num = 9; num > 0; num--)
            {
                var location = this._sprite[num.ToString()].MatchTo(source, ref percentage, null as Nullable<OpenCvSharp.Point>, null as Nullable<OpenCvSharp.Size>, true, false);
                if (location == null)
                    continue;

                if (percentage > maximum)
                {
                    selected = num;
                    maximum = percentage;
                }
            }

            return selected;
        }

        public IronPython.Runtime.List Partition(Mat frame)
        {
            var components = new Mat[3, 3, 3, 3];
            var componentsInt = new Nullable<int>[3, 3, 3, 3];
            var begin = new OpenCvSharp.Point(11, 159);
            var size = new OpenCvSharp.Size(180, 180);

            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var partition = frame.Clone(new OpenCvSharp.Rect(new OpenCvSharp.Point(begin.X + (size.Width + 1) * col, begin.Y + (size.Height + 1) * row), size));
                    this.ExtractPartitionComponents(partition, row, col, components);
                }
            }

            var pythonList = new List();
            for (var i1 = 0; i1 < 3; i1++)
            {
                var outerRowList = new List();
                for (var i2 = 0; i2 < 3; i2++)
                {
                    var outerColumnList = new List();
                    for (var i3 = 0; i3 < 3; i3++)
                    {
                        var innerRowList = new List();
                        for (var i4 = 0; i4 < 3; i4++)
                        {
                            var innerColumnList = new List();
                            try
                            {
                                var num = this.MatchNumber(components[i1, i2, i3, i4]);
                                if (num == null)
                                    throw new Exception();

                                //Console.WriteLine("{0} {1} {2} {3} : {4}", i1, i2, i3, i4, num);
                                innerRowList.Add(num);
                            }
                            catch (Exception)
                            {
                                for (var i = 0; i < 9; i++)
                                    innerColumnList.Add(i + 1);
                                innerRowList.Add(innerColumnList);
                                //Console.WriteLine("{0} {1} {2} {3} : {4}", i1, i2, i3, i4, "unknown");
                            }
                        }
                        outerColumnList.Add(innerRowList);
                    }
                    outerRowList.Add(outerColumnList);
                }
                pythonList.Add(outerRowList);
            }
            //Console.WriteLine(Environment.NewLine + Environment.NewLine + Environment.NewLine);

            return pythonList;
        }

        private void ExtractPartitionComponents(Mat frame, int sourceRow, int sourceColumn, Mat[,,,] frames)
        {
            var size = new OpenCvSharp.Size(60, 60);
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var component = frame.Clone(new OpenCvSharp.Rect(new OpenCvSharp.Point(size.Width * col, size.Height * row), size));
                    frames[sourceRow, sourceColumn, row, col] = component;
                }
            }
        }

        public List<OpenCvSharp.Rect>[] Compare(Mat frame, OpenCvSharp.Point offset1, OpenCvSharp.Point offset2, OpenCvSharp.Size size, int threshold = 64, int count = 5)
        {
            var source1 = new Mat(frame, new OpenCvSharp.Rect(offset1, size));
            var source2 = new Mat(frame, new OpenCvSharp.Rect(offset2, size));

            var difference = new Mat();
            Cv2.Absdiff(source1, source2, difference);

            difference = difference.Threshold(threshold, 255, ThresholdTypes.Binary); // 이거 애매함
            difference = difference.MedianBlur(5);

            var kernel = Mat.Ones(5, 5, MatType.CV_8UC1);
            difference = difference.Dilate(kernel);
            difference = difference.CvtColor(ColorConversionCodes.BGR2GRAY);

            var percentage = Cv2.CountNonZero(difference) * 100.0f / (difference.Width * difference.Height);
            if (percentage > 10.0f)
                return null;

            var labels = new Mat();
            var stats = new Mat();
            var centroids = new Mat();
            var countLabels = difference.ConnectedComponentsWithStats(labels, stats, centroids);

            var areaList1 = new List<OpenCvSharp.Rect>();
            var areaList2 = new List<OpenCvSharp.Rect>();
            for (var i = 1; i < countLabels; i++)
            {
                var x = stats.Get<int>(i, 0);
                var y = stats.Get<int>(i, 1);
                var width = stats.Get<int>(i, 2);
                var height = stats.Get<int>(i, 3);
                areaList1.Add(new OpenCvSharp.Rect(offset1.X + x, offset1.Y + y, width, height));
                areaList2.Add(new OpenCvSharp.Rect(offset2.X + x, offset2.Y + y, width, height));
            }

            areaList1.Sort((area1, area2) => area1.Width * area1.Height > area2.Width * area2.Height ? -1 : 1);
            areaList2.Sort((area1, area2) => area1.Width * area1.Height > area2.Width * area2.Height ? -1 : 1);

            var cloned = frame.Clone();
            foreach (var area in areaList1)
                cloned.Rectangle(area, new Scalar(0, 0, 255));

            //Cv2.ImShow("before", cloned);
            //Cv2.WaitKey(0);
            //Cv2.DestroyAllWindows();
            //cloned.Dispose();

            var deletedList = new List<OpenCvSharp.Rect>();
            var basedLength = 250;
            for (var i1 = 0; i1 < areaList1.Count; i1++)
            {
                if (deletedList.Contains(areaList1[i1]))
                    continue;

                for (var i2 = i1 + 1; i2 < areaList1.Count; i2++)
                {
                    var scaleLength = Math.Min(50, (int)(10 + 250 / ((Math.Max(areaList1[i1].Width, areaList1[i1].Height) + Math.Max(areaList1[i2].Width, areaList1[i2].Height)) / 2.0f)));
                    var scaledArea = new OpenCvSharp.Rect(areaList1[i1].X - scaleLength, areaList1[i1].Y - scaleLength, areaList1[i1].Width + scaleLength * 2, areaList1[i1].Height + scaleLength * 2);
                    var overlapped = scaledArea & areaList1[i2];
                    if (overlapped.Width != 0 && overlapped.Height != 0)
                        deletedList.Add(areaList1[i2]);
                }
            }

            foreach (var deleted in deletedList)
                areaList1.Remove(deleted);

            cloned = frame.Clone();
            foreach (var area in areaList1)
                cloned.Rectangle(area, new Scalar(0, 0, 255));

            //Cv2.ImShow("after", cloned);
            //Cv2.WaitKey(0);
            //Cv2.DestroyAllWindows();
            //cloned.Dispose();


            if (areaList1.Count != count)
            {
                if (threshold < 0)
                    return null;

                return Compare(frame, offset1, offset2, size, threshold - 1, count);
            }

            deletedList.Clear();
            for (var i1 = 0; i1 < areaList2.Count; i1++)
            {
                if (deletedList.Contains(areaList2[i1]))
                    continue;

                for (var i2 = i1 + 1; i2 < areaList2.Count; i2++)
                {
                    var scaleLength = Math.Min(50, (int)(10 + 250 / ((Math.Max(areaList2[i1].Width, areaList2[i1].Height) + Math.Max(areaList2[i2].Width, areaList2[i2].Height)) / 2.0f)));
                    var scaledArea = new OpenCvSharp.Rect(areaList2[i1].X - scaleLength, areaList2[i1].Y - scaleLength, areaList2[i1].Width + scaleLength * 2, areaList2[i1].Height + scaleLength * 2);
                    var overlapped = scaledArea & areaList2[i2];
                    if (overlapped.Width != 0 && overlapped.Height != 0)
                        deletedList.Add(areaList2[i2]);
                }
            }

            foreach (var deleted in deletedList)
                areaList2.Remove(deleted);

            return new List<OpenCvSharp.Rect>[] { areaList1, areaList2 };
        }

        public IronPython.Runtime.List Compare(Mat frame, PythonTuple offset1, PythonTuple offset2, PythonTuple size, int threshold = 64, int count = 5)
        {
            try
            {
                var areaLists = this.Compare(frame, new OpenCvSharp.Point((int)offset1[0], (int)offset1[1]), new OpenCvSharp.Point((int)offset2[0], (int)offset2[1]), new OpenCvSharp.Size((int)size[0], (int)size[1]), threshold, count);
                if (areaLists == null)
                    throw new Exception();

                var pythonAreaLists = new IronPython.Runtime.List();
                foreach (var areaList in areaLists)
                {
                    var pythonAreaList = new IronPython.Runtime.List();
                    foreach (var area in areaList)
                    {
                        var pythonArea = new PythonTuple(new object[] { area.X, area.Y, area.Width, area.Height });
                        pythonAreaList.Add(pythonArea);
                    }
                    pythonAreaLists.Add(pythonAreaList);
                }

                return pythonAreaLists;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool DrawRectangles(Mat frame, List<OpenCvSharp.Rect> areas, uint color = 0xffff0000)
        {
            if (frame == null)
                return false;

            foreach (var area in areas)
            {
                var r = (color & 0x00ff0000) >> 4;
                var g = (color & 0x0000ff00) >> 2;
                var b = (color & 0x000000ff);
                frame.Rectangle(area, new Scalar(b, g, r));
            }
            return true;
        }

        public bool DrawRectangles(Mat frame, IronPython.Runtime.List areas, uint color = 0xffff0000)
        {
            try
            {
                var csAreaList = new List<OpenCvSharp.Rect>();
                foreach (var area in areas)
                {
                    var pythonArea = area as PythonTuple;
                    csAreaList.Add(new OpenCvSharp.Rect((int)pythonArea[0], (int)pythonArea[1], (int)pythonArea[2], (int)pythonArea[3]));
                }

                return this.DrawRectangles(frame, csAreaList, color);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Sleep(int m)
        {
            Thread.Sleep(m);
        }

        public void AddHistory(string message, Nullable<DateTime> datetime = null)
        {
            try
            {
                this.LogItems.Add(new LogViewModel(message, datetime != null ? datetime.Value : DateTime.Now));
            }
            catch (Exception)
            { }
        }
    }
}

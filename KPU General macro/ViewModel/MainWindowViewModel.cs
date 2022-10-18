﻿using IronPython.Hosting;
using IronPython.Runtime;
using KPUGeneralmacro.Extension;
using KPUGeneralMacro.Dialog;
using KPUGeneralMacro.Extension;
using KPUGeneralMacro.ViewModel;
using Microsoft.Scripting.Hosting;
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KPUGeneralMacro
{
    public static class LogExt
    {
        public static void Add(this ObservableCollection<LogViewModel> logs, string message)
        {
            try
            {
                logs.Add(new LogViewModel(message, DateTime.Now));
            }
            catch
            { }
        }
    }

    public static class PythonExt
    {
        public static PythonDictionary ToPythonDictionary<K, V>(this IDictionary<K, V> dict)
        {
            var pythonDict = new PythonDictionary();
            foreach (var pair in dict)
                pythonDict.Add(pair.Key, pair.Value);

            return pythonDict;
        }
    }

    public class MainWindowViewModel : BaseViewModel, IDisposable
    {
        public static SolidColorBrush INACTIVE_FRAME_BACKGROUND = new SolidColorBrush(Colors.White);
        public static SolidColorBrush ACTIVE_FRAME_BACKGROUND = new SolidColorBrush(System.Windows.Media.Color.FromRgb(222, 222, 222));
        public const int MAXIMUM_IDLE_TIME = 1000 * 2;

        private ScriptRuntime _pythonRuntime;
        private readonly Stopwatch _elapsedStopwatch = new Stopwatch();
        private readonly Stopwatch _idleStopwatch = new Stopwatch();
        private string _lastStatusName = string.Empty;
        private Dialog.SpriteDialog _spriteDialog;

        public MainWindow MainWindow { get; private set; }

        public Model.App target { get; private set; }

        public OptionViewModel OptionViewModel { get; private set; } = new OptionViewModel();

        public OptionDialog OptionDialog { get; private set; }

        public TimeSpan ElapsedTime { get; private set; } = new TimeSpan();

        public bool IsRunning { get; private set; } = false;

        public string RunningStateText => this.IsRunning ? "진행중" : "대기중";

        public SolidColorBrush FrameBackgroundBrush => this.Bitmap != null ? ACTIVE_FRAME_BACKGROUND : INACTIVE_FRAME_BACKGROUND;

        public string ExceptionText { get; private set; } = string.Empty;
        public string StatusName { get; private set; } = "Unknown";

        public string RunButtonText => this.IsRunning? "Stop" : "Run";

        private BitmapImage _bitmap;
        public BitmapImage Bitmap
        {
            get
            {
                if (this.IsRunning == false)
                    return null;

                return this._bitmap;
            }
            set 
            {
                this._bitmap = value; 
                this.OnPropertyChanged(nameof(this.FrameBackgroundBrush)); 
            }
        }

        public Dictionary<string, Model.Sprite> Sprites { get; private set; } = new Dictionary<string, Model.Sprite>();
        public PythonDictionary sprites => Sprites.ToPythonDictionary();

        private Mutex _sourceFrameLock = new Mutex();
        private Mat _sourceFrame = null;
        public Mat Frame
        {
            get
            {
                try
                {
                    this._sourceFrameLock.WaitOne();
                    if (this._sourceFrame == null)
                        return null;

                    var mat = new Mat();
                    this._sourceFrame.CopyTo(mat);
                    return mat;
                }
                finally
                {
                    this._sourceFrameLock.ReleaseMutex();
                }
            }
            set
            {
                this._sourceFrameLock.WaitOne();
                if (this._sourceFrame != null)
                    this._sourceFrame.Dispose();

                this._sourceFrame = value;
                this._sourceFrameLock.ReleaseMutex();
            }
        }

        public ObservableCollection<LogViewModel> Logs { get; private set; } = new ObservableCollection<LogViewModel>();

        public Visibility DarkBackgroundVisibility { get; private set; } = Visibility.Hidden;

        public ICommand SetMinimizeCommand { get; private set; }
        public ICommand SetMaximizeCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand OptionCommand { get; private set; }
        public ICommand EditSpriteCommand { get; private set; }
        public ICommand RunCommand { get; private set; }
        public ICommand CompleteCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SelectResourceFileCommand { get; private set; }
        public ICommand BrowsePythonDirectoryCommand { get; private set; }
        public ICommand SelectedRectCommand { get; private set; }
        public ICommand CreateSpriteCommand { get; private set; }
        public ICommand CancelSpriteCommand { get; private set; }
        public ICommand DeleteSpriteCommand { get; private set; }
        public ICommand DeleteStatusCommand { get; private set; }

        public MainWindowViewModel(MainWindow mainWindow)
        {
            this.MainWindow = mainWindow;
            this.OptionViewModel.Load();

            this.SetMinimizeCommand = new RelayCommand(this.OnSetMinimize);
            this.SetMaximizeCommand = new RelayCommand(this.OnSetMaximize);
            this.CloseCommand = new RelayCommand(this.OnClose);
            this.OptionCommand = new RelayCommand(this.OnOption);
            this.EditSpriteCommand = new RelayCommand(this.OnEditSprite);
            this.RunCommand = new RelayCommand(this.OnRun);

            this.CompleteCommand = new RelayCommand(this.OnComplete);
            this.CancelCommand = new RelayCommand(this.OnCancel);
            this.SelectResourceFileCommand = new RelayCommand(this.OnSelectSpriteCommand);
            this.BrowsePythonDirectoryCommand = new RelayCommand(this.OnBrowsePythonDirectory);
            this.SelectedRectCommand = new RelayCommand(this.OnSelectedRect);
            this.CancelSpriteCommand = new RelayCommand(this.OnCancelSprite);
        }

        private void OnEditSprite(object obj)
        {
            if (this._spriteDialog != null)
                return;

            this._spriteDialog = new Dialog.SpriteDialog
            {
                DataContext = new ViewModel.SpriteDialog(this.Sprites.Values)
                {
                    CompleteCommand = new RelayCommand(this.OnSpriteChanged),
                    CancelCommand = new RelayCommand(this.OnCancelSprite)
                }
            };

            this._spriteDialog.Show();
            this._spriteDialog.Closed += this._spriteDialog_Closed;
        }

        private void OnSelectedRect(object obj)
        {
            var parameters = obj as object[];
            var selectedRect = (System.Windows.Rect)parameters[1];
            var selectedFrame = new Mat(this.Frame, new OpenCvSharp.Rect { X = (int)selectedRect.X, Y = (int)selectedRect.Y, Width = (int)selectedRect.Width, Height = (int)selectedRect.Height });

            if (this._spriteDialog != null)
                return;

            this._spriteDialog = new Dialog.SpriteDialog
            {
                DataContext = new ViewModel.SpriteDialog(selectedFrame, this.Sprites.Values)
                { 
                    CompleteCommand = new RelayCommand(this.OnSpriteCreated),
                    CancelCommand = new RelayCommand(this.OnCancelSprite)
                }
            };

            this._spriteDialog.Show();
            this._spriteDialog.Closed += this._spriteDialog_Closed;
        }

        private void _spriteDialog_Closed(object sender, EventArgs e)
        {
            this._spriteDialog = null;
        }

        private void OnSpriteCreated(object obj)
        {
            try
            {
                if (this._spriteDialog == null)
                    return;

                var vm = obj as ViewModel.Sprite;
                var sprite = vm.Model;
                if (string.IsNullOrEmpty(vm.Name))
                    throw new Exception("스프라이트 이름을 입력해야 합니다.");

                if (this.Sprites.ContainsKey(sprite.Name))
                    throw new Exception($"{sprite.Name} : 존재하는 스프라이트 이름입니다.");

                this.Sprites[sprite.Name] = sprite;
                this.Save("sprites.dat");
                this._spriteDialog.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void OnSpriteChanged(object obj)
        {
            try
            {
                if (this._spriteDialog == null)
                    return;

                var vm = this._spriteDialog.DataContext as ViewModel.SpriteDialog;
                var nameChanged = (vm.Original.Name != vm.Sprite.Name);
                if (nameChanged)
                {
                    if (string.IsNullOrEmpty(vm.Sprite.Name))
                        throw new Exception("스프라이트 이름을 입력해야 합니다.");

                    if (this.Sprites.ContainsKey(vm.Sprite.Name))
                        throw new Exception($"{vm.Sprite.Name} : 존재하는 스프라이트 이름입니다.");
                }

                this.Sprites.Remove(vm.Original.Name);
                this.Sprites.Add(vm.Sprite.Name, vm.Sprite.Model);
                this.Save("sprites.dat");
                this._spriteDialog.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void Save(string filename = "sprites.dat")
        {
            using (var writer = new BinaryWriter(File.Open(filename, FileMode.OpenOrCreate)))
            {
                writer.Write(this.Sprites);
            }
        }

        public void Load(string filename = "sprites.dat")
        {
            if (File.Exists(filename) == false)
                throw new Exception($"{filename} 파일을 찾을 수 없습니다.");

            using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                this.Sprites = reader.ReadSprites();
            }
        }

        private void OnCancelSprite(object obj)
        {
            if (this._spriteDialog == null)
                return;

            this._spriteDialog.Close();
        }

        private void App_Frame(OpenCvSharp.Mat frame)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            this.Frame = frame;
            this.Bitmap = frame.ToBitmap();
            this._elapsedStopwatch.Stop();
            this.ElapsedTime = this.ElapsedTime + TimeSpan.FromMilliseconds(this._elapsedStopwatch.ElapsedMilliseconds);
            this._elapsedStopwatch.Restart();

            stopWatch.Stop();
            Thread.Sleep(Math.Max(0, 1000 / OptionViewModel.Model.RenderFPS - (int)stopWatch.ElapsedMilliseconds));
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
                this.LoadResources(this.OptionViewModel.Model.ResourceFile);
                this._elapsedStopwatch.Restart();
                this._idleStopwatch.Reset();
                if (this.target != null)
                    target.Frame -= this.App_Frame;

                target = Model.App.Find(this.OptionViewModel.Model.ClassName);
                target.Frame += this.App_Frame;
                target.Start();
                this.IsRunning = true;

                this.ExceptionText = string.Empty;

                Task.Run(() => 
                {
                    this.ExecPython("scripts/do.py");
                });
            }
            catch (Exception e)
            {
                if (target != null)
                {
                    target.Stop();
                    target.Frame -= this.App_Frame;
                    target = null;
                }
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

            if (target != null)
            {
                target.Stop();
                target.Frame -= this.App_Frame;
                target = null;
            }
            this.ElapsedTime = new TimeSpan();
            this._elapsedStopwatch.Reset();
            this._idleStopwatch.Reset();
            this._lastStatusName = string.Empty;
            this.IsRunning = false;
            this.ReleasePythonModule();

            //if (this._spriteWindow != null)
            //    this._spriteWindow.Close();
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
            dialog.InitialDirectory = this.OptionViewModel.ResourceFile.Content;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            this.OptionViewModel.ResourceFile.Content = dialog.FileName;
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
                throw new Exception($"{path} path does not exist.");

            if (File.Exists(Path.Combine(path, "python.exe")) == false)
                throw new Exception($"Cannot find python.exe file in '{path}'.");

            if (this._pythonRuntime != null)
                this._pythonRuntime.Shutdown();

            this._pythonRuntime = Python.CreateRuntime();
            var engine = this._pythonRuntime.GetEngine("IronPython");
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
            if(this._pythonRuntime != null)
                this._pythonRuntime.Shutdown();

            this._pythonRuntime = null;
        }

        private void LoadResources(string resourceFileName)
        {
            //this.Resource.Load(resourceFileName);

            //this.Sprite = this.Resource.Sprites.ToDict();
            //this.Status = this.Resource.Statuses.ToDict();
            //this.Detector = new Detector(this.Resource.Sprites, this.Resource.Statuses);
        }

        private void ReleaseResources()
        {
            //this.Resource.Clear();

            //this.Sprite.Clear();
            //this.Sprite = null;

            //this.Status.Clear();
            //this.Status = null;

            //this.Detector = null;
        }

        private object ExecPython(string path)
        {
            try
            {
                if (this._pythonRuntime == null)
                    return null;

                dynamic scope = this._pythonRuntime.UseFile(path);
                var ret = scope.callback(this);
                if (ret != null)
                    this.Logs.Add($"{path} return : {ret}");

                return ret;
            }
            catch (System.IO.FileNotFoundException)
            {
                this.Logs.Add($"{path} does not exists");
                return null;
            }
            catch (Exception e)
            {
                this.Logs.Add($"{path} return : {e.Message}");
                return null;
            }
            finally
            {
            }
        }

        public void Dispose()
        {
            this.OptionViewModel.Save();
            this.Stop();
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

        private Dictionary<string, Model.Sprite.DetectionResult> Detect(List<string> spriteNames, OpenCvSharp.Rect? area = null)
        {
            return spriteNames.Select(x =>
            {
                if (this.Sprites.TryGetValue(x, out var sprite) == false)
                    return null;

                return sprite;
            })
            .Where(x => x != null)
            .ToDictionary(x => x.Name, x => x.MatchTo(this.Frame, area));
        }

        public PythonDictionary Detect(PythonTuple spriteNames, double minPercentage = 0.0, PythonDictionary area = null)
        {
            while (this.IsRunning)
            {
                var areaCv = area != null ? 
                    (OpenCvSharp.Rect?)new OpenCvSharp.Rect((int)area["x"], (int)area["y"], (int)area["width"], (int)area["height"]) : 
                    null;

                var result = Detect(spriteNames.Select(x => x as string).ToList(), area: areaCv);
                result = result.Where(x => x.Value.Percentage >= minPercentage).ToDictionary(x => x.Key, x => x.Value);
                if (result.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                return result.ToDictionary(x => x.Key, x => x.Value.ToPythonDictionary())
                    .ToPythonDictionary();
            }

            return new PythonDictionary();
        }

        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}

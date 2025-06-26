using macro.Command;
using macro.Model;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace macro.ViewModel
{
    public class Option : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Model.Option Model { get; set; }

        public string OperationType
        {
            get => $"{Model.OperationType}";
            set
            {
                if (Enum.TryParse<OperationType>(value, out var x))
                    Model.OperationType = x;
            }
        }

        public string Class
        {
            get => Model.Class;
            set => Model.Class = value;
        }
        public string ClassNameException => string.IsNullOrEmpty(Class) ? "Class name cannot be empty" : string.Empty;
        public Visibility IsClassNameValid => string.IsNullOrEmpty(ClassNameException) ? Visibility.Hidden : Visibility.Visible;


        public string ResourceFilePath
        {
            get => Model.ResourceFilePath;
            set => Model.ResourceFilePath = value;
        }
        public string ResourceFilePathException
        {
            get
            {
                if (string.IsNullOrEmpty(ResourceFilePath))
                    return "Resource file path cannot be empty";

                return string.Empty;
            }
        }
        public Visibility IsResourceFilePathValid => string.IsNullOrEmpty(ResourceFilePathException) ? Visibility.Hidden : Visibility.Visible;


        public string ScriptDirectoryPath
        {
            get => Model.ScriptDirectoryPath;
            set => Model.ScriptDirectoryPath = value;
        }
        public string ScriptDirectoryPathException
        {
            get
            {
                if (string.IsNullOrEmpty(ScriptDirectoryPath))
                    return "Script directory path cannot be empty";

                return string.Empty;
            }
        }
        public Visibility IsScriptDirectoryPathValid => string.IsNullOrEmpty(ScriptDirectoryPathException) ? Visibility.Hidden : Visibility.Visible;


        public string PythonDirectoryPath
        {
            get => Model.PythonDirectoryPath;
            set => Model.PythonDirectoryPath = value;
        }
        public string PythonDirectoryPathException
        {
            get
            {
                if (string.IsNullOrEmpty(PythonDirectoryPath))
                    return "Python directory path cannot be empty";

                return string.Empty;
            }
        }
        public Visibility IsPythonDirectoryPathValid => string.IsNullOrEmpty(PythonDirectoryPathException) ? Visibility.Hidden : Visibility.Visible;

        public string InitializeScriptName
        {
            get => Model.InitializeScriptName;
            set => Model.InitializeScriptName = value;
        }
        public string InitializeScriptNameException
        {
            get
            {
                if (string.IsNullOrEmpty(InitializeScriptName))
                    return $"InitializeScriptName cannot be empty";

                return string.Empty;
            }
        }
        public Visibility InitializeScriptNameValid => string.IsNullOrEmpty(InitializeScriptNameException) ? Visibility.Hidden : Visibility.Visible;


        public string FrameScriptName
        {
            get => Model.FrameScriptName;
            set => Model.FrameScriptName = value;
        }
        public string FrameScriptNameException
        {
            get
            {
                if (string.IsNullOrEmpty(FrameScriptName))
                    return $"FrameScriptName cannot be empty";

                return string.Empty;
            }
        }
        public Visibility FrameScriptNameValid => string.IsNullOrEmpty(FrameScriptNameException) ? Visibility.Hidden : Visibility.Visible;


        public string RenderScriptName
        {
            get => Model.RenderScriptName;
            set => Model.RenderScriptName = value;
        }
        public string RenderScriptNameException
        {
            get
            {
                if (string.IsNullOrEmpty(RenderScriptName))
                    return $"RenderScriptName cannot be empty";

                return string.Empty;
            }
        }
        public Visibility RenderScriptNameValid => string.IsNullOrEmpty(RenderScriptNameException) ? Visibility.Hidden : Visibility.Visible;


        public string DisposeScriptName
        {
            get => Model.DisposeScriptName;
            set => Model.DisposeScriptName = value;
        }
        public string DisposeScriptNameException
        {
            get
            {
                if (string.IsNullOrEmpty(DisposeScriptName))
                    return $"DisposeScriptName cannot be empty";

                return string.Empty;
            }
        }
        public Visibility DisposeScriptNameValid => string.IsNullOrEmpty(DisposeScriptNameException) ? Visibility.Hidden : Visibility.Visible;


        public bool SoftwareMode
        {
            get => Model.OperationType == macro.Model.OperationType.Software;
            set => Model.OperationType = value ? macro.Model.OperationType.Software : macro.Model.OperationType.Hardware;
        }

        public bool HardwareMode
        {
            get => Model.OperationType == macro.Model.OperationType.Hardware;
            set => Model.OperationType = value ? macro.Model.OperationType.Hardware : macro.Model.OperationType.Software;
        }

        public int RenderFrame
        {
            get => Model.RenderFrame;
            set => Model.RenderFrame = value;
        }

        public int DetectFrame
        {
            get => Model.DetectFrame;
            set => Model.DetectFrame = value;
        }

        public bool IsCompletable
        {
            get
            {
                return new[]
                {
                    ClassNameException,
                    ResourceFilePathException,
                    ScriptDirectoryPathException,
                    PythonDirectoryPathException,
                    InitializeScriptNameException,
                    FrameScriptNameException,
                    RenderScriptNameException,
                    DisposeScriptNameException
                }.All(x => string.IsNullOrEmpty(x));
            }
        }

        public ICommand SelectResourceFileCommand { get; private set; }
        public ICommand SelectScriptPathCommand { get; private set; }
        public ICommand BrowsePythonDirectoryCommand { get; private set; }

        public Option(Model.Option model)
        {
            Model = model;

            SelectResourceFileCommand = new RelayCommand(OnSelectResourceFile);
            SelectScriptPathCommand = new RelayCommand(OnSelectScriptPath);
            BrowsePythonDirectoryCommand = new RelayCommand(OnBrowsePythonDirectory);
        }

        private void Option_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private void OnSelectResourceFile(object obj)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                DefaultExt = ".dat",
                Filter = "Resource (.dat)|*.dat"
            };
            if (dialog.ShowDialog() == false)
                return;

            ResourceFilePath = dialog.FileName;
        }
        private void OnSelectScriptPath(object obj)
        {
            var dialog = new CommonOpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            ScriptDirectoryPath = dialog.FileName;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScriptDirectoryPath)));
        }
        private void OnBrowsePythonDirectory(object obj)
        {
            var dialog = new CommonOpenFileDialog
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            PythonDirectoryPath = dialog.FileName;
        }
    }
}

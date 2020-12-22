using KPU_General_macro.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KPU_General_macro.ViewModel
{
    public class OptionViewModel : BaseViewModel
    {
        private List<IValidAssertable> _exceptableTextList = new List<IValidAssertable>();

        public Option Model { get; private set; } = new Option();

        public ExceptableText<string> ClassName { get; set; } = new ExceptableText<string>();
        public int RenderFPS { get; set; } = 30;
        public int DetectFPS { get; set; } = 30;
        public OperationType Operation { get; set; }
        public ExceptableText<string> ResourceFile { get; set; } = new ExceptableText<string>();
        public ExceptableText<string> PythonDirectory { get; set; } = new ExceptableText<string>();
        public ExceptableText<string> InitializeScriptName { get; set; } = new ExceptableText<string>();
        public ExceptableText<string> FrameScriptName { get; set; } = new ExceptableText<string>();
        public ExceptableText<string> RenderScriptName { get; set; } = new ExceptableText<string>();
        public ExceptableText<string> DisposeScriptName { get; set; } = new ExceptableText<string>();

        public bool Completable
        {
            get
            {
                foreach (var exceptableText in this._exceptableTextList)
                {
                    if (exceptableText.Valid() == false)
                        return false;
                }

                return true;
            }
        }

        public string OperationTypeText
        {
            get
            {
                return DestinationApp.Instance.OperationType == OperationType.Software ? "Software" : "Hardware";
            }
        }

        public OptionViewModel()
        {
            this.ClassName.Content = this.Model.ClassName;
            this.ClassName.Assert += this.ClassName_Assert;
            this.ClassName.Complete += this.ClassName_Complete;
            this._exceptableTextList.Add(this.ClassName);

            this.Operation = DestinationApp.Instance.OperationType;

            this.ResourceFile.Content = this.Model.ResourceFile;
            this.ResourceFile.Assert += this.Directory_Assert;
            this.ResourceFile.Complete += this.ResourcesDirectory_Complete;
            this._exceptableTextList.Add(this.ResourceFile);

            this.PythonDirectory.Content = this.Model.PythonDirectory;
            this.PythonDirectory.Assert += this.Directory_Assert;
            this.PythonDirectory.Complete += this.PythonDirectory_Complete;
            this._exceptableTextList.Add(this.PythonDirectory);

            this.InitializeScriptName.Content = this.Model.InitializeScriptName;
            this.InitializeScriptName.Assert += this.InitializeScriptName_Assert;
            this.InitializeScriptName.Complete += this.InitializeScriptName_Complete;
            this._exceptableTextList.Add(this.InitializeScriptName);

            this.FrameScriptName.Content = this.Model.FrameScriptName;
            this.FrameScriptName.Assert += this.FrameScriptName_Assert;
            this.FrameScriptName.Complete += this.FrameScriptName_Complete;
            this._exceptableTextList.Add(this.FrameScriptName);

            this.RenderScriptName.Content = this.Model.RenderScriptName;
            this.RenderScriptName.Assert += this.RenderScriptName_Assert; ;
            this.RenderScriptName.Complete += this.RenderScriptName_Complete; ;
            this._exceptableTextList.Add(this.RenderScriptName);

            this.DisposeScriptName.Content = this.Model.DisposeScriptName;
            this.DisposeScriptName.Assert += this.DisposeScriptName_Assert;
            this.DisposeScriptName.Complete += this.DisposeScriptName_Complete;
            this._exceptableTextList.Add(this.DisposeScriptName);
        }

        private void RenderScriptName_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.RenderScriptName));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void RenderScriptName_Assert(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception("렌더 스크립트를 설정해야 합니다.");
        }

        private void DisposeScriptName_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.DisposeScriptName));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void DisposeScriptName_Assert(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception("디스포즈 스크립트를 설정해야 합니다.");
        }

        private void FrameScriptName_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.FrameScriptName));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void FrameScriptName_Assert(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception("프레임 스크립트를 설정해야 합니다.");
        }

        private void InitializeScriptName_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.InitializeScriptName));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void InitializeScriptName_Assert(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception("초기화 스크립트를 설정해야 합니다.");
        }

        private void PythonDirectory_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.PythonDirectory));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void ResourcesDirectory_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.ResourceFile));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void Directory_Assert(string content)
        {
            if (IsValidPath(content) == false)
                throw new Exception("올바른 경로가 아닙니다.");
        }

        private void ClassName_Complete(object sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.ClassName));
            this.OnPropertyChanged(nameof(this.Completable));
        }

        private void ClassName_Assert(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception("클래스를 입력해야 합니다.");
        }

        public void Apply()
        {
            try
            {
                this.Model.ClassName = this.ClassName.Content;
                DestinationApp.Instance.OperationType = this.Operation;
                this.Model.RenderFPS = this.RenderFPS;
                this.Model.DetectFPS = this.DetectFPS;
                this.Model.ResourceFile = Path.GetFullPath(this.ResourceFile.Content);
                this.Model.PythonDirectory = Path.GetFullPath(this.PythonDirectory.Content);
                this.Model.InitializeScriptName = this.InitializeScriptName.Content;
                this.Model.FrameScriptName = this.FrameScriptName.Content;
                this.Model.DisposeScriptName = this.DisposeScriptName.Content;

                this.OnPropertyChanged(nameof(this.OperationTypeText));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ApplyToVModel()
        {
            this.ClassName.Content = this.Model.ClassName;
            this.Operation = DestinationApp.Instance.OperationType;
            this.RenderFPS = this.Model.RenderFPS;
            this.DetectFPS = this.Model.DetectFPS;
            this.ResourceFile.Content = this.Model.ResourceFile;
            this.PythonDirectory.Content = this.Model.PythonDirectory;
            this.InitializeScriptName.Content = this.Model.InitializeScriptName;
            this.FrameScriptName.Content = this.Model.FrameScriptName;
            this.DisposeScriptName.Content = this.Model.DisposeScriptName;
        }

        private static bool IsValidPath(string path)
        {
            var driveCheck = new Regex(@"^[a-zA-Z]:\\$");
            if (path.Length < 3)
                return false;

            if (!driveCheck.IsMatch(path.Substring(0, 3)))
                return false;

            var strTheseAreInvalidFileNameChars = new string(Path.GetInvalidPathChars());
            strTheseAreInvalidFileNameChars += @":/?*" + "\"";
            var containsABadCharacter = new Regex("[" + Regex.Escape(strTheseAreInvalidFileNameChars) + "]");
            if (containsABadCharacter.IsMatch(path.Substring(3, path.Length - 3)))
                return false;

            return true;
        }

        public void Save(string filename = "config.dat")
        {
            this.Model.Save(filename);
        }

        public bool Load(string filename = "config.dat")
        {
            var result = this.Model.Load(filename);
            this.ApplyToVModel();
            return result;
        }
    }
}

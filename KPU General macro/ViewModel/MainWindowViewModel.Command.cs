using KPU_General_macro.Dialog;
using KPU_General_macro.Model;
using KPU_General_macro.ViewModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KPU_General_macro
{
    public partial class MainWindowViewModel
    {
        public ICommand SetMinimizeCommand { get; private set; }
        public ICommand SetMaximizeCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }
        public ICommand OptionCommand { get; private set; }
        public ICommand EditResourceCommand { get; private set; }
        public ICommand RunCommand { get; private set; }
        public ICommand CompleteCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SelectResourceFileCommand { get; private set; }
        public ICommand BrowsePythonDirectoryCommand { get; private set; }
        public ICommand SelectedRectCommand { get; private set; }
        public ICommand CreateSpriteCommand { get; private set; }
        public ICommand CancelSpriteCommand { get; private set; }
        public ICommand ChangedColorCommand { get; private set; }
        public ICommand BindSpriteCommand { get; private set; }
        public ICommand UnbindSpriteCommand { get; private set; }
        public ICommand CreateStatusCommand { get; private set; }
        public ICommand SelectedSpriteChangedCommand { get; private set; }
        public ICommand ModifySpriteCommand { get; private set; }
        public ICommand SelectedStatusChangedCommand { get; private set; }
        public ICommand ModifyStatusCommand { get; private set; }
        public ICommand DeleteSpriteCommand { get; private set; }
        public ICommand DeleteStatusCommand { get; private set; }
        public ICommand GenerateScriptCommand { get; private set; }

        private void OnGenerateScript(object obj)
        {
            var dataContext = obj as ResourceWindowViewModel;

            try
            {
                if (string.IsNullOrEmpty(dataContext.StatusVM.Name))
                    throw new Exception("이름을 입력하세요. Status 이름을 기반으로 스크립트가 생성됩니다.");

                var component = dataContext.StatusVM.Components.FirstOrDefault() ??
                    throw new Exception("선택된 컴포넌트가 없습니다.");

                var code = $@"
# -*- coding: utf-8 -*-
def callback(vmodel, frame, parameter):
	vmodel.App.Click(parameter['{component.Sprite.Name}'], True)
";

                var scriptName = $"{dataContext.StatusVM.Name.ToLower()}.py";
                File.WriteAllText($"scripts/{scriptName}", code);
                MessageBox.Show("생성했습니다.", "SUCCESS");

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "ERROR");
            }
        }

        private void OnModifyStatus(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var status = parameters[1] as Status;

            if (status == null)
                return;

            this.Resource.Statuses.Remove(status.Name);
            this.OnCreateStatus(new object[] { dataContext });
            dataContext.StatusVM.Components.Clear();
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));
        }

        private void OnSelectedStatusChanged(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var status = parameters[1] as Status;

            if (status == null)
            {
                dataContext.StatusVM.Name = string.Empty;
                dataContext.StatusVM.Components.Clear();
            }
            else
            {
                dataContext.StatusVM.Name = status.Name;
                dataContext.StatusVM.Components.Clear();
                dataContext.StatusVM.Components.AddRange(status.Components);
            }
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));
        }

        private void OnModifySprite(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var sprite = parameters[1] as Sprite;

            if (sprite == null)
                return;

            this.Resource.Sprites.Remove(sprite.Name);
            this.OnCreateSprite(new object[] { dataContext });
            dataContext.SpriteVM.Frame = null;
        }

        private void OnSelectedSpriteChanged(object obj)
        {
            try
            {
                var parameters = obj as object[];
                var dataContext = parameters[0] as ResourceWindowViewModel;
                var sprite = parameters[1] as Sprite;
                if (sprite == null)
                    return;

                dataContext.SpriteVM.Name = sprite.Name;
                dataContext.SpriteVM.Frame = sprite.Frame;
                dataContext.SpriteVM.Threshold = sprite.Threshold;
                if (sprite.Color != null)
                {
                    dataContext.SpriteVM.Color = ColorTranslator.ToHtml(sprite.Color.Value);
                    dataContext.SpriteVM.ColorErrorFactor = sprite.ErrorFactor;
                }
                else
                {
                    dataContext.SpriteVM.Color = string.Empty;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void OnEditResource(object obj)
        {
            if (this.IsRunning == false)
                return;

            if (this._spriteWindow != null)
                return;

            this._spriteWindow = new SpriteWindow(SpriteWindow.EditMode.Modify)
            {
                Owner = this.MainWindow,
                ModifySpriteCommand = this.ModifySpriteCommand,
                ColorChangedCommand = this.ChangedColorCommand,
                BindSpriteCommand = this.BindSpriteCommand,
                UnbindSpriteCommand = this.UnbindSpriteCommand,
                SelectedSpriteChangedCommand = this.SelectedSpriteChangedCommand,
                SelectedStatusChangedCommand = this.SelectedStatusChangedCommand,

                CreateStatusCommand = this.CreateStatusCommand,
                ModifyStatusCommand = this.ModifyStatusCommand,
                DeleteSpriteCommand = this.DeleteSpriteCommand,
                DeleteStatusCommand = this.DeleteStatusCommand,
                GenerateScriptCommand = this.GenerateScriptCommand,
                DataContext = new ResourceWindowViewModel(this.Resource),
            };

            this._spriteWindow.Closed += this._spriteWindow_Closed;
            this._spriteWindow.Show();
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

        private void OnUnbindSprite(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var sprite = parameters[1] as Sprite;
            var isRequirement = (bool)parameters[2];

            var component = dataContext.StatusVM.Components.Find(x => x.Sprite == sprite);
            dataContext.StatusVM.Components.Remove(component);
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));

            this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
        }

        private void OnBindSprite(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var sprite = parameters[1] as Sprite;
            var isRequirement = (bool)parameters[2];

            dataContext.StatusVM.Components.Add(new Status.Component(sprite, isRequirement));
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));

            this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
        }

        private void OnChangedCommand(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var color = (System.Drawing.Color)parameters[1];

            var spriteWindowVM = dataContext.SpriteVM;
            spriteWindowVM.Color = ColorTranslator.ToHtml(color);
        }

        private void OnCancelSprite(object obj)
        {
            throw new NotImplementedException();
        }

        private void OnCreateSprite(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            try
            {
                if (string.IsNullOrEmpty(dataContext.SpriteVM.Name))
                    throw new Exception("이름을 입력하세요.");

                if (this.Resource.Sprites.ContainsKey(dataContext.SpriteVM.Name))
                    throw new Exception("이미 존재하는 스프라이트 이름입니다.");

                if (string.IsNullOrEmpty(dataContext.SpriteVM.Color))
                    this.Resource.Sprites.Add(dataContext.SpriteVM.Name, new Sprite(dataContext.SpriteVM.Name, dataContext.SpriteVM.Frame.ToBytes(), (float)dataContext.SpriteVM.Threshold));
                else
                    this.Resource.Sprites.Add(dataContext.SpriteVM.Name, new Sprite(dataContext.SpriteVM.Name, dataContext.SpriteVM.Frame.ToBytes(), (float)dataContext.SpriteVM.Threshold, ColorTranslator.FromHtml(dataContext.SpriteVM.Color), (float)dataContext.SpriteVM.ColorErrorFactor));

                dataContext.SpriteVM.OnPropertyChanged(nameof(this.Resource.Sprites));
                dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));

                dataContext.SpriteVM.Name = string.Empty;
                dataContext.SpriteVM.Threshold = SpriteWindowViewModel.INIT_THRESHOLD_VALUE;
                dataContext.SpriteVM.Color = string.Empty;

                this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void OnSelectedRect(object obj)
        {
            var parameters = obj as object[];
            var selectedRect = (System.Windows.Rect)parameters[1];
            var selectedFrame = new Mat(this.SourceFrame, new OpenCvSharp.Rect { X = (int)selectedRect.X, Y = (int)selectedRect.Y, Width = (int)selectedRect.Width, Height = (int)selectedRect.Height });

            var resourceViewModel = new ResourceWindowViewModel(this.Resource);
            resourceViewModel.SpriteVM.Frame = selectedFrame;

            if (this._spriteWindow != null)
                return;

            this._spriteWindow = new SpriteWindow(SpriteWindow.EditMode.Create)
            {
                Owner = this.MainWindow,
                CreateSpriteCommand = this.CreateSpriteCommand,
                ColorChangedCommand = this.ChangedColorCommand,
                BindSpriteCommand = this.BindSpriteCommand,
                UnbindSpriteCommand = this.UnbindSpriteCommand,

                CreateStatusCommand = this.CreateStatusCommand,
                DeleteSpriteCommand = this.DeleteSpriteCommand,
                DeleteStatusCommand = this.DeleteStatusCommand,
                GenerateScriptCommand = this.GenerateScriptCommand,
                SelectedStatusChangedCommand = this.SelectedStatusChangedCommand,

                ModifyStatusCommand = this.ModifyStatusCommand,
                DataContext = resourceViewModel
            };
            this._spriteWindow.Closed += this._spriteWindow_Closed;
            this._spriteWindow.Show();
        }

        private void OnDeleteSprite(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var sprite = parameters[1] as Sprite;

            foreach (var status in this.Resource.Statuses.Values)
            {
                status.Components
                    .Where(x => x.Sprite == sprite).ToList()
                    .ForEach(x =>
                    {
                        status.Components.Remove(x);
                    });
            }

            dataContext.StatusVM.Components
                .Where(x => x.Sprite == sprite).ToList()
                .ForEach(x =>
                {
                    dataContext.StatusVM.Components.Remove(x);
                });

            this.Resource.Sprites.Remove(sprite.Name);
            dataContext.SpriteVM.OnPropertyChanged(nameof(dataContext.SpriteVM.Sprites));
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));

            this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
        }

        private void OnDeleteStatus(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;
            var status = parameters[1] as Status;

            this.Resource.Statuses.Remove(status.Name);
            dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Statuses));

            this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
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

        private void OnCreateStatus(object obj)
        {
            var parameters = obj as object[];
            var dataContext = parameters[0] as ResourceWindowViewModel;

            try
            {
                if (string.IsNullOrEmpty(dataContext.StatusVM.Name))
                    throw new Exception("상태 이름을 입력하세요.");

                if (this.Resource.Statuses.ContainsKey(dataContext.StatusVM.Name))
                {
                    if (MessageBox.Show("이미 존재하는 상태입니다. 덮어쓰시겠습니까?", "경고", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                        return;

                    this.Resource.Statuses.Remove(dataContext.StatusVM.Name);
                }

                var createdStatus = new Status(dataContext.StatusVM.Name);
                createdStatus.Components.AddRange(dataContext.StatusVM.Components);
                this.Resource.Statuses.Add(dataContext.StatusVM.Name, createdStatus);

                dataContext.StatusVM.Name = string.Empty;
                dataContext.StatusVM.Components.Clear();
                dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Components));
                dataContext.StatusVM.OnPropertyChanged(nameof(dataContext.StatusVM.Statuses));

                this.Resource.Save(this.OptionViewModel.ResourceFile.Content);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}

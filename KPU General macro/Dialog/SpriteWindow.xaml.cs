using KPU_General_macro.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KPU_General_macro.Dialog
{
    /// <summary>
    /// Interaction logic for SpriteWindow.xaml
    /// </summary>
    public partial class SpriteWindow : Window
    {
        public enum EditMode { Create, Modify }

        public EditMode Mode { get; private set; }

        public string CompleteSpriteButtonText
        {
            get { return this.Mode == EditMode.Create ? "추가" : "수정"; }
        }

        private bool _dragging = false;
        private ListView _sourceListView;

        public static readonly DependencyProperty CreateSpriteCommandProperty = DependencyProperty.Register("CreateSpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand CreateSpriteCommand
        {
            get { return (ICommand)GetValue(CreateSpriteCommandProperty); }
            set { SetValue(CreateSpriteCommandProperty, value); }
        }

        public static readonly DependencyProperty ModifySpriteCommandProperty = DependencyProperty.Register("ModifySpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand ModifySpriteCommand
        {
            get { return (ICommand)GetValue(ModifySpriteCommandProperty); }
            set { SetValue(ModifySpriteCommandProperty, value); }
        }

        public static readonly DependencyProperty ColorChangedCommandProperty = DependencyProperty.Register("ColorChangedCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand ColorChangedCommand
        {
            get { return (ICommand)GetValue(ColorChangedCommandProperty); }
            set { SetValue(ColorChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty BindSpriteCommandProperty = DependencyProperty.Register("BindSpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand BindSpriteCommand
        {
            get { return (ICommand)GetValue(BindSpriteCommandProperty); }
            set { SetValue(BindSpriteCommandProperty, value); }
        }

        public static readonly DependencyProperty UnbindSpriteCommandProperty = DependencyProperty.Register("UnbindSpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand UnbindSpriteCommand
        {
            get { return (ICommand)GetValue(UnbindSpriteCommandProperty); }
            set { SetValue(UnbindSpriteCommandProperty, value); }
        }

        public static readonly DependencyProperty CreateStatusCommandProperty = DependencyProperty.Register("CreateStatusCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand CreateStatusCommand
        {
            get { return (ICommand)GetValue(CreateStatusCommandProperty); }
            set { SetValue(CreateStatusCommandProperty, value); }
        }

        public static readonly DependencyProperty ModifyStatusCommandProperty = DependencyProperty.Register("ModifyStatusCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand ModifyStatusCommand
        {
            get { return (ICommand)GetValue(ModifyStatusCommandProperty); }
            set { SetValue(ModifyStatusCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectedSpriteChangedCommandProperty = DependencyProperty.Register("SelectedSpriteChangedCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand SelectedSpriteChangedCommand
        {
            get { return (ICommand)GetValue(SelectedSpriteChangedCommandProperty); }
            set { SetValue(SelectedSpriteChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty SelectedStatusChangedCommandProperty = DependencyProperty.Register("SelectedStatusChangedCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand SelectedStatusChangedCommand
        {
            get { return (ICommand)GetValue(SelectedStatusChangedCommandProperty); }
            set { SetValue(SelectedStatusChangedCommandProperty, value); }
        }

        public static readonly DependencyProperty DeleteSpriteCommandProperty = DependencyProperty.Register("DeleteSpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand DeleteSpriteCommand
        {
            get { return (ICommand)GetValue(DeleteSpriteCommandProperty); }
            set { SetValue(DeleteSpriteCommandProperty, value); }
        }

        public static readonly DependencyProperty DeleteStatusCommandProperty = DependencyProperty.Register("DeleteStatusCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand DeleteStatusCommand
        {
            get { return (ICommand)GetValue(DeleteStatusCommandProperty); }
            set { SetValue(DeleteStatusCommandProperty, value); }
        }

        public static readonly DependencyProperty GenerateScriptCommandProperty = DependencyProperty.Register("GenerateScriptCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand GenerateScriptCommand
        {
            get { return (ICommand)GetValue(GenerateScriptCommandProperty); }
            set { SetValue(GenerateScriptCommandProperty, value); }
        }


        public SpriteWindow(EditMode mode)
        {
            InitializeComponent();
            this.Mode = mode;
        }

        private void OnCompleteSprite(object sender, RoutedEventArgs e)
        {
            object[] parameters;
            switch (this.Mode)
            {
                case EditMode.Create:
                    parameters = new object[] { this.DataContext };
                    if (this.CreateSpriteCommand.CanExecute(parameters))
                        this.CreateSpriteCommand.Execute(parameters);
                    break;

                case EditMode.Modify:
                    parameters = new object[] { this.DataContext, this.SpriteListView.SelectedValue };
                    if (this.ModifySpriteCommand.CanExecute(parameters))
                    {
                        this.ModifySpriteCommand.Execute(parameters);
                        this.SpriteListView.SelectedIndex = -1;
                    }
                    break;
            }
        }

        private void OnCancelSprite(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SpriteThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void OnBrowseColor(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.ColorDialog { AllowFullOpen = true };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var parameters = new object[] { this.DataContext, dialog.Color };
            if(this.ColorChangedCommand.CanExecute(parameters))
                this.ColorChangedCommand.Execute(parameters);
        }

        private void SpriteListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listview = sender as ListView;
            var element = listview.InputHitTest(e.GetPosition(listview));
            var item = FindAncestor<ListViewItem>(element as DependencyObject);

            if (item != null)
            {
                this._dragging = true;
                this._sourceListView = listview;
            }
        }

        private static T FindAncestor<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            do
            {
                if (dependencyObject is T)
                    return (T)dependencyObject;

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            } while (dependencyObject != null);

            return null;
        }

        private void SpriteListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (this._dragging == false)
                return;

            var listview = sender as SpriteListView;
            var item = FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject);
            if (item == null)
                return;

            DragDrop.DoDragDrop(item, new DataObject("sprite", item.DataContext), DragDropEffects.Move);
        }

        private void SpriteListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("sprite") == false)
                return;
            var data = e.Data.GetData("sprite");

            this._dragging = false;

            var destListView = sender as ListView;
            if (this._sourceListView == destListView)
                return;

            if (data is Sprite sprite)
                this.OnSpriteMoved(destListView, sprite);
            else if (data is Status.Component component)
                this.OnSpriteMoved(destListView, component.Sprite);
        }

        private void SpriteListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sourceListView = sender as ListView;
            var destListView = sourceListView == this.bindedStatusComponentListView ? this.unbindedSpriteListView as ListView : this.bindedStatusComponentListView as ListView;
            if (sourceListView.SelectedValue is Sprite sprite)
                this.OnSpriteMoved(destListView, sprite);
            else if (sourceListView.SelectedValue is Status.Component component)
                this.OnSpriteMoved(destListView, component.Sprite);
        }

        private void CreateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var parameters = new object[] { this.DataContext };
            if (this.CreateStatusCommand.CanExecute(parameters))
                this.CreateStatusCommand.Execute(parameters);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool OnSpriteMoved(ListView to, Sprite sprite)
        {
            var isRequirement = MessageBoxResult.Yes;
            if (to == this.bindedStatusComponentListView)
            {
                isRequirement = MessageBox.Show("Requirement?", "Requirement", MessageBoxButton.YesNoCancel);
                if (isRequirement == MessageBoxResult.Cancel)
                    return false;
            }

            var parameters = new object[] { this.DataContext, sprite, isRequirement == MessageBoxResult.Yes };
            if (to == this.bindedStatusComponentListView)
            {
                if (this.BindSpriteCommand.CanExecute(parameters))
                    this.BindSpriteCommand.Execute(parameters);
            }
            else
            {
                if (this.UnbindSpriteCommand.CanExecute(parameters))
                    this.UnbindSpriteCommand.Execute(parameters);
            }

            return true;
        }

        private void SpriteListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.Mode == EditMode.Create)
                return;

            var spriteListView = sender as SpriteListView;
            var sprite = spriteListView.SelectedValue;

            var parameters = new object[] { this.DataContext, sprite };
            if (this.SelectedSpriteChangedCommand.CanExecute(parameters))
                this.SelectedSpriteChangedCommand.Execute(parameters);
        }

        private void StatusListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ModifyStatusButton.IsEnabled = (this.StatusListView.SelectedIndex != -1);

            var status = StatusListView.SelectedValue;

            var parameters = new object[] { this.DataContext, status };
            if (this.SelectedStatusChangedCommand.CanExecute(parameters))
                this.SelectedStatusChangedCommand.Execute(parameters);
        }

        private void ModifyStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var status = this.StatusListView.SelectedValue as Status;
            if (status == null)
                return;

            var parameters = new object[] { this.DataContext, status };
            if (this.ModifyStatusCommand.CanExecute(parameters))
                this.ModifyStatusCommand.Execute(parameters);
        }

        private void GenerateScript_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}

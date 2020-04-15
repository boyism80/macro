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
        private bool _dragging = false;
        private SpriteListView _sourceListView;


        public static readonly DependencyProperty CompleteSpriteCommandProperty = DependencyProperty.Register("CompleteSpriteCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand CompleteSpriteCommand
        {
            get { return (ICommand)GetValue(CompleteSpriteCommandProperty); }
            set { SetValue(CompleteSpriteCommandProperty, value); }
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

        public static readonly DependencyProperty CompleteStatusCommandProperty = DependencyProperty.Register("CompleteStatusCommand", typeof(ICommand), typeof(SpriteWindow));
        public ICommand CompleteStatusCommand
        {
            get { return (ICommand)GetValue(CompleteStatusCommandProperty); }
            set { SetValue(CompleteStatusCommandProperty, value); }
        }

        public SpriteWindow()
        {
            InitializeComponent();
        }

        private void OnCompleteSprite(object sender, RoutedEventArgs e)
        {
            if (this.CompleteSpriteCommand.CanExecute(this.DataContext))
                this.CompleteSpriteCommand.Execute(this.DataContext);
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
            var listview = sender as SpriteListView;
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

            //this._draggingIndex = listview.Items.IndexOf(item.DataContext);
            DragDrop.DoDragDrop(item, new DataObject("sprite", item.DataContext), DragDropEffects.Move);
        }

        private void SpriteListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("sprite") == false)
                return;
            var sprite = e.Data.GetData("sprite") as Sprite;


            var destListView = sender as SpriteListView;
            if (this._sourceListView == destListView)
                return;

            var parameters = new object[] { this.DataContext, sprite };
            if (destListView == this.bindedSpriteListView)
            {
                if (this.BindSpriteCommand.CanExecute(parameters))
                    this.BindSpriteCommand.Execute(parameters);
            }
            else
            {
                if (this.UnbindSpriteCommand.CanExecute(parameters))
                    this.UnbindSpriteCommand.Execute(parameters);
            }

            this._dragging = false;
        }

        private void SpriteListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listview = sender as SpriteListView;
            var sprite = listview.SelectedValue as Sprite;

            var parameters = new object[] { this.DataContext, sprite };
            if (listview == this.bindedSpriteListView)
            {
                if (this.UnbindSpriteCommand.CanExecute(parameters))
                    this.UnbindSpriteCommand.Execute(parameters);
            }
            else
            {
                if (this.BindSpriteCommand.CanExecute(parameters))
                    this.BindSpriteCommand.Execute(parameters);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CompleteStatusCommand.CanExecute(this.DataContext))
                this.CompleteStatusCommand.Execute(this.DataContext);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

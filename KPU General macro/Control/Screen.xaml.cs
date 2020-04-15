using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for Screen.xaml
    /// </summary>
    public partial class Screen : UserControl, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public Size Size { get; private set; } = new Size();
        public Point Begin { get; private set; } = new Point();
        public bool Dragging { get; private set; } = false;
        public Visibility SelectedRectVisibility { get; set; } = Visibility.Hidden;

        public static readonly DependencyProperty FrameProperty = DependencyProperty.Register("Frame", typeof(BitmapImage), typeof(Screen));
        public static readonly DependencyProperty SelectRectCommandProperty = DependencyProperty.Register("SelectRectCommand", typeof(ICommand), typeof(Screen));

        public BitmapImage Frame
        {
            get { return (BitmapImage)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }

        public ICommand SelectRectCommand
        {
            get { return (ICommand)GetValue(SelectRectCommandProperty); }
            set { SetValue(SelectRectCommandProperty, value); }
        }

        public Screen()
        {
            InitializeComponent();
        }

        private void Screen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (this.Dragging)
                return;

            this.Dragging = true;
            this.Begin = e.GetPosition(this);
            this.Size = new Size();
            this.SelectedRectVisibility = Visibility.Visible;
        }

        private void Screen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
                return;

            if (this.Dragging == false)
                return;

            if (this.SelectRectCommand.CanExecute(this))
                this.SelectRectCommand.Execute(new object[] { this.DataContext, new Rect(this.Begin, this.Size), this.RenderSize });

            this.Dragging = false;
            this.Begin = new Point();
            this.Size = new Size();
            this.SelectedRectVisibility = Visibility.Hidden;
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.Dragging == false)
                return;

            var coordination = e.GetPosition(this);
            var moved = new Size(Math.Abs(coordination.X - this.Begin.X), Math.Abs(coordination.Y - this.Begin.Y));

            if (coordination.X <= this.Begin.X)
            {
                this.Begin = new Point(coordination.X, this.Begin.Y);
                this.Size = new Size(this.Size.Width + moved.Width, Size.Height);
            }
            else
            {
                this.Size = new Size(coordination.X - this.Begin.X, this.Size.Height);
            }

            if (coordination.Y <= this.Begin.Y)
            {
                this.Begin = new Point(this.Begin.X, coordination.Y);
                this.Size = new Size(this.Size.Width, this.Size.Height + moved.Height);
            }
            else
            {
                this.Size = new Size(this.Size.Width, coordination.Y - this.Begin.Y);
            }
            this.InvalidateVisual();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

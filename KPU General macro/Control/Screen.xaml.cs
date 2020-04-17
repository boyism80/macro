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

        public Visibility RectVisibility
        {
            get { return this.Frame != null ? Visibility.Visible : Visibility.Hidden; }
        }

        private double Ratio
        {
            get
            {
                if (this.Frame.Width > this.Frame.Height)
                    return (this.ActualWidth / this.Frame.Width);
                else
                    return (this.ActualHeight / this.Frame.Height);
            }
        }

        private Rect ValidRect
        {
            get
            {
                try
                {
                    if (this.Frame == null)
                        throw new Exception();

                    var actualFrameSize = new Size
                    {
                        Width = this.Ratio * this.Frame.Width,
                        Height = this.Ratio * this.Frame.Height
                    };

                    var padding = new Point
                    {
                        X = (this.ActualWidth - actualFrameSize.Width) / 2.0,
                        Y = (this.ActualHeight - actualFrameSize.Height) / 2.0
                    };

                    return new Rect(padding, actualFrameSize);
                }
                catch
                {
                    return new Rect();
                }
            }
        }

        public Rect SelectedRect
        {
            get
            {
                var validRect = this.ValidRect;
                var selectedRect = new Rect(this.Begin, this.Size);

                if (selectedRect.Left < validRect.Left)
                {
                    selectedRect.X = validRect.Left;
                    selectedRect.Width = Math.Max(0, this.Size.Width - (validRect.Left - this.Begin.X));
                }

                if (selectedRect.Left > validRect.Right)
                {
                    selectedRect.X = validRect.Right;
                    selectedRect.Width = 0;
                }

                if (selectedRect.Top < validRect.Top)
                {
                    selectedRect.Y = validRect.Top;
                    selectedRect.Height = Math.Max(0, this.Size.Height - (validRect.Top - this.Begin.Y));
                }

                if (selectedRect.Top > validRect.Bottom)
                {
                    selectedRect.Y = validRect.Bottom;
                    selectedRect.Height = 0;
                }

                if (selectedRect.Right > validRect.Right)
                    selectedRect.Width -= (selectedRect.Right - validRect.Right);

                if (selectedRect.Bottom > validRect.Bottom)
                    selectedRect.Height -= (validRect.Bottom - validRect.Bottom);

                return selectedRect;
            }
        }

        private Rect SelectedFrameRect
        {
            get
            {
                var validRect = this.ValidRect;
                var selectedRect = this.SelectedRect;
                
                selectedRect.X -= validRect.X;
                selectedRect.Y -= validRect.Y;

                return new Rect
                {
                    X = selectedRect.X / this.Ratio,
                    Y = selectedRect.Y / this.Ratio,
                    Width = selectedRect.Width / this.Ratio,
                    Height = selectedRect.Height / this.Ratio,
                };
            }
        }

        public Size Size { get; private set; } = new Size();
        public Point Begin { get; private set; } = new Point();
        public bool Dragging { get; private set; } = false;
        public Visibility SelectedRectVisibility { get; set; } = Visibility.Hidden;

        public static readonly DependencyProperty FrameProperty = DependencyProperty.Register("Frame", typeof(BitmapImage), typeof(Screen));
        public BitmapImage Frame
        {
            get { return (BitmapImage)GetValue(FrameProperty); }
            set 
            { 
                SetValue(FrameProperty, value);
                this.OnPropertyChanged(nameof(this.RectVisibility));
                this.OnPropertyChanged(nameof(this.SelectedRect));
            }
        }

        public static readonly DependencyProperty SelectRectCommandProperty = DependencyProperty.Register("SelectRectCommand", typeof(ICommand), typeof(Screen));
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
            if (this.Frame == null)
                return;

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
            if (this.Frame == null)
                return;

            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
                return;

            if (this.Dragging == false)
                return;

            if (this.SelectRectCommand.CanExecute(this))
                this.SelectRectCommand.Execute(new object[] { this.DataContext, this.SelectedFrameRect });

            this.Dragging = false;
            this.Begin = new Point();
            this.Size = new Size();
            this.SelectedRectVisibility = Visibility.Hidden;
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.Frame == null)
                return;

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

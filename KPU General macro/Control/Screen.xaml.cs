using KPUGeneralMacro.Extension;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KPUGeneralMacro
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

        #region Curosr
        /// <summary>
        /// 커서 위치
        /// </summary>
        private Point _cursorPoint = new Point();
        public Point CursorPoint
        {
            get { return this._cursorPoint; }
            private set
            {
                this._cursorPoint = value;
                this.OnPropertyChanged(nameof(this.CursorPointText));
            }
        }

        /// <summary>
        /// 커서 레이블 표시 여부
        /// </summary>
        private Visibility _cursorLabelVisibility = Visibility.Hidden;
        public Visibility CursorLabelVisibility
        {
            get
            {
                if (this.Bitmap == null)
                    return Visibility.Hidden;

                return this._cursorLabelVisibility;
            }
            set
            {
                this._cursorLabelVisibility = value;
            }
        }

        /// <summary>
        /// 커서 위치 텍스트
        /// </summary>
        public string CursorPointText
        {
            get
            {
                return $"{(int)this.CursorPoint.X}, {(int)this.CursorPoint.Y}";
            }
        }
        public Point PointLabelLocation { get; private set; } = new Point();
        #endregion



        #region Selection
        /// <summary>
        /// 드래그 상태
        /// </summary>
        private bool Dragging { get; set; } = false;

        /// <summary>
        /// 드래그 영역 사이즈
        /// </summary>
        private Size Size { get; set; } = new Size();

        /// <summary>
        /// 드래그 시작점
        /// </summary>
        private Point Begin { get; set; } = new Point();

        /// <summary>
        /// 선택영역을 지정할 수 있는 영역
        /// </summary>
        private Rect ValidRect
        {
            get
            {
                try
                {
                    if (this.Bitmap == null)
                        throw new Exception();

                    var actualFrameSize = new Size
                    {
                        Width = this.Ratio * this.Bitmap.Width,
                        Height = this.Ratio * this.Bitmap.Height
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

        /// <summary>
        /// 선택된 영역
        /// </summary>
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
                    selectedRect.Height -= (selectedRect.Bottom - validRect.Bottom);

                return selectedRect;
            }
        }

        /// <summary>
        /// 현재 보여지는 프레임 기준의 선택된 영역
        /// </summary>
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

        /// <summary>
        /// 선택영역 표시 여부
        /// </summary>
        public Visibility SelectedRectVisibility { get; private set; } = Visibility.Hidden;

        public MouseButton MouseButton { get; private set; }
        #endregion




        /// <summary>
        /// 프레임과 컨트롤 크기 비율
        /// </summary>
        private double Ratio
        {
            get
            {
                if (this.Bitmap.Width > this.Bitmap.Height)
                    return (this.ActualWidth / this.Bitmap.Width);
                else
                    return (this.ActualHeight / this.Bitmap.Height);
            }
        }

        public static readonly DependencyProperty BitmapProperty = DependencyProperty.Register("Bitmap", typeof(BitmapImage), typeof(Screen));
        public BitmapImage Bitmap
        {
            get { return (BitmapImage)GetValue(BitmapProperty); }
            set
            {
                SetValue(BitmapProperty, value);
                this.OnPropertyChanged(nameof(this.SelectedRect));
                this.OnPropertyChanged(nameof(this.CursorLabelVisibility));
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

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            this.CursorLabelVisibility = Visibility.Visible;
            this.UpdateCursorPoint(e.GetPosition(this));
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            this.CursorLabelVisibility = Visibility.Hidden;
        }

        private void UpdateCursorPoint(Point absolutePoint)
        {
            if (this.Bitmap == null)
                return;

            this.PointLabelLocation = absolutePoint;
            this.CursorLabelVisibility = this.ValidRect.Contains(absolutePoint) ? Visibility.Visible : Visibility.Hidden;

            var mappedPoint = new Point
            {
                X = (absolutePoint.X - this.ValidRect.X) / this.Ratio,
                Y = (absolutePoint.Y - this.ValidRect.Y) / this.Ratio,
            };

            this.CursorPoint = new Point
            {
                X = Math.Max(0, Math.Min(this.Bitmap.Width, mappedPoint.X)),
                Y = Math.Max(0, Math.Min(this.Bitmap.Height, mappedPoint.Y)),
            };
        }

        private void Screen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Bitmap == null)
                return;

            if (this.Dragging)
                return;

            this.MouseButton = e.ChangedButton;
            this.Dragging = true;
            this.Begin = e.GetPosition(this);
            this.Size = new Size();
            this.SelectedRectVisibility = Visibility.Visible;
        }

        private void Screen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.Bitmap == null)
                return;

            if (this.Dragging == false)
                return;

            if(this.SelectedFrameRect.Width > 0 && this.SelectedFrameRect.Height > 0)
                this.SelectRectCommand.Execute(new object[] { this.DataContext, this.SelectedFrameRect, e.ChangedButton });

            this.Dragging = false;
            this.Begin = new Point();
            this.Size = new Size();
            this.SelectedRectVisibility = Visibility.Hidden;
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            var coordination = e.GetPosition(this);
            this.UpdateCursorPoint(coordination);

            if (this.Bitmap == null)
                return;

            if (this.Dragging == false)
                return;

            var moved = new Size(Math.Abs(coordination.X - this.Begin.X), Math.Abs(coordination.Y - this.Begin.Y));

            if (coordination.X <= this.Begin.X)
            {
                this.Begin = new Point(coordination.X, this.Begin.Y);
                this.Size = new Size(this.Size.Width + moved.Width, this.Size.Height);
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

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace macro.Control
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
            get { return _cursorPoint; }
            private set
            {
                _cursorPoint = value;
                OnPropertyChanged(nameof(CursorPointText));
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
                if (Bitmap == null)
                    return Visibility.Hidden;

                return _cursorLabelVisibility;
            }
            set
            {
                _cursorLabelVisibility = value;
            }
        }

        /// <summary>
        /// 커서 위치 텍스트
        /// </summary>
        public string CursorPointText
        {
            get
            {
                return $"{(int)CursorPoint.X}, {(int)CursorPoint.Y}";
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
                    if (Bitmap == null)
                        throw new Exception();

                    var actualFrameSize = new Size
                    {
                        Width = Ratio * Bitmap.Width,
                        Height = Ratio * Bitmap.Height
                    };

                    var padding = new Point
                    {
                        X = (ActualWidth - actualFrameSize.Width) / 2.0,
                        Y = (ActualHeight - actualFrameSize.Height) / 2.0
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
                var validRect = ValidRect;
                var selectedRect = new Rect(Begin, Size);

                if (selectedRect.Left < validRect.Left)
                {
                    selectedRect.X = validRect.Left;
                    selectedRect.Width = Math.Max(0, Size.Width - (validRect.Left - Begin.X));
                }

                if (selectedRect.Left > validRect.Right)
                {
                    selectedRect.X = validRect.Right;
                    selectedRect.Width = 0;
                }

                if (selectedRect.Top < validRect.Top)
                {
                    selectedRect.Y = validRect.Top;
                    selectedRect.Height = Math.Max(0, Size.Height - (validRect.Top - Begin.Y));
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
                var validRect = ValidRect;
                var selectedRect = SelectedRect;

                selectedRect.X -= validRect.X;
                selectedRect.Y -= validRect.Y;

                return new Rect
                {
                    X = selectedRect.X / Ratio,
                    Y = selectedRect.Y / Ratio,
                    Width = selectedRect.Width / Ratio,
                    Height = selectedRect.Height / Ratio,
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
                if (Bitmap.Width > Bitmap.Height)
                    return (ActualWidth / Bitmap.Width);
                else
                    return (ActualHeight / Bitmap.Height);
            }
        }

        public static readonly DependencyProperty BitmapProperty = DependencyProperty.Register("Bitmap", typeof(WriteableBitmap), typeof(Screen));
        public WriteableBitmap Bitmap
        {
            get { return (WriteableBitmap)GetValue(BitmapProperty); }
            set
            {
                SetValue(BitmapProperty, value);
                OnPropertyChanged(nameof(SelectedRect));
                OnPropertyChanged(nameof(CursorLabelVisibility));

                // Force immediate visual update to prevent WPF render optimization delays
                if (value != null)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                    {
                        InvalidateVisual();
                        UpdateLayout();
                    }));
                }
            }
        }

        public static readonly DependencyProperty SelectRectCommandProperty = DependencyProperty.Register("SelectRectCommand", typeof(ICommand), typeof(Screen));
        public ICommand SelectRectCommand
        {
            get { return (ICommand)GetValue(SelectRectCommandProperty); }
            set { SetValue(SelectRectCommandProperty, value); }
        }

        private bool _isRenderingConnected = false;

        public Screen()
        {
            InitializeComponent();

            // Optimize rendering settings for better performance
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

            // Use hardware acceleration if available
            CacheMode = new BitmapCache();

            // Connect to WPF's rendering pipeline to force continuous rendering
            this.Loaded += Screen_Loaded;
            this.Unloaded += Screen_Unloaded;
        }

        private void Screen_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isRenderingConnected)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                _isRenderingConnected = true;
            }
        }

        private void Screen_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_isRenderingConnected)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                _isRenderingConnected = false;
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Force continuous rendering by invalidating visual on every frame
            // This prevents WPF from optimizing away rendering when there's no input
            if (IsVisible && Bitmap != null)
            {
                InvalidateVisual();
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            CursorLabelVisibility = Visibility.Visible;
            UpdateCursorPoint(e.GetPosition(this));
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            CursorLabelVisibility = Visibility.Hidden;
        }

        private void UpdateCursorPoint(Point absolutePoint)
        {
            if (Bitmap == null)
                return;

            PointLabelLocation = absolutePoint;
            CursorLabelVisibility = ValidRect.Contains(absolutePoint) ? Visibility.Visible : Visibility.Hidden;

            var mappedPoint = new Point
            {
                X = (absolutePoint.X - ValidRect.X) / Ratio,
                Y = (absolutePoint.Y - ValidRect.Y) / Ratio,
            };

            CursorPoint = new Point
            {
                X = Math.Max(0, Math.Min(Bitmap.Width, mappedPoint.X)),
                Y = Math.Max(0, Math.Min(Bitmap.Height, mappedPoint.Y)),
            };
        }

        private void Screen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Bitmap == null)
                return;

            if (Dragging)
                return;

            MouseButton = e.ChangedButton;
            Dragging = true;
            Begin = e.GetPosition(this);
            Size = new Size();
            SelectedRectVisibility = Visibility.Visible;
        }

        private void Screen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Bitmap == null)
                return;

            if (Dragging == false)
                return;

            if (SelectedFrameRect.Width > 0 && SelectedFrameRect.Height > 0)
                SelectRectCommand.Execute(new object[] { DataContext, SelectedFrameRect, e.ChangedButton });

            Dragging = false;
            Begin = new Point();
            Size = new Size();
            SelectedRectVisibility = Visibility.Hidden;
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            var coordination = e.GetPosition(this);
            UpdateCursorPoint(coordination);

            if (Bitmap == null)
                return;

            if (Dragging == false)
                return;

            var moved = new Size(Math.Abs(coordination.X - Begin.X), Math.Abs(coordination.Y - Begin.Y));

            if (coordination.X <= Begin.X)
            {
                Begin = new Point(coordination.X, Begin.Y);
                Size = new Size(Size.Width + moved.Width, Size.Height);
            }
            else
            {
                Size = new Size(coordination.X - Begin.X, Size.Height);
            }

            if (coordination.Y <= Begin.Y)
            {
                Begin = new Point(Begin.X, coordination.Y);
                Size = new Size(Size.Width, Size.Height + moved.Height);
            }
            else
            {
                Size = new Size(Size.Width, coordination.Y - Begin.Y);
            }
            InvalidateVisual();
        }

        public void Dispose()
        {
            // Clean up rendering connection
            if (_isRenderingConnected)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                _isRenderingConnected = false;
            }

            this.Loaded -= Screen_Loaded;
            this.Unloaded -= Screen_Unloaded;
        }
    }
}

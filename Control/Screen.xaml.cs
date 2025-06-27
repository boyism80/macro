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
    /// Interactive screen control for displaying and selecting areas on images
    /// </summary>
    public partial class Screen : UserControl, INotifyPropertyChanged, IDisposable
    {
        #region Constants

        private const int SELECTION_BORDER_RADIUS = 3;
        private const int SELECTION_BORDER_THICKNESS = 1;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Fields

        private Point _cursorPoint = new();
        private Visibility _cursorLabelVisibility = Visibility.Hidden;
        private bool _isDragging = false;
        private Size _selectionSize = new();
        private Point _selectionBegin = new();
        private bool _isRenderingConnected = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current cursor position in image coordinates
        /// </summary>
        public Point CursorPoint
        {
            get => _cursorPoint;
            private set
            {
                _cursorPoint = value;
                OnPropertyChanged(nameof(CursorPointText));
            }
        }

        /// <summary>
        /// Gets or sets the visibility of the cursor label
        /// </summary>
        public Visibility CursorLabelVisibility
        {
            get => Bitmap == null ? Visibility.Hidden : _cursorLabelVisibility;
            set => _cursorLabelVisibility = value;
        }

        /// <summary>
        /// Gets the cursor position as formatted text
        /// </summary>
        public string CursorPointText => $"{(int)CursorPoint.X}, {(int)CursorPoint.Y}";

        /// <summary>
        /// Gets the location for the point label
        /// </summary>
        public Point PointLabelLocation { get; private set; } = new();

        /// <summary>
        /// Gets whether the user is currently dragging to select an area
        /// </summary>
        public bool IsDragging
        {
            get => _isDragging;
            private set => _isDragging = value;
        }

        /// <summary>
        /// Gets the current selection size
        /// </summary>
        public Size SelectionSize
        {
            get => _selectionSize;
            private set => _selectionSize = value;
        }

        /// <summary>
        /// Gets the selection start point
        /// </summary>
        public Point SelectionBegin
        {
            get => _selectionBegin;
            private set => _selectionBegin = value;
        }

        /// <summary>
        /// Gets the valid area for selection based on bitmap dimensions and control size
        /// </summary>
        private Rect ValidRect
        {
            get
            {
                try
                {
                    if (Bitmap == null)
                        throw new InvalidOperationException("Bitmap is null");

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
        /// Gets the selected rectangle with boundary constraints
        /// </summary>
        public Rect SelectedRect
        {
            get
            {
                var validRect = ValidRect;
                var selectedRect = new Rect(SelectionBegin, SelectionSize);

                return ConstrainRectToValidArea(selectedRect, validRect);
            }
        }

        /// <summary>
        /// Converts screen coordinates to bitmap coordinates
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
        /// Gets the visibility of the selection rectangle
        /// </summary>
        public Visibility SelectedRectVisibility { get; private set; } = Visibility.Hidden;

        /// <summary>
        /// Gets the mouse button used for selection
        /// </summary>
        public MouseButton MouseButton { get; private set; }

        /// <summary>
        /// Maintains aspect ratio while scaling the bitmap to fit the control
        /// </summary>
        private double Ratio
        {
            get
            {
                if (Bitmap == null) return 1.0;

                return Bitmap.Width > Bitmap.Height
                    ? ActualWidth / Bitmap.Width
                    : ActualHeight / Bitmap.Height;
            }
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty BitmapProperty =
            DependencyProperty.Register(nameof(Bitmap), typeof(WriteableBitmap), typeof(Screen));

        /// <summary>
        /// Gets or sets the bitmap to display
        /// </summary>
        public WriteableBitmap Bitmap
        {
            get => (WriteableBitmap)GetValue(BitmapProperty);
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

        public static readonly DependencyProperty SelectRectCommandProperty =
            DependencyProperty.Register(nameof(SelectRectCommand), typeof(ICommand), typeof(Screen));

        /// <summary>
        /// Gets or sets the command to execute when a rectangle is selected
        /// </summary>
        public ICommand SelectRectCommand
        {
            get => (ICommand)GetValue(SelectRectCommandProperty);
            set => SetValue(SelectRectCommandProperty, value);
        }

        #endregion

        #region Constructor

        public Screen()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            ConnectToRenderingIfNeeded();
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            CursorLabelVisibility = Visibility.Hidden;
            OnPropertyChanged(nameof(CursorLabelVisibility));
        }

        private void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(this);
            UpdateCursorPoint(currentPosition);

            if (Bitmap == null || !IsDragging)
                return;

            var movement = CalculateMovement(currentPosition, SelectionBegin);
            SelectionSize = movement;
            OnPropertyChanged(nameof(SelectedRect));
        }

        private void Screen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Bitmap == null || IsDragging)
                return;

            StartSelection(e);
        }

        private void Screen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Bitmap == null || !IsDragging)
                return;

            CompleteSelection(e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Connects to the rendering event if not already connected
        /// </summary>
        private void ConnectToRenderingIfNeeded()
        {
            if (!_isRenderingConnected)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                _isRenderingConnected = true;
            }
        }

        /// <summary>
        /// Handles the composition target rendering event
        /// </summary>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (Bitmap != null)
            {
                OnPropertyChanged(nameof(SelectedRect));
            }
        }

        /// <summary>
        /// Updates the cursor point and label position
        /// </summary>
        /// <param name="position">Current mouse position</param>
        private void UpdateCursorPoint(Point position)
        {
            PointLabelLocation = position;
            CursorLabelVisibility = ValidRect.Contains(position) ? Visibility.Visible : Visibility.Hidden;

            var mappedPoint = new Point
            {
                X = (position.X - ValidRect.X) / Ratio,
                Y = (position.Y - ValidRect.Y) / Ratio,
            };

            CursorPoint = new Point
            {
                X = Math.Max(0, Math.Min(Bitmap?.Width ?? 0, mappedPoint.X)),
                Y = Math.Max(0, Math.Min(Bitmap?.Height ?? 0, mappedPoint.Y)),
            };
        }

        /// <summary>
        /// Starts a new selection operation
        /// </summary>
        /// <param name="e">Mouse button event args</param>
        private void StartSelection(MouseButtonEventArgs e)
        {
            MouseButton = e.ChangedButton;
            IsDragging = true;
            SelectionBegin = e.GetPosition(this);
            SelectionSize = new Size();
            SelectedRectVisibility = Visibility.Visible;
        }

        /// <summary>
        /// Completes the current selection operation
        /// </summary>
        /// <param name="e">Mouse button event args</param>
        private void CompleteSelection(MouseButtonEventArgs e)
        {
            if (HasValidSelection())
            {
                ExecuteSelectRectCommand(e);
            }

            ResetSelection();
        }

        /// <summary>
        /// Checks if the current selection is valid (has width and height)
        /// </summary>
        /// <returns>True if selection is valid</returns>
        private bool HasValidSelection()
        {
            return SelectedFrameRect.Width > 0 && SelectedFrameRect.Height > 0;
        }

        /// <summary>
        /// Executes the select rectangle command
        /// </summary>
        /// <param name="e">Mouse button event args</param>
        private void ExecuteSelectRectCommand(MouseButtonEventArgs e)
        {
            var parameters = new object[] { DataContext, SelectedFrameRect, e.ChangedButton };
            SelectRectCommand?.Execute(parameters);
        }

        /// <summary>
        /// Resets the selection state
        /// </summary>
        private void ResetSelection()
        {
            IsDragging = false;
            SelectionBegin = new Point();
            SelectionSize = new Size();
            SelectedRectVisibility = Visibility.Hidden;
        }

        /// <summary>
        /// Calculates the movement size from begin point to current point
        /// </summary>
        /// <param name="currentPoint">Current mouse position</param>
        /// <param name="beginPoint">Selection start position</param>
        /// <returns>Movement size</returns>
        private Size CalculateMovement(Point currentPoint, Point beginPoint)
        {
            return new Size(
                Math.Abs(currentPoint.X - beginPoint.X),
                Math.Abs(currentPoint.Y - beginPoint.Y));
        }

        /// <summary>
        /// Constrains a rectangle to fit within the valid area
        /// </summary>
        /// <param name="rect">Rectangle to constrain</param>
        /// <param name="validArea">Valid area bounds</param>
        /// <returns>Constrained rectangle</returns>
        private Rect ConstrainRectToValidArea(Rect rect, Rect validArea)
        {
            // Constrain left edge
            if (rect.Left < validArea.Left)
            {
                rect.X = validArea.Left;
                rect.Width = Math.Max(0, rect.Width - (validArea.Left - rect.X));
            }

            // Constrain right edge
            if (rect.Left > validArea.Right)
            {
                rect.X = validArea.Right;
                rect.Width = 0;
            }

            // Constrain top edge
            if (rect.Top < validArea.Top)
            {
                rect.Y = validArea.Top;
                rect.Height = Math.Max(0, rect.Height - (validArea.Top - rect.Y));
            }

            // Constrain bottom edge
            if (rect.Top > validArea.Bottom)
            {
                rect.Y = validArea.Bottom;
                rect.Height = 0;
            }

            // Trim width if extends beyond right edge
            if (rect.Right > validArea.Right)
                rect.Width -= rect.Right - validArea.Right;

            // Trim height if extends beyond bottom edge
            if (rect.Bottom > validArea.Bottom)
                rect.Height -= rect.Bottom - validArea.Bottom;

            return rect;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the changed property</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_isRenderingConnected)
                    {
                        CompositionTarget.Rendering -= CompositionTarget_Rendering;
                        _isRenderingConnected = false;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

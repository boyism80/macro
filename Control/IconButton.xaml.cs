using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace macro.Control
{
    /// <summary>
    /// A custom icon button control with hover effects
    /// </summary>
    public partial class IconButton : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(IconButton));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(IconButton),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register(nameof(IconWidth), typeof(int), typeof(IconButton),
                new FrameworkPropertyMetadata(16));

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register(nameof(IconHeight), typeof(int), typeof(IconButton),
                new FrameworkPropertyMetadata(16));

        public static readonly DependencyProperty HoverBackgroundProperty =
            DependencyProperty.Register(nameof(HoverBackground), typeof(SolidColorBrush), typeof(IconButton),
                new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Transparent)));

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the command to execute when the button is clicked
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon path or resource
        /// </summary>
        public string Icon
        {
            get => GetValue(IconProperty)?.ToString() ?? string.Empty;
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the icon
        /// </summary>
        public int IconWidth
        {
            get => (int)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the icon
        /// </summary>
        public int IconHeight
        {
            get => (int)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush when hovering
        /// </summary>
        public SolidColorBrush HoverBackground
        {
            get => (SolidColorBrush)GetValue(HoverBackgroundProperty);
            set => SetValue(HoverBackgroundProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the button is clicked
        /// </summary>
        public event RoutedEventHandler Click
        {
            add => button.AddHandler(ButtonBase.ClickEvent, value);
            remove => button.RemoveHandler(ButtonBase.ClickEvent, value);
        }

        #endregion

        #region Constructor

        public IconButton()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Event is handled through Command binding
        }

        #endregion
    }
}

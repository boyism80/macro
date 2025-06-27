using System.Windows;
using System.Windows.Controls;

namespace macro.Control
{
    /// <summary>
    /// Types of validation for text input
    /// </summary>
    public enum ValidTextType
    {
        Default,
        File,
        Directory
    }

    /// <summary>
    /// A text input control with built-in validation feedback
    /// </summary>
    public partial class ValidTextBlock : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ValidTextBlock),
                new FrameworkPropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValidTextTypeProperty =
            DependencyProperty.Register(nameof(ValidTextType), typeof(ValidTextType), typeof(ValidTextBlock),
                new FrameworkPropertyMetadata(Control.ValidTextType.Default));

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the text value
        /// </summary>
        public string Text
        {
            get => GetValue(TextProperty)?.ToString() ?? string.Empty;
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the validation type
        /// </summary>
        public ValidTextType ValidTextType
        {
            get => (ValidTextType)GetValue(ValidTextTypeProperty);
            set => SetValue(ValidTextTypeProperty, value);
        }

        /// <summary>
        /// Gets the validation error message, if any
        /// </summary>
        public string ExceptionText
        {
            get
            {
                if (string.IsNullOrEmpty(Text))
                    return "Cannot be empty";

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets whether the current input is valid
        /// </summary>
        public bool IsValid => string.IsNullOrEmpty(ExceptionText);

        #endregion

        #region Constructor

        public ValidTextBlock()
        {
            InitializeComponent();
        }

        #endregion
    }
}

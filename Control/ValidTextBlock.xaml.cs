using System.Windows;
using System.Windows.Controls;

namespace macro.Control
{
    public enum ValidTextType
    { 
        Default,
        File,
        Directory
    }

    /// <summary>
    /// Interaction logic for ValidTextBlock.xaml
    /// </summary>
    public partial class ValidTextBlock : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ValidTextBlock), new FrameworkPropertyMetadata(string.Empty));
        public string Text
        {
            get { return GetValue(TextProperty).ToString(); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty ValidTextTypeProperty = DependencyProperty.Register("ValidTextType", typeof(ValidTextType), typeof(ValidTextBlock), new FrameworkPropertyMetadata(macro.Control.ValidTextType.Default));
        public string ValidTextType
        {
            get { return GetValue(ValidTextTypeProperty).ToString(); }
            set { SetValue(ValidTextTypeProperty, value); }
        }

        public string ExceptionText
        {
            get
            {
                if (string.IsNullOrEmpty(Text))
                    return $"Cannot be empty";

                return string.Empty;
            }
        }

        public bool IsValid
        {
            get
            {
                return string.IsNullOrEmpty(ExceptionText);
            }
        }

        public ValidTextBlock()
        {
            InitializeComponent();
        }
    }
}

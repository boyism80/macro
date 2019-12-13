using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for HistoryControl.xaml
    /// </summary>
    public partial class HistoryControl : UserControl, IDisposable
    {
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(HistoryControl), new FrameworkPropertyMetadata(string.Empty));
        public static readonly DependencyProperty DatetimeProperty = DependencyProperty.Register("Datetime", typeof(DateTime), typeof(HistoryControl));

        public string Message
        {
            get { return GetValue(MessageProperty).ToString(); }
            set { SetValue(MessageProperty, value); }
        }

        ~HistoryControl()
        { }

        public DateTime Datetime
        {
            get { return (DateTime)GetValue(DatetimeProperty); }
            set { SetValue(DatetimeProperty, value); }
        }

        public HistoryControl()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, HistoryControl.MessageProperty);
            BindingOperations.ClearBinding(this, HistoryControl.DatetimeProperty);
            this.Container.Child = null;
            this.DataContext = null;
        }
    }
}

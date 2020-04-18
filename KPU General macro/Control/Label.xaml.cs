using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for Label.xaml
    /// </summary>
    public partial class Label : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(Label), new FrameworkPropertyMetadata(string.Empty));
        public string Text
        {
            get { return GetValue(TextProperty).ToString(); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(double), typeof(Label), new FrameworkPropertyMetadata(0.0));
        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(double), typeof(Label), new FrameworkPropertyMetadata(0.0));
        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public static readonly DependencyProperty LabelBackgroundProperty = DependencyProperty.Register("LabelBackground", typeof(SolidColorBrush), typeof(Label), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.Lime)));
        public SolidColorBrush LabelBackground
        {
            get { return (SolidColorBrush)GetValue(LabelBackgroundProperty); }
            set { SetValue(LabelBackgroundProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(Label), new FrameworkPropertyMetadata(new CornerRadius(5, 5, 5, 5)));
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public Label()
        {
            DependencyPropertyDescriptor.FromProperty(XProperty, typeof(Label)).AddValueChanged(this, OnXChanged);
            DependencyPropertyDescriptor.FromProperty(YProperty, typeof(Label)).AddValueChanged(this, OnYChanged);

            InitializeComponent();
        }

        private void OnXChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                var control = sender as Label;
                Canvas.SetLeft(control, control.X);
            }
            catch (Exception)
            { }
        }

        private void OnYChanged(object sender, EventArgs e)
        {
            try
            {
                var control = sender as Label;
                Canvas.SetTop(control, control.Y);
            }
            catch (Exception)
            { }
        }

        public void Dispose()
        {
            DependencyPropertyDescriptor.FromProperty(XProperty, typeof(Label)).RemoveValueChanged(this, OnXChanged);
            DependencyPropertyDescriptor.FromProperty(YProperty, typeof(Label)).RemoveValueChanged(this, OnYChanged);

            BindingOperations.ClearBinding(this, Label.XProperty);
            BindingOperations.ClearBinding(this, Label.YProperty);
            BindingOperations.ClearBinding(this, Label.TextProperty);
            BindingOperations.ClearBinding(this, Label.LabelBackgroundProperty);
            BindingOperations.ClearBinding(this, Label.CornerRadiusProperty);

            this.container.Child = null;
        }
    }
}

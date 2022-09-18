using System;
using System.Globalization;
using System.Windows;

namespace KPUGeneralMacro
{
    public class MainWindowIconConverter : BaseValueConverter<MainWindowIconConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var windowState = (WindowState)value;

            if (windowState == System.Windows.WindowState.Maximized)
                return "/Images/normalize.png";
            else
                return "/Images/maximize.png";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

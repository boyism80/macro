using System.Windows;
using System.Windows.Controls;

namespace macro.Control
{
    /// <summary>
    /// Interaction logic for EditResourceControl.xaml
    /// </summary>
    public partial class EditResourceControl : UserControl
    {
        public EditResourceControl()
        {
            InitializeComponent();
        }

        private void ColorPicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded == false)
                return;

            //var dataContext = DataContext as ViewModel.Sprite;
            //dataContext?.OnCapture(Frame);
        }
    }
}

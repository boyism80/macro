using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

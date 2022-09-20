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
using System.Windows.Shapes;

namespace KPUGeneralMacro.Dialog
{
    /// <summary>
    /// Interaction logic for SpriteDialog.xaml
    /// </summary>
    public partial class SpriteDialog : Window
    {
        public SpriteDialog()
        {
            InitializeComponent();
        }

        private void ColorPicker_ColorChanged(object sender, RoutedEventArgs e)
        {
            var ctx = this.DataContext as ViewModel.SpriteDialog;
            ctx.Sprite.ExtColor.MediaPivot = this.ColorPicker.SelectedColor;
        }
    }
}

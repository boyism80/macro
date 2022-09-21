using System.Windows;

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
            if (ctx == null)
                return;

            if (ctx.Sprite == null)
                return;

            ctx.Sprite.ExtColor.MediaPivot = this.ColorPicker.SelectedColor;
        }
    }
}

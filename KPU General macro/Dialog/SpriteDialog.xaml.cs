using System.Windows;

namespace KPUGeneralMacro.Dialog
{
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

        private void SpriteSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var ctx = this.DataContext as ViewModel.SpriteDialog;
            if (ctx == null)
                return;

            if (ctx.Mode == ViewModel.SpriteDialogMode.Create)
                return;

            var selected = this.Sprites.SelectedItem as ViewModel.Sprite;
            if (selected == null)
                return;

            ctx.Sprite = selected;
        }
    }
}

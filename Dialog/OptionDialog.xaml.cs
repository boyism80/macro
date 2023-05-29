using System.Windows;

namespace macro.Dialog
{
    /// <summary>
    /// Interaction logic for OptionDialog.xaml
    /// </summary>
    public partial class OptionDialog : Window
    {
        public OptionDialog()
        {
            InitializeComponent();
        }

        private void OnComplete(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataContext = DataContext as ViewModel.Option;
                if (dataContext.IsCompletable == false)
                    throw new System.Exception("완료할 수 없습니다.");

                Close();
            }
            catch (System.Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

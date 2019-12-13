using KPU_General_macro.Model;
using System.Windows;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel MainWindowViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.MainWindowViewModel = new MainWindowViewModel(this);
            this.DataContext = this.MainWindowViewModel;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            this.MainWindowViewModel.Dispose();
            DestinationApp.Instance.Stop();
        }
    }
}

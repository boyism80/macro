using KPUGeneralMacro.ViewModel;
using System;
using System.Windows;

namespace KPUGeneralMacro
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
            try
            {
                this.MainWindowViewModel.Load();
            }
            catch
            { }
            this.DataContext = this.MainWindowViewModel;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            this.MainWindowViewModel.Dispose();
            this.MainWindowViewModel.Stop();
            this.MainWindowViewModel.Save();
        }
    }
}

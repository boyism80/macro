using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for StatusListView.xaml
    /// </summary>
    public partial class StatusListView : ListView
    {
        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(StatusListView));
        public ICommand DeleteCommand
        {
            get { return (ICommand)GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        public StatusListView()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuitem = sender as MenuItem;
            var parameters = new object[] { this.DataContext, menuitem.CommandParameter };
            if (this.DeleteCommand.CanExecute(parameters))
                this.DeleteCommand.Execute(parameters);
        }
    }
}

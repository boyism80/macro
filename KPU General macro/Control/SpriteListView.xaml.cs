using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KPU_General_macro
{
    /// <summary>
    /// Interaction logic for SpriteListView.xaml
    /// </summary>
    public partial class SpriteListView : ListView
    {
        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register("DeleteCommand", typeof(ICommand), typeof(SpriteListView));
        public ICommand DeleteCommand
        {
            get { return (ICommand)GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        public static readonly DependencyProperty DeletableProperty = DependencyProperty.Register("Deletable", typeof(bool), typeof(SpriteListView), new PropertyMetadata(true));
        public bool Deletable
        {
            get { return (bool)GetValue(DeletableProperty); }
            set { SetValue(DeletableProperty, value); }
        }

        public SpriteListView()
        {
            InitializeComponent();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var parameters = new object[] { this.DataContext, button.DataContext };
            
            if (this.DeleteCommand.CanExecute(parameters))
                this.DeleteCommand.Execute(parameters);
        }
    }
}

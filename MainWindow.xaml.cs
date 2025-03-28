using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClientCentralino.Models;
using ClientCentralino.ViewModels;

namespace ClientCentralino
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var chiamataSelezionata = (Chiamata)e.AddedItems[0];
                MessageBox.Show($"Hai selezionato la chiamata ID: {chiamataSelezionata.Id}");
            }
        }

        private void MostraStatistiche(object sender, RoutedEventArgs e)
        {
            ContentArea.Visibility = Visibility.Visible;
        }
    }
}
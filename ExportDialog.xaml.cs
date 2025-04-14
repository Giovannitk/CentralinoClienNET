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

namespace ClientCentralino_vs2
{
    /// <summary>
    /// Logica di interazione per ExportDialog.xaml
    /// </summary>
    // ExportDialog.xaml.cs
    public partial class ExportDialog : Window
    {
        public ExportFormat SelectedFormat { get; private set; }

        public ExportDialog()
        {
            InitializeComponent();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SelectedFormat = RdbCsv.IsChecked == true ? ExportFormat.CSV : ExportFormat.Excel;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public enum ExportFormat
    {
        CSV,
        Excel
    }
}

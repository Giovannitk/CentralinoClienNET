using ClientCentralino_vs2.Models;
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
    public partial class IncompleteContactsWindow : Window
    {
        public Contatto ContattoSelezionato { get; private set; }

        public IncompleteContactsWindow(List<Contatto> contatti)
        {
            InitializeComponent();
            DgIncompleteContacts.ItemsSource = contatti;
        }

        private void DgIncompleteContacts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgIncompleteContacts.SelectedItem is Contatto contatto)
            {
                ContattoSelezionato = contatto;
                this.DialogResult = true; // Chiude la finestra con successo
                this.Close();
            }
        }
    }


}

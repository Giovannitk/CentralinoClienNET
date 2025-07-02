using System;
using System.Windows;
using ClientCentralino_vs2.Models;
using System.Threading.Tasks;
using ClientCentralino_vs2.Services;

namespace ClientCentralino_vs2
{
    public partial class IncomingCallNotificationWindow : Window
    {
        private readonly string _numeroChiamante;
        private readonly DateTime _orarioArrivo;
        private readonly ApiService _apiService;

        public IncomingCallNotificationWindow(ApiService apiService, IncomingCall call)
        {
            InitializeComponent();

            _apiService = apiService;

            // Posiziona la finestra in basso a destra ma più in alto (80px dal bordo)
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 80;

            _numeroChiamante = call.NumeroChiamante;
            _orarioArrivo = call.OrarioArrivo;

            TxtCallerNumber.Text = _numeroChiamante;
            TxtArrivalTime.Text = _orarioArrivo.ToString("dd/MM/yyyy HH:mm:ss");

            // Mostra ragione sociale se presente, altrimenti cerca in rubrica
            if (!string.IsNullOrWhiteSpace(call.RagioneSociale))
            {
                TxtRagioneSociale.Text = call.RagioneSociale;
            }
            else
            {
                // Async void per semplicità in UI
                _ = LoadRagioneSocialeAsync(_numeroChiamante);
            }
        }

        private async Task LoadRagioneSocialeAsync(string numero)
        {
            var contatto = await _apiService.FindContactAsync(numero);
            if (contatto != null && !string.IsNullOrWhiteSpace(contatto.RagioneSociale))
            {
                TxtRagioneSociale.Text = contatto.RagioneSociale;
            }
            else
            {
                TxtRagioneSociale.Text = "N/D";
            }
        }

        private void BtnOpenApp_Click(object sender, RoutedEventArgs e)
        {
            string numeroPulito = CleanPhoneNumber(_numeroChiamante);
            if (!string.IsNullOrEmpty(numeroPulito))
            {
                MostraFinestraPrincipale(numeroPulito);
            }
            else
            {
                MessageBox.Show("Numero chiamante non valido");
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return string.Empty;
            return new string(phoneNumber.Where(c => char.IsDigit(c)).ToArray());
        }

        private void MostraFinestraPrincipale(string numeroChiamante = null)
        {
            if (Application.Current.MainWindow == null)
            {
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                if (!string.IsNullOrEmpty(numeroChiamante))
                {
                    (mainWindow as MainWindow)?.CaricaContattoDaNumeroAsync(numeroChiamante);
                    mainWindow.Show();
                    Close();
                }
            }
            else
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    if (!string.IsNullOrEmpty(numeroChiamante))
                    {
                        mainWindow.CaricaContattoDaNumeroAsync(numeroChiamante);
                    }
                    if (mainWindow.WindowState == WindowState.Minimized)
                    {
                        mainWindow.WindowState = WindowState.Normal;
                        Close();
                    }
                    mainWindow.Show();
                    mainWindow.Activate();
                    mainWindow.Topmost = true;
                    mainWindow.Topmost = false;
                    mainWindow.Focus();
                }
            }
        }
    }
}
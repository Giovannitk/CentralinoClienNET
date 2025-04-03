using System;
using System.Windows;
using ClientCentralino_vs2.Models;
using ClientCentralino_vs2.Services;

namespace ClientCentralino_vs2
{
    public partial class NotificationWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly Chiamata _call;
        private readonly Action<int> _onOpenInMainApp;

        public NotificationWindow(ApiService apiService, Chiamata call, Action<int> onOpenInMainApp)
        {
            InitializeComponent();

            _apiService = apiService;
            _call = call;
            _onOpenInMainApp = onOpenInMainApp;

            // Popola i dati della chiamata
            TxtCallId.Text = _call.Id.ToString();
            TxtCallerNumber.Text = _call.NumeroChiamante;
            TxtCallDate.Text = _call.DataArrivoChiamata.ToString("dd/MM/yyyy HH:mm:ss");
            TxtLocationUpdate.Text = _call.Locazione;
        }

        private async void BtnUpdateLocation_Click(object sender, RoutedEventArgs e)
        {
            string location = TxtLocationUpdate.Text.Trim();

            try
            {
                bool success = await _apiService.UpdateCallLocationAsync(_call.Id, location);

                if (success)
                {
                    MessageBox.Show("Località aggiornata con successo.", "Informazione",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("Impossibile aggiornare la località.", "Errore",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'aggiornamento: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenApp_Click(object sender, RoutedEventArgs e)
        {
            _onOpenInMainApp?.Invoke(_call.Id);
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
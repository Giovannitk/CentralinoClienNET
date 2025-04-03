//using System;
//using System.Collections.Generic;
//using System.Windows;
//using System.Windows.Controls;
//using ClientCentralino_vs2.Models;
//using ClientCentralino_vs2.Services;

//namespace ClientCentralino_vs2
//{
//    public partial class MainWindow : Window
//    {
//        private readonly ApiService _apiService;
//        private Chiamata _selectedCall;

//        public MainWindow()
//        {
//            InitializeComponent();
//            _apiService = new ApiService();

//            // Carica i dati iniziali
//            LoadAllCalls();
//        }

//        private async void LoadAllCalls()
//        {
//            try
//            {
//                var calls = await _apiService.GetAllCallsAsync();
//                DgCalls.ItemsSource = calls;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Errore nel caricamento delle chiamate: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private async void BtnFilterCalls_Click(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                string phoneNumber = TxtFilterNumber.Text.Trim();

//                if (string.IsNullOrEmpty(phoneNumber))
//                {
//                    LoadAllCalls();
//                    return;
//                }

//                var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
//                DgCalls.ItemsSource = calls;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Errore nel filtrare le chiamate: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private void BtnRefreshCalls_Click(object sender, RoutedEventArgs e)
//        {
//            LoadAllCalls();
//            TxtFilterNumber.Clear();
//        }

//        private void DgCalls_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            _selectedCall = DgCalls.SelectedItem as Chiamata;

//            if (_selectedCall != null)
//            {
//                TxtLocation.Text = _selectedCall.Extra;
//            }
//        }

//        private async void BtnUpdateLocation_Click(object sender, RoutedEventArgs e)
//        {
//            if (_selectedCall == null)
//            {
//                MessageBox.Show("Seleziona prima una chiamata.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
//                return;
//            }

//            try
//            {
//                string location = TxtLocation.Text.Trim();
//                bool success = await _apiService.UpdateCallLocationAsync(_selectedCall.Id, location);

//                if (success)
//                {
//                    MessageBox.Show("Località aggiornata con successo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

//                    // Aggiorna la vista
//                    _selectedCall.Extra = location;
//                    DgCalls.Items.Refresh();
//                }
//                else
//                {
//                    MessageBox.Show("Impossibile aggiornare la località.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Errore nell'aggiornamento della località: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private async void BtnSearchContact_Click(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                string phoneNumber = TxtSearchContact.Text.Trim();

//                if (string.IsNullOrEmpty(phoneNumber))
//                {
//                    MessageBox.Show("Inserisci un numero di telefono.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
//                    return;
//                }

//                var contact = await _apiService.FindContactAsync(phoneNumber);

//                if (contact != null)
//                {
//                    // Riempie i campi del contatto
//                    TxtContactNumber.Text = contact.NumeroContatto;
//                    TxtContactCompany.Text = contact.RagioneSociale;
//                    TxtContactCity.Text = contact.Citta;
//                    TxtContactInternal.Text = contact.Interno?.ToString();

//                    // Carica le chiamate del contatto
//                    var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
//                    DgContactCalls.ItemsSource = calls;
//                }
//                else
//                {
//                    MessageBox.Show("Contatto non trovato. Puoi inserire i dati e salvarlo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

//                    // Prepara i campi per un nuovo contatto
//                    TxtContactNumber.Text = phoneNumber;
//                    TxtContactCompany.Clear();
//                    TxtContactCity.Clear();
//                    TxtContactInternal.Clear();

//                    // Pulisce la griglia delle chiamate
//                    DgContactCalls.ItemsSource = null;
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Errore nella ricerca del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private async void BtnSaveContact_Click(object sender, RoutedEventArgs e)
//        {
//            try
//            {
//                string phoneNumber = TxtContactNumber.Text.Trim();

//                if (string.IsNullOrEmpty(phoneNumber))
//                {
//                    MessageBox.Show("Il numero di telefono è obbligatorio.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//                    return;
//                }

//                var contact = new Contatto
//                {
//                    NumeroContatto = phoneNumber,
//                    RagioneSociale = TxtContactCompany.Text.Trim(),
//                    Citta = TxtContactCity.Text.Trim()
//                };

//                if (!string.IsNullOrEmpty(TxtContactInternal.Text.Trim()) && int.TryParse(TxtContactInternal.Text.Trim(), out int interno))
//                {
//                    contact.Interno = interno;
//                }

//                bool success = await _apiService.AddContactAsync(contact);

//                if (success)
//                {
//                    MessageBox.Show("Contatto salvato con successo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

//                    // Ricarica le chiamate del contatto
//                    var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
//                    DgContactCalls.ItemsSource = calls;
//                }
//                else
//                {
//                    MessageBox.Show("Impossibile salvare il contatto.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Errore nel salvataggio del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ClientCentralino_vs2.Models;
using ClientCentralino_vs2.Services;
using System.Media;
using System.Windows.Threading;

namespace ClientCentralino_vs2
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly CallNotificationService _notificationService;
        private Chiamata _selectedCall;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _notificationService = new CallNotificationService(_apiService);

            // Carica i dati iniziali
            LoadAllCalls();

            // Avvia il servizio di notifica
            _notificationService.Start(OnNewCallReceived);

            // Assicurati che il servizio di notifica si fermi quando l'applicazione si chiude
            Closing += (s, e) => _notificationService.Dispose();
        }

        private void OnNewCallReceived(Chiamata call)
        {
            try
            {
                // Riproduci un suono di notifica
                SystemSounds.Exclamation.Play();

                // Mostra la finestra di notifica
                var notificationWindow = new NotificationWindow(_apiService, call, OpenAndSelectCall);
                notificationWindow.Show();

                // Aggiorna la griglia chiamate se è visibile
                if (TabControl.SelectedIndex == 0)
                {
                    RefreshCalls();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella gestione della notifica: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OpenAndSelectCall(int callId)
        {
            try
            {
                // Attiva questa finestra e seleziona la tab delle chiamate
                Activate();
                TabControl.SelectedIndex = 0;

                // Carica tutte le chiamate per assicurarsi che quella nuova sia presente
                await RefreshCalls();

                // Trova e seleziona la chiamata specifica
                foreach (Chiamata call in DgCalls.Items)
                {
                    if (call.Id == callId)
                    {
                        DgCalls.SelectedItem = call;
                        DgCalls.ScrollIntoView(call);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'apertura della chiamata: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadAllCalls()
        {
            try
            {
                var calls = await _apiService.GetAllCallsAsync();
                // Ordina le chiamate per data in ordine decrescente (dalla più recente alla più vecchia)
                DgCalls.ItemsSource = calls.OrderByDescending(c => c.DataArrivoChiamata).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel caricamento delle chiamate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Metodo per aggiornare le chiamate e ritornare il risultato
        private async System.Threading.Tasks.Task<List<Chiamata>> RefreshCalls()
        {
            try
            {
                var calls = await _apiService.GetAllCallsAsync();
                // Ordina le chiamate per data in ordine decrescente (dalla più recente alla più vecchia)
                DgCalls.ItemsSource = calls.OrderByDescending(c => c.DataArrivoChiamata).ToList();
                return calls;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'aggiornamento delle chiamate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async void BtnFilterCalls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtFilterNumber.Text.Trim();

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    LoadAllCalls();
                    return;
                }

                var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
                // Ordina le chiamate per data in ordine decrescente
                DgCalls.ItemsSource = calls.OrderByDescending(c => c.DataArrivoChiamata).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel filtrare le chiamate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRefreshCalls_Click(object sender, RoutedEventArgs e)
        {
            await RefreshCalls();
            TxtFilterNumber.Clear();
        }

        private void DgCalls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCall = DgCalls.SelectedItem as Chiamata;

            if (_selectedCall != null)
            {
                TxtLocation.Text = _selectedCall.Locazione;
            }
        }

        private async void BtnUpdateLocation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCall == null)
            {
                MessageBox.Show("Seleziona prima una chiamata.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string location = TxtLocation.Text.Trim();
                bool success = await _apiService.UpdateCallLocationAsync(_selectedCall.Id, location);

                if (success)
                {
                    MessageBox.Show("Località aggiornata con successo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Aggiorna la vista
                    _selectedCall.Locazione = location;
                    DgCalls.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("Impossibile aggiornare la località.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'aggiornamento della località: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSearchContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtSearchContact.Text.Trim();

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    MessageBox.Show("Inserisci un numero di telefono.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var contact = await _apiService.FindContactAsync(phoneNumber);

                if (contact != null)
                {
                    // Riempie i campi del contatto
                    TxtContactNumber.Text = contact.NumeroContatto;
                    TxtContactCompany.Text = contact.RagioneSociale;
                    TxtContactCity.Text = contact.Citta;
                    TxtContactInternal.Text = contact.Interno?.ToString();

                    // Carica le chiamate del contatto
                    var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
                    DgContactCalls.ItemsSource = calls;
                }
                else
                {
                    MessageBox.Show("Contatto non trovato. Puoi inserire i dati e salvarlo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Prepara i campi per un nuovo contatto
                    TxtContactNumber.Text = phoneNumber;
                    TxtContactCompany.Clear();
                    TxtContactCity.Clear();
                    TxtContactInternal.Clear();

                    // Pulisce la griglia delle chiamate
                    DgContactCalls.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella ricerca del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSaveContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtContactNumber.Text.Trim();

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    MessageBox.Show("Il numero di telefono è obbligatorio.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var contact = new Contatto
                {
                    NumeroContatto = phoneNumber,
                    RagioneSociale = TxtContactCompany.Text.Trim(),
                    Citta = TxtContactCity.Text.Trim()
                };

                if (!string.IsNullOrEmpty(TxtContactInternal.Text.Trim()) && int.TryParse(TxtContactInternal.Text.Trim(), out int interno))
                {
                    contact.Interno = interno;
                }

                bool success = await _apiService.AddContactAsync(contact);

                if (success)
                {
                    MessageBox.Show("Contatto salvato con successo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Ricarica le chiamate del contatto
                    var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
                    DgContactCalls.ItemsSource = calls;
                }
                else
                {
                    MessageBox.Show("Impossibile salvare il contatto.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel salvataggio del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
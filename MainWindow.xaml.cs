using System.Windows;
using System.Windows.Controls;
using ClientCentralino_vs2.Models;
using ClientCentralino_vs2.Services;
using System.Media;
using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Windows.Media;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System.Windows.Controls.Primitives;
using System.Reflection;
using System.Collections.ObjectModel;

namespace ClientCentralino_vs2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly CallNotificationService _notificationService;
        private Chiamata _selectedCall;

        private List<Contatto>? _cachedContacts = null;
        private bool _isShowingContacts = false;

        private List<Chiamata> _cachedCalls;
        private Dictionary<string, Contatto> _contattiCache = new Dictionary<string, Contatto>();

        private bool _enableDetailedNotifications = true;

        // Proprietà per i dati dei grafici --------------------------------------
        private SeriesCollection _seriesCollection;
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set
            {
                _seriesCollection = value;
                OnPropertyChanged(nameof(SeriesCollection));
            }
        }

        private SeriesCollection _seriesCollection2;
        public SeriesCollection SeriesCollection2
        {
            get => _seriesCollection2;
            set
            {
                _seriesCollection2 = value;
                OnPropertyChanged(nameof(SeriesCollection2));
            }
        }

        private SeriesCollection _pieSeries;
        public SeriesCollection PieSeries
        {
            get => _pieSeries;
            set
            {
                _pieSeries = value;
                OnPropertyChanged(nameof(PieSeries));
            }
        }

        private SeriesCollection _pieSeries2;
        public SeriesCollection PieSeries2
        {
            get => _pieSeries2;
            set
            {
                _pieSeries2 = value;
                OnPropertyChanged(nameof(PieSeries2));
            }
        }

        private SeriesCollection _pieSeries3;
        public SeriesCollection PieSeries3
        {
            get => _pieSeries3;
            set
            {
                _pieSeries3 = value;
                OnPropertyChanged(nameof(PieSeries3));
            }
        }

        private SeriesCollection _seriesCollectionCallsPerDay;
        public SeriesCollection SeriesCollectionCallsPerDay
        {
            get => _seriesCollectionCallsPerDay;
            set
            {
                _seriesCollectionCallsPerDay = value;
                OnPropertyChanged(nameof(SeriesCollectionCallsPerDay));
            }
        }

        private string[] _labelsCallsPerDay;
        public string[] LabelsCallsPerDay
        {
            get => _labelsCallsPerDay;
            set
            {
                _labelsCallsPerDay = value;
                OnPropertyChanged(nameof(LabelsCallsPerDay));
            }
        }


        // Label grafico a barre per chiamate in entrata
        private string[] _labels;
        public string[] Labels
        {
            get => _labels;
            set
            {
                _labels = value;
                OnPropertyChanged(nameof(Labels));
            }
        }

        // Label grafico a barre per chiamate in uscita
        private string[] _labels2;
        public string[] Labels2
        {
            get => _labels2;
            set
            {
                _labels2 = value;
                OnPropertyChanged(nameof(Labels2));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private SeriesCollection _seriesCollectionByType;
        public SeriesCollection SeriesCollectionByType
        {
            get => _seriesCollectionByType;
            set
            {
                _seriesCollectionByType = value;
                OnPropertyChanged(nameof(SeriesCollectionByType));
            }
        }

        private SeriesCollection _avgDurationSeries;
        public SeriesCollection AvgDurationSeries
        {
            get => _avgDurationSeries;
            set
            {
                _avgDurationSeries = value;
                OnPropertyChanged(nameof(AvgDurationSeries));
            }
        }

        private string[] _avgDurationLabels;
        public string[] AvgDurationLabels
        {
            get => _avgDurationLabels;
            set
            {
                _avgDurationLabels = value;
                OnPropertyChanged(nameof(AvgDurationLabels));
            }
        }

        private SeriesCollection _outgoingCallsSeries;
        public SeriesCollection OutgoingCallsSeries
        {
            get => _outgoingCallsSeries;
            set
            {
                _outgoingCallsSeries = value;
                OnPropertyChanged(nameof(OutgoingCallsSeries));
            }
        }

        private string[] _outgoingCallsLabels;
        public string[] OutgoingCallsLabels
        {
            get => _outgoingCallsLabels;
            set
            {
                _outgoingCallsLabels = value;
                OnPropertyChanged(nameof(OutgoingCallsLabels));
            }
        }


        private Style _columnHeaderStyle;
        private Style _dataGridCellStyle;


        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.OnSessionExpired += HandleSessionExpired;
            _notificationService = new CallNotificationService(_apiService);

            // Imposta il flag in base allo stato iniziale della checkbox
            _enableDetailedNotifications = CbEnableDesktopNotifications.IsChecked == true;
            CbEnableDesktopNotifications.Checked += (s, e) => _enableDetailedNotifications = true;
            CbEnableDesktopNotifications.Unchecked += (s, e) => _enableDetailedNotifications = false;

            // Avvia tutto subito dopo login, anche se la finestra non è mai mostrata
            _ = StartAppLogicAsync();

            _columnHeaderStyle = new Style(typeof(DataGridColumnHeader))
            {
                Setters = {
            new Setter(System.Windows.Controls.Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left),
            new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(10, 0, 0, 0)),
            new Setter(FontFamilyProperty, DgContactCalls.FontFamily),
            new Setter(FontSizeProperty, DgContactCalls.FontSize)
        }
            };

            _dataGridCellStyle = new Style(typeof(DataGridCell))
            {
                Setters = {
            new Setter(System.Windows.Controls.Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left),
            new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(10, 0, 0, 0))
        }
            };

            DataContext = this;

            // Carica le impostazioni
            LoadSettings();

            this.Hide();
        }

        private async Task StartAppLogicAsync()
        {
            await _apiService.LoginTask;
            _notificationService.Start(OnNewCallReceived);
            _notificationService.StartIncomingCallPolling(OnIncomingCallReceived);
            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            try
            {
                // Carica i contatti
                await InitializeCachedContactsAsync();

                // Carica le chiamate
                await RefreshCalls();

                // Carica le statistiche
                await InitializeChartsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'inizializzazione dei dati: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task InitializeCachedContactsAsync()
        {
            try
            {
                _cachedContacts = await _apiService.GetAllContactsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore nel caricamento dei contatti: " + ex.Message);
            }
        }



        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private async void OnNewCallReceived(Chiamata call)
        {
            try
            {
                if (!_enableDetailedNotifications)
                    return;

                bool interno = await IsInternoAsync(call.NumeroChiamante);

                // Se il checkbox è selezionato e il numero chiamante è interno, esci
                if (CbOnlyExternalCalls.IsChecked == true && interno)
                {
                    return;
                }

                // Riproduci un suono di notifica
                SystemSounds.Exclamation.Play();

                // Mostra la finestra di notifica
                var notificationWindow = new NotificationWindow(_apiService, call, OpenAndSelectCall);
                notificationWindow.Show();

                // Aggiorna la griglia chiamate se è visibile
                if (TabControl.SelectedIndex == 0)
                {
                    await RefreshCalls();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella gestione della notifica: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> IsInternoAsync(string numero)
        {
            Contatto chiamante = null;

            if (!string.IsNullOrWhiteSpace(numero))
            {
                if (!_contattiCache.TryGetValue(numero, out chiamante))
                {
                    chiamante = await _apiService.FindContactAsync(numero);
                    _contattiCache[numero] = chiamante;
                }
            }
            // Considera interni i numeri di 3 o 4 cifre
            return !string.IsNullOrWhiteSpace(numero) && numero.Length <= 4 && numero.All(char.IsDigit);
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

        private async Task<List<Chiamata>> GetFilteredCallsAsync()
        {
            try
            {
                // Se non è già popolato, scarica tutte le chiamate una volta sola
                if (_cachedCalls == null)
                    _cachedCalls = await _apiService.GetAllCallsAsync();

                var filtered = new List<Chiamata>();

                double minDuration = Settings1.Default.MinCallDuration > 0
                                        ? Settings1.Default.MinCallDuration
                                        : 5;

                foreach (var call in _cachedCalls)
                {
                    // Escludi chiamate troppo brevi
                    if ((call.DataFineChiamata - call.DataArrivoChiamata).TotalSeconds < minDuration)
                        continue;

                    if (Settings1.Default.HideInternalCalls)
                    {
                        Contatto chiamante = null;
                        Contatto chiamato = null;

                        // Recupera da cache o da API il chiamante
                        if (!string.IsNullOrWhiteSpace(call.NumeroChiamante))
                        {
                            if (!_contattiCache.TryGetValue(call.NumeroChiamante, out chiamante))
                            {
                                chiamante = await _apiService.FindContactAsync(call.NumeroChiamante);
                                _contattiCache[call.NumeroChiamante] = chiamante;
                            }
                        }

                        // Recupera da cache o da API il chiamato
                        if (!string.IsNullOrWhiteSpace(call.NumeroChiamato))
                        {
                            if (!_contattiCache.TryGetValue(call.NumeroChiamato, out chiamato))
                            {
                                chiamato = await _apiService.FindContactAsync(call.NumeroChiamato);
                                _contattiCache[call.NumeroChiamato] = chiamato;
                            }
                        }

                        // Se entrambi sono interni, escludi la chiamata
                        if (chiamante?.Interno == 1 && chiamato?.Interno == 1)
                            continue;
                    }

                    filtered.Add(call);
                }

                return filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel recupero delle chiamate filtrate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


        // Metodo per aggiornare le chiamate e ritornare il risultato
        //private async System.Threading.Tasks.Task<List<Chiamata>> RefreshCalls()
        //{
        //    try
        //    {
        //        var calls = await _apiService.GetAllCallsAsync();
        //        // Ordina le chiamate per data in ordine decrescente (dalla più recente alla più vecchia)
        //        //DgCalls.ItemsSource = calls.OrderByDescending(c => c.DataArrivoChiamata).ToList();
        //        //return calls;

        //        // Filtra le chiamate con durata > 15 secondi
        //        var filteredCalls = calls
        //            .Where(c => (c.DataFineChiamata - c.DataArrivoChiamata).TotalSeconds > 15)
        //            .OrderByDescending(c => c.DataArrivoChiamata)
        //            .ToList();

        //        DgCalls.ItemsSource = filteredCalls;
        //        return filteredCalls;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Errore nell'aggiornamento delle chiamate: {ex.Message}", "Errore",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //        return null;
        //    }
        //}

        private void LoadSettings()
        {
            // Imposta il valore dello slider con il valore salvato
            SliderMinCallDuration.Value = Settings1.Default.MinCallDuration;

            // Imposta lo stato del checkbox in base alle impostazioni salvate
            CbHideInternalCalls.IsChecked = Settings1.Default.HideInternalCalls;

            CbOnlyExternalCalls.IsChecked = Settings1.Default.CbOnlyExternalCalls;
        }



        //private async System.Threading.Tasks.Task<List<Chiamata>> RefreshCalls()
        //{
        //    try
        //    {
        //        var calls = await _apiService.GetAllCallsAsync();

        //        // Recupera il valore configurato (default 5 se non impostato)
        //        double minDuration = Settings1.Default.MinCallDuration > 0
        //                            ? Settings1.Default.MinCallDuration
        //                            : 5;

        //        // Filtra le chiamate con durata > del valore configurato
        //        var filteredCalls = calls
        //            .Where(c => (c.DataFineChiamata - c.DataArrivoChiamata).TotalSeconds >= minDuration)
        //            .OrderByDescending(c => c.DataArrivoChiamata)
        //            .ToList();

        //        DgCalls.ItemsSource = filteredCalls;
        //        return filteredCalls;
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Errore nell'aggiornamento delle chiamate: {ex.Message}", "Errore",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //        return null;
        //    }
        //}
        private async Task<List<Chiamata>> RefreshCalls()
        {
            try
            {
                var calls = await _apiService.GetAllCallsAsync();

                double minDuration = Settings1.Default.MinCallDuration > 0
                                    ? Settings1.Default.MinCallDuration
                                    : 5;

                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.Today;
                DateTime toDate = DatePickerTo.SelectedDate?.AddDays(1).AddSeconds(-1)
                                    ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                var filtered = new List<Chiamata>();
                var contattiCache = new Dictionary<string, Contatto>();
                bool nascondiInterni = Settings1.Default.HideInternalCalls == true;
                bool soloEsterni = Settings1.Default.CbOnlyExternalCalls == true;

                foreach (var call in calls)
                {
                    if ((call.DataFineChiamata - call.DataArrivoChiamata).TotalSeconds < minDuration)
                        continue;

                    if (call.DataArrivoChiamata < fromDate || call.DataArrivoChiamata > toDate)
                        continue;

                    // Recupera le informazioni sui contatti
                    Contatto chiamante = null, chiamato = null;

                    if (!string.IsNullOrWhiteSpace(call.NumeroChiamante))
                    {
                        if (!contattiCache.TryGetValue(call.NumeroChiamante, out chiamante))
                        {
                            chiamante = await _apiService.FindContactAsync(call.NumeroChiamante);
                            contattiCache[call.NumeroChiamante] = chiamante;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(call.NumeroChiamato))
                    {
                        if (!contattiCache.TryGetValue(call.NumeroChiamato, out chiamato))
                        {
                            chiamato = await _apiService.FindContactAsync(call.NumeroChiamato);
                            contattiCache[call.NumeroChiamato] = chiamato;
                        }
                    }

                    bool isChiamanteInterno = chiamante?.Interno == 1;
                    bool isChiamatoInterno = chiamato?.Interno == 1;

                    // Gestione delle chiamate in base ai filtri
                    if (nascondiInterni && isChiamanteInterno && isChiamatoInterno)
                    {
                        // Nascondi solo le chiamate tra interni
                        continue;
                    }

                    if (soloEsterni && isChiamanteInterno)
                    {
                        // Nascondi le chiamate degli interni verso l'esterno
                        continue;
                    }

                    filtered.Add(call);
                }

                DgCalls.ItemsSource = filtered.OrderByDescending(c => c.DataArrivoChiamata).ToList();
                return filtered;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'aggiornamento delle chiamate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }




        //private async void BtnFilterCalls_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        string phoneNumber = TxtFilterNumber.Text.Trim();
        //        MessageBox.Show($"Numero inserito: {phoneNumber}");

        //        if (string.IsNullOrEmpty(phoneNumber))
        //        {
        //            LoadAllCalls();
        //            return;
        //        }

        //        var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
        //        MessageBox.Show($"Numero chiamate trovate: {calls.Count}");

        //        DgCalls.ItemsSource = calls.OrderByDescending(c => c.DataArrivoChiamata).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Errore nel filtrare le chiamate: {ex.Message}", "Errore",
        //            MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}


        private async void BtnFilterCalls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtFilterNumber.Text.Trim();

                // Verifica se contiene solo cifre
                if (!string.IsNullOrEmpty(phoneNumber) && !phoneNumber.All(char.IsDigit))
                {
                    MessageBox.Show("Inserire solo numeri nel campo 'Filtra per numero'.", "Formato non valido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.Today;
                DateTime toDate = DatePickerTo.SelectedDate?.AddDays(1).AddSeconds(-1)
                                    ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    await RefreshCalls();
                    return;
                }

                var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);

                var filteredCalls = calls
                    .Where(c => c.DataArrivoChiamata >= fromDate && c.DataArrivoChiamata <= toDate)
                    .OrderByDescending(c => c.DataArrivoChiamata)
                    .ToList();

                if (filteredCalls.Count == 0)
                {
                    MessageBox.Show("Nessuna chiamata trovata per il numero inserito nel range di date selezionato.", "Nessun risultato", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DgCalls.ItemsSource = filteredCalls;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel filtrare le chiamate: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void BtnFilterByRagioneSociale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ragioneSociale = TxtFilterRagioneSociale.Text.Trim().ToLower();
                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.Today;
                DateTime toDate = DatePickerTo.SelectedDate?.AddDays(1).AddSeconds(-1) ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                var calls = await GetFilteredCallsAsync();

                var filteredCalls = calls
                    .Where(c => c.DataArrivoChiamata >= fromDate && c.DataArrivoChiamata <= toDate)
                    .Where(c => !string.IsNullOrEmpty(ragioneSociale) &&
                                ((c.RagioneSocialeChiamante != null && c.RagioneSocialeChiamante.ToLower().Contains(ragioneSociale)) ||
                                 (c.RagioneSocialeChiamato != null && c.RagioneSocialeChiamato.ToLower().Contains(ragioneSociale))))
                    .OrderByDescending(c => c.DataArrivoChiamata)
                    .ToList();

                if (filteredCalls.Count == 0)
                {
                    MessageBox.Show("Nessuna chiamata trovata per la ragione sociale inserita nel range di date selezionato.", "Nessun risultato", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DgCalls.ItemsSource = filteredCalls;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel filtro per ragione sociale: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnFilterByLocazione_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string locazione = TxtFilterLocazione.Text.Trim().ToLower();
                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.Today;
                DateTime toDate = DatePickerTo.SelectedDate?.AddDays(1).AddSeconds(-1)
                                    ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                var calls = await GetFilteredCallsAsync();

                var filteredCalls = calls
                    .Where(c => c.DataArrivoChiamata >= fromDate && c.DataArrivoChiamata <= toDate)
                    .Where(c => !string.IsNullOrEmpty(locazione) &&
                                (c.Locazione != null && c.Locazione.ToLower().Contains(locazione)))
                    .OrderByDescending(c => c.DataArrivoChiamata)
                    .ToList();

                if (filteredCalls.Count == 0)
                {
                    MessageBox.Show("Nessuna chiamata trovata per la locazione inserita nel range di date selezionato.", "Nessun risultato", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DgCalls.ItemsSource = filteredCalls;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel filtro per locazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // funzione per il bottone per filtrare per tutti i 3 campi
        private async void BtnFilterAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string numero = TxtFilterNumber.Text.Trim().ToLower();
                string ragioneSociale = TxtFilterRagioneSociale.Text.Trim().ToLower();
                string locazione = TxtFilterLocazione.Text.Trim().ToLower();

                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.Today;
                DateTime toDate = DatePickerTo.SelectedDate?.AddDays(1).AddSeconds(-1)
                                    ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                var calls = await GetFilteredCallsAsync();

                var filteredCalls = calls
                    .Where(c => c.DataArrivoChiamata >= fromDate && c.DataArrivoChiamata <= toDate);

                if (!string.IsNullOrEmpty(numero))
                {
                    filteredCalls = filteredCalls
                        .Where(c => (c.NumeroChiamante != null && c.NumeroChiamante.ToLower().Contains(numero)) ||
                                    (c.NumeroChiamato != null && c.NumeroChiamato.ToLower().Contains(numero)));
                }

                if (!string.IsNullOrEmpty(ragioneSociale))
                {
                    filteredCalls = filteredCalls
                        .Where(c => (c.RagioneSocialeChiamante != null && c.RagioneSocialeChiamante.ToLower().Contains(ragioneSociale)) ||
                                    (c.RagioneSocialeChiamato != null && c.RagioneSocialeChiamato.ToLower().Contains(ragioneSociale)));
                }

                if (!string.IsNullOrEmpty(locazione))
                {
                    filteredCalls = filteredCalls
                        .Where(c => c.Locazione != null && c.Locazione.ToLower().Contains(locazione));
                }

                var resultList = filteredCalls.OrderByDescending(c => c.DataArrivoChiamata).ToList();

                if (resultList.Count == 0)
                {
                    MessageBox.Show("Nessuna chiamata trovata con i filtri selezionati.", "Filtra Tutti",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DgCalls.ItemsSource = resultList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel filtro combinato: {ex.Message}", "Errore",
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


        private void DgCalls_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgCalls.SelectedItem == null) return;

            var selectedCall = (Chiamata)DgCalls.SelectedItem;

            // Funzione locale per controllare se un campo è vuoto, nullo o "Non Registrato"
            bool IsFieldEmptyOrUnregistered(string value) =>
                string.IsNullOrWhiteSpace(value) || value.Trim().Equals("Non registrato", StringComparison.OrdinalIgnoreCase);

            // Controlla se il chiamante è incompleto
            bool chiamanteIncompleto = IsFieldEmptyOrUnregistered(selectedCall.RagioneSocialeChiamante) ||
                                       IsFieldEmptyOrUnregistered(selectedCall.Locazione);

            // Controlla se il chiamato è incompleto
            bool chiamatoIncompleto = IsFieldEmptyOrUnregistered(selectedCall.RagioneSocialeChiamato);

            string numeroDaModificare = null;
            string ragioneSociale = null;

            if (chiamanteIncompleto)
            {
                // Modifica il chiamante
                numeroDaModificare = selectedCall.NumeroChiamante;
                ragioneSociale = selectedCall.RagioneSocialeChiamante;
            }
            else if (chiamatoIncompleto)
            {
                // Modifica il chiamato
                numeroDaModificare = selectedCall.NumeroChiamato;
                ragioneSociale = selectedCall.RagioneSocialeChiamato;
            }
            else
            {
                // Tutti i dati sono completi, opzionale: non fare nulla o avvisare
                MessageBox.Show("Entrambi i contatti sono completi.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Passa al tab CONTATTI
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                TabControl.SelectedIndex = 1;

                // Popola i campi nella scheda CONTATTI
                TxtContactNumber.Text = numeroDaModificare;
                TxtContactCompany.Text = ragioneSociale;

                // Opzionale: campo ricerca
                TxtSearchContact.Text = numeroDaModificare;
                TxtSearchContact.Focus();
            }), DispatcherPriority.Background);
        }

        // PANNELLO CONTATTI -------------------------------------------------------------------------------------

        // Funzione ricerca contatto per numero
        private async void BtnSearchContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtSearchByNumber.Text.Trim();
                List<Contatto> matchedContacts;

                if (_cachedContacts != null && _cachedContacts.Any())
                {
                    matchedContacts = string.IsNullOrEmpty(phoneNumber)
                        ? _cachedContacts
                        : _cachedContacts
                            .Where(c => !string.IsNullOrEmpty(c.NumeroContatto) &&
                                        c.NumeroContatto.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                }
                else
                {
                    _cachedContacts = await _apiService.GetAllContactsAsync();

                    matchedContacts = string.IsNullOrEmpty(phoneNumber)
                        ? _cachedContacts
                        : _cachedContacts
                            .Where(c => !string.IsNullOrEmpty(c.NumeroContatto) &&
                                        c.NumeroContatto.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                }

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    TxtCallHeaderContactNumber.Text = "Tutti i contatti";
                    TxtCallHeaderInfo.Text = $"[{_cachedContacts.Count}]";

                    _isShowingContacts = true;
                    SetDataGridColumns();
                    DgContactCalls.ItemsSource = _cachedContacts;
                    DgContactCalls.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtCallHeaderContactNumber.Text = "";
                    TxtCallHeaderInfo.Text = "";

                    if (matchedContacts.Count == 1)
                    {
                        var contact = matchedContacts.First();

                        TxtContactNumber.Text = contact.NumeroContatto;
                        TxtContactCompany.Text = contact.RagioneSociale;
                        TxtContactCity.Text = contact.Citta;

                        //TxtContactInternal.Text = contact.Interno?.ToString();
                        if (contact.Interno.HasValue)
                        {
                            string internoValue = contact.Interno.Value.ToString();
                            var matchingItem = CmbContactInternal.Items
                                .OfType<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag?.ToString() == internoValue);

                            if (matchingItem != null)
                            {
                                CmbContactInternal.SelectedItem = matchingItem;
                            }
                            else
                            {
                                CmbContactInternal.SelectedIndex = -1;
                            }
                        }
                        else
                        {
                            CmbContactInternal.SelectedIndex = -1;
                        }


                        TxtCallHeaderContactNumber.Text = $"[{contact.NumeroContatto}] ({contact.RagioneSociale})";

                        if (_isShowingContacts)
                        {
                            DgContactCalls.Columns.Clear();
                            _isShowingContacts = false;
                        }

                        SetDataGridColumnsCall();

                        var calls = await _apiService.GetCallsByNumberAsync(contact.NumeroContatto);
                        DgContactCalls.ItemsSource = calls;
                        DgContactCalls.Visibility = Visibility.Visible;
                    }
                    else if (matchedContacts.Count > 1)
                    {
                        TxtContactNumber.Clear();
                        TxtContactCompany.Clear();
                        TxtContactCity.Clear();

                        //TxtContactInternal.Clear();
                        CmbContactInternal.SelectedIndex = -1;

                        TxtCallHeaderContactNumber.Text = $"[{phoneNumber}] - {matchedContacts.Count} contatti trovati";

                        _isShowingContacts = true;
                        SetDataGridColumns();
                        DgContactCalls.ItemsSource = matchedContacts;
                        DgContactCalls.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Contatto non trovato. Puoi inserire i dati e salvarlo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                        TxtContactNumber.Text = phoneNumber;
                        TxtContactCompany.Clear();
                        TxtContactCity.Clear();

                        //TxtContactInternal.Clear();
                        CmbContactInternal.SelectedIndex = -1;

                        TxtCallHeaderContactNumber.Text = $"[{phoneNumber}]";

                        if (_isShowingContacts)
                        {
                            DgContactCalls.Columns.Clear();
                            _isShowingContacts = false;
                        }

                        DgContactCalls.ItemsSource = null;
                        DgContactCalls.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella ricerca del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void TxtSearchContact_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"[\d+]"); // accetta solo numeri o il +
        }

        private void TxtSearchByNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            string typedText = TxtSearchByNumber.Text.ToLower();

            if (string.IsNullOrWhiteSpace(typedText) || _cachedContacts == null)
            {
                NumberSuggestionsPopup.IsOpen = false;
                return;
            }

            var matches = _cachedContacts
                .Where(c => !string.IsNullOrWhiteSpace(c.NumeroContatto) &&
                            c.NumeroContatto.ToLower().Contains(typedText))
                .Select(c => c.NumeroContatto)
                .Distinct()
                .Take(10)
                .ToList();

            if (matches.Any())
            {
                NumberSuggestionsListBox.ItemsSource = matches;
                NumberSuggestionsPopup.IsOpen = true;
            }
            else
            {
                NumberSuggestionsPopup.IsOpen = false;
            }
        }

        private void NumberSuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NumberSuggestionsListBox.SelectedItem is string selectedNumber)
            {
                TxtSearchByNumber.Text = selectedNumber;
                NumberSuggestionsPopup.IsOpen = false;

                // Puoi eventualmente lanciare subito la ricerca:
                BtnSearchContact_Click(null, null);
            }
        }


        private void SetDataGridColumns()
        {
            DgContactCalls.Columns.Clear();
            DgContactCalls.ColumnHeaderStyle = _columnHeaderStyle;
            DgContactCalls.CellStyle = _dataGridCellStyle;

            var properties = typeof(Contatto).GetProperties().ToList();

            foreach (var prop in properties)
            {
                DgContactCalls.Columns.Add(new DataGridTextColumn
                {
                    Header = prop.Name,
                    Binding = new Binding(prop.Name),
                    Width = prop.Name.Contains("NumeroContatto") || prop.Name.Contains("Interno") ? 150 : 320
                });
            }
        }

        // Metodo helper per determinare la larghezza
        private double GetColumnWidth(PropertyInfo prop)
        {
            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                return 150;
            if (prop.Name.Contains("RagioneSociale"))
                return 280;
            if (prop.Name.Contains("Locazione"))
                return 250;
            return 140;
        }

        private void SetDataGridColumnsCall()
        {
            DgContactCalls.Columns.Clear();
            DgContactCalls.ColumnHeaderStyle = _columnHeaderStyle;
            DgContactCalls.CellStyle = _dataGridCellStyle;

            var properties = typeof(Chiamata).GetProperties()
                .Where(p => p.Name != "Id")
                .ToList(); // Materializza la query

            foreach (var prop in properties)
            {
                var column = new DataGridTextColumn
                {
                    Header = prop.Name,
                    Binding = new Binding(prop.Name),
                    Width = GetColumnWidth(prop)
                };

                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                {
                    ((Binding)column.Binding).StringFormat = "dd/MM/yyyy HH:mm";
                }

                DgContactCalls.Columns.Add(column);
            }
        }




        private async void BtnSearchByCompany_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ragioneSociale = TxtSearchByCompany.Text.Trim();
                List<Contatto> matchedContacts;

                // Se abbiamo già caricato i contatti, usiamo la cache
                if (_cachedContacts != null && _cachedContacts.Any())
                {
                    matchedContacts = string.IsNullOrEmpty(ragioneSociale)
                        ? _cachedContacts
                        : _cachedContacts
                            .Where(c => !string.IsNullOrEmpty(c.RagioneSociale) &&
                                      c.RagioneSociale.Equals(ragioneSociale, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                }
                else
                {
                    // Altrimenti chiama l'API e memorizza il risultato
                    _cachedContacts = await _apiService.GetAllContactsAsync();

                    matchedContacts = string.IsNullOrEmpty(ragioneSociale)
                        ? _cachedContacts
                        : _cachedContacts
                            .Where(c => !string.IsNullOrEmpty(c.RagioneSociale) &&
                                      c.RagioneSociale.Equals(ragioneSociale, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                }

                // Se non è stata inserita nessuna ragione sociale, mostra tutti i contatti
                if (string.IsNullOrEmpty(ragioneSociale))
                {
                    TxtContactNumber.Clear();
                    TxtContactCompany.Clear();
                    TxtContactCity.Clear();

                    //TxtContactInternal.Clear();
                    CmbContactInternal.SelectedIndex = -1;

                    TxtCallHeaderContactNumber.Text = "Tutti i contatti";
                    TxtCallHeaderInfo.Text = $"[{_cachedContacts.Count}]";

                    _isShowingContacts = true;
                    SetDataGridColumns();
                    DgContactCalls.ItemsSource = _cachedContacts;
                    DgContactCalls.Visibility = Visibility.Visible;
                }
                else // Altrimenti
                {
                    TxtCallHeaderContactNumber.Text = "";
                    TxtCallHeaderInfo.Text = "";

                    if (matchedContacts.Count == 1)
                    {
                        var contact = matchedContacts.First();

                        TxtContactNumber.Text = contact.NumeroContatto;
                        TxtContactCompany.Text = contact.RagioneSociale;
                        TxtContactCity.Text = contact.Citta;


                        //TxtContactInternal.Text = contact.Interno?.ToString();
                        if (contact.Interno.HasValue)
                        {
                            string internoValue = contact.Interno.Value.ToString();
                            var matchingItem = CmbContactInternal.Items
                                .OfType<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag?.ToString() == internoValue);

                            if (matchingItem != null)
                            {
                                CmbContactInternal.SelectedItem = matchingItem;
                            }
                            else
                            {
                                CmbContactInternal.SelectedIndex = -1;
                            }
                        }
                        else
                        {
                            CmbContactInternal.SelectedIndex = -1;
                        }

                        TxtCallHeaderContactNumber.Text = $"[{contact.NumeroContatto}] ({contact.RagioneSociale})";

                        // Ripristina colonne statiche per le chiamate se necessario
                        if (_isShowingContacts)
                        {
                            DgContactCalls.Columns.Clear();
                            _isShowingContacts = false;
                        }

                        SetDataGridColumnsCall();

                        var calls = await _apiService.GetCallsByNumberAsync(contact.NumeroContatto);
                        DgContactCalls.ItemsSource = calls;
                        DgContactCalls.Visibility = Visibility.Visible;
                    }
                    else if (matchedContacts.Count > 1)
                    {
                        TxtContactNumber.Clear();
                        TxtContactCompany.Text = ragioneSociale;
                        TxtContactCity.Clear();

                        //TxtContactInternal.Clear();
                        CmbContactInternal.SelectedIndex = -1;

                        TxtCallHeaderContactNumber.Text = $"[{ragioneSociale}] - {matchedContacts.Count} contatti trovati";

                        _isShowingContacts = true;
                        SetDataGridColumns();
                        DgContactCalls.ItemsSource = matchedContacts;
                        DgContactCalls.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Nessun contatto trovato. Puoi inserire i dati e salvarlo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                        TxtContactNumber.Clear();
                        TxtContactCompany.Text = ragioneSociale;
                        TxtContactCity.Clear();

                        //TxtContactInternal.Clear();
                        CmbContactInternal.SelectedIndex = -1;

                        DgContactCalls.Visibility = Visibility.Collapsed;
                        DgContactCalls.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella ricerca per ragione sociale: {ex.Message} - {ex.StackTrace}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearchByCompany_TextChanged(object sender, TextChangedEventArgs e)
        {
            string typedText = TxtSearchByCompany.Text.ToLower();

            if (string.IsNullOrWhiteSpace(typedText) || _cachedContacts == null)
            {
                CompanySuggestionsPopup.IsOpen = false;
                return;
            }

            var matches = _cachedContacts
                .Where(c => !string.IsNullOrWhiteSpace(c.RagioneSociale) &&
                            c.RagioneSociale.ToLower().Contains(typedText))
                .Select(c => c.RagioneSociale)
                .Distinct()
                .Take(10)
                .ToList();

            if (matches.Any())
            {
                CompanySuggestionsListBox.ItemsSource = matches;
                CompanySuggestionsPopup.IsOpen = true;
            }
            else
            {
                CompanySuggestionsPopup.IsOpen = false;
            }
        }

        private void CompanySuggestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CompanySuggestionsListBox.SelectedItem is string selectedCompany)
            {
                TxtSearchByCompany.Text = selectedCompany;
                CompanySuggestionsPopup.IsOpen = false;

                // Puoi eventualmente lanciare subito la ricerca:
                BtnSearchByCompany_Click(null, null);
            }
        }



        private void TxtContactInternal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Consente solo 0 o 1
            e.Handled = !(e.Text == "0" || e.Text == "1");
        }


        private async void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.MinCallDuration = (int)SliderMinCallDuration.Value;
            Settings1.Default.HideInternalCalls = CbHideInternalCalls.IsChecked == true;
            Settings1.Default.CbOnlyExternalCalls = CbOnlyExternalCalls.IsChecked == true;

            Settings1.Default.Save();
            await RefreshCalls(); // Ricarica i dati dopo il salvataggio
            MessageBox.Show("Impostazioni salvate e chiamate aggiornate!", "Salvataggio", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        // Funzione salvataggio di un contatto
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

                string ragioneSociale = TxtContactCompany.Text;

                if (string.IsNullOrEmpty(ragioneSociale))
                {
                    MessageBox.Show("La ragione sociale è obbligatoria.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var contact = new Contatto
                {
                    NumeroContatto = phoneNumber,
                    RagioneSociale = TxtContactCompany.Text.Trim(),
                    Citta = TxtContactCity.Text.Trim()
                };

                if (CmbContactInternal.SelectedItem is ComboBoxItem selectedItem && int.TryParse(selectedItem.Tag?.ToString(), out int interno))
                {
                    contact.Interno = interno;
                }

                bool success = await _apiService.AddContactAsync(contact);

                if (success)
                {
                    MessageBox.Show("Contatto salvato con successo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Aggiorna la cache
                    if (_cachedContacts != null)
                    {
                        var existing = _cachedContacts.FirstOrDefault(c => c.NumeroContatto == phoneNumber);
                        if (existing != null)
                        {
                            // Aggiorna i dati del contatto esistente
                            existing.RagioneSociale = contact.RagioneSociale;
                            existing.Citta = contact.Citta;
                            existing.Interno = contact.Interno;
                        }
                        else
                        {
                            // Aggiungi nuovo contatto
                            _cachedContacts.Add(contact);
                        }
                    }

                    // Ricarica le chiamate del contatto
                    var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
                    DgContactCalls.ItemsSource = calls;
                }
                else
                {
                    MessageBox.Show("Impossibile salvare il contatto. Campi mancanti (probabile il campo interno).", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel salvataggio del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Funzione per eliminare un contatto
        private async void BtnDeleteContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string phoneNumber = TxtContactNumber.Text.Trim();

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    MessageBox.Show("Nessun numero da eliminare.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Sei sicuro di voler eliminare il contatto {phoneNumber}?", "Conferma eliminazione", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    bool deleted = await _apiService.DeleteContactAsync(phoneNumber);

                    if (deleted)
                    {
                        // Rimuove il contatto dalla cache
                        if (_cachedContacts != null)
                        {
                            var contactToRemove = _cachedContacts.FirstOrDefault(c => c.NumeroContatto == phoneNumber);
                            if (contactToRemove != null)
                                _cachedContacts.Remove(contactToRemove);
                        }

                        MessageBox.Show("Contatto eliminato con successo.", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Pulisci i campi
                        TxtContactNumber.Clear();
                        TxtContactCompany.Clear();
                        TxtContactCity.Clear();

                        //TxtContactInternal.Clear();ù
                        CmbContactInternal.SelectedIndex = -1;

                        DgContactCalls.ItemsSource = null;
                        TxtCallHeaderContactNumber.Text = "";
                        TxtCallHeaderInfo.Text = "";
                    }
                    else
                    {
                        MessageBox.Show("Impossibile eliminare il contatto.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'eliminazione del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Funzione per visualizzare la lista dei contatti incompleti
        private async void BtnShowIncompleteContacts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var contattiIncompleti = await _apiService.GetIncompleteContactsAsync();

                if (contattiIncompleti == null || contattiIncompleti.Count == 0)
                {
                    MessageBox.Show("Nessun contatto incompleto trovato.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var finestra = new IncompleteContactsWindow(contattiIncompleti);
                finestra.Owner = this;
                var risultato = finestra.ShowDialog();

                if (risultato == true && finestra.ContattoSelezionato != null)
                {
                    // Precompila i campi del pannello
                    TxtContactNumber.Text = finestra.ContattoSelezionato.NumeroContatto;
                    TxtContactCompany.Text = finestra.ContattoSelezionato.RagioneSociale;
                    TxtContactCity.Text = finestra.ContattoSelezionato.Citta;

                    //TxtContactInternal.Text = finestra.ContattoSelezionato.Interno?.ToString();
                    if (finestra.ContattoSelezionato.Interno.HasValue)
                    {
                        var valoreInterno = finestra.ContattoSelezionato.Interno.Value;
                        foreach (ComboBoxItem item in CmbContactInternal.Items)
                        {
                            if (item.Tag?.ToString() == valoreInterno.ToString())
                            {
                                CmbContactInternal.SelectedItem = item;
                                break;
                            }
                        }
                    }
                    else
                    {
                        CmbContactInternal.SelectedIndex = -1; // Nessuna selezione
                    }

                    // Imposta il titolo del GroupBox con numero e (eventualmente) ragione sociale
                    var numero = finestra.ContattoSelezionato.NumeroContatto;
                    var ragione = finestra.ContattoSelezionato.RagioneSociale;
                    if (!string.IsNullOrWhiteSpace(ragione))
                    {
                        TxtCallHeaderContactNumber.Text = $"[{numero}] ({ragione})";
                    }
                    else
                    {
                        TxtCallHeaderContactNumber.Text = $"[{numero}]";
                    }

                    // Carica anche le chiamate se vuoi
                    var calls = await _apiService.GetCallsByNumberAsync(numero);
                    DgContactCalls.ItemsSource = calls;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il caricamento dei contatti incompleti: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        // ---------------------------------------------------------------------------------------------


        // PANNELLO STATISTICHE ----------------------------------------------------------------------------------
        // Funzione principale per visualizzare i grafici delle statistiche.
        private async Task InitializeChartsAsync(int days = 1, TimeSpan? timeSpan = null)
        {
            var calls = await _apiService.GetAllCallsAsync();

            // Filtro per giorni recenti
            //var dateThreshold = DateTime.Now.AddDays(-days);
            //var recentCalls = calls.Where(c => c.DataArrivoChiamata >= dateThreshold).ToList();

            // Filtro per periodo selezionato
            IEnumerable<Chiamata> recentCalls;
            if (timeSpan.HasValue)
            {
                var timeThreshold = DateTime.Now.Subtract(timeSpan.Value);
                recentCalls = calls.Where(c => c.DataArrivoChiamata >= timeThreshold).ToList();
            }
            else
            {
                var dateThreshold = DateTime.Now.AddDays(-days);
                recentCalls = calls.Where(c => c.DataArrivoChiamata >= dateThreshold).ToList();
            }

            // Calcolo statistiche
            var topCalled = recentCalls
                .Where(c => c.TipoChiamata?.ToLower() == "entrata")
                .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato })
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>($"{g.Key.RagioneSocialeChiamato} ({g.Key.NumeroChiamato})", g.Count()))
                .FirstOrDefault();

            var topCaller = recentCalls
                .Where(c => c.TipoChiamata?.ToLower() == "uscita")
                .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante })
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>($"{g.Key.RagioneSocialeChiamante} ({g.Key.NumeroChiamante})", g.Count()))
                .FirstOrDefault();

            //var topComune = recentCalls
            //    .Select(c => EstraiComune(c.Locazione))
            //    .Where(comune => !string.IsNullOrWhiteSpace(comune))
            //    .GroupBy(comune => comune)
            //    .OrderByDescending(g => g.Count())
            //    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            //    .FirstOrDefault();
            var allComuni = recentCalls
                .Select(c => EstraiComune(c.Locazione))
                .Where(comune => !string.IsNullOrWhiteSpace(comune))
                .GroupBy(comune => comune)
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToList();


            var topCalledOutbound = recentCalls
                .Where(c => c.TipoChiamata?.ToLower() == "uscita")
                .GroupBy(c => new { c.NumeroChiamato, c.RagioneSocialeChiamato })
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>($"{g.Key.RagioneSocialeChiamato} ({g.Key.NumeroChiamato})", g.Count()))
                .FirstOrDefault();

            var topCallerInbound = recentCalls
                .Where(c => c.TipoChiamata?.ToLower() == "entrata")
                .GroupBy(c => new { c.NumeroChiamante, c.RagioneSocialeChiamante })
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>($"{g.Key.RagioneSocialeChiamante} ({g.Key.NumeroChiamante})", g.Count()))
                .FirstOrDefault();

            // Estrai "Comune di ..." dal campo Locazione
            //var comuniChiamanti = recentCalls
            //    .Where(c => !string.IsNullOrEmpty(c.Locazione))
            //    .Select(c =>
            //    {
            //        // Cerca "Comune di ..." nella locazione
            //        var match = Regex.Match(c.Locazione, @"Comune di ([\w\s]+)", RegexOptions.IgnoreCase);
            //        return match.Success ? match.Groups[1].Value.Trim() : null;
            //    })
            //    .Where(c => !string.IsNullOrEmpty(c))
            //    .GroupBy(c => c)
            //    .OrderByDescending(g => g.Count())
            //    .Select(g => new { Comune = g.Key, Chiamate = g.Count() })
            //    .ToList();


            var stats = new CallStatsDto
            {
                Inbound = recentCalls.Count(c => c.TipoChiamata?.ToLower() == "entrata"),
                Outbound = recentCalls.Count(c => c.TipoChiamata?.ToLower() == "uscita"),
                TopNumCallsInbound = topCalled,
                TopNumCallsOutbound = topCaller,
                //TopComune = topComune,
                TopCallerInInbound = topCallerInbound,
                MostCalledInOutbound = topCalledOutbound,
                //ComuniChiamanti = allComuni.Take(10).ToList()
                ComuniChiamanti = allComuni
            };

            // Salva gli altri comuni (se esistono)
            if (allComuni.Count > 10)
            {
                stats.AltriComuni = allComuni.Skip(10).ToList();
            }


            var call = calls[0].TipoChiamata;

            //MessageBox.Show($"inbound: {stats.Inbound} - outbound: {stats.Outbound} - calls: {calls.Count()} - first call: {call}");

            var dailyStats = recentCalls
                .GroupBy(c => c.DataArrivoChiamata.Date)
                .Select(g => new DailyCallStatsDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();


            // === Nuovi grafici per durata media e chiamate in uscita ===
            // 1. Calcolo DURATA MEDIA delle chiamate IN ENTRATA (esterne --> azienda)
            var entrataConRagioneSociale = recentCalls
                .Where(c => c.TipoChiamata?.Equals("entrata", StringComparison.OrdinalIgnoreCase) == true
                            && !string.IsNullOrEmpty(c.RagioneSocialeChiamante)
                            && c.DataArrivoChiamata != null
                            && c.DataFineChiamata != null
                            && c.DataFineChiamata > c.DataArrivoChiamata) // Esclude chiamate con durata negativa
                .GroupBy(c => c.RagioneSocialeChiamante)
                .Select(g => new
                {
                    RagioneSociale = g.Key,
                    MediaDurataSecondi = Math.Round(g.Average(c => (c.DataFineChiamata - c.DataArrivoChiamata).TotalSeconds)),
                    NumeroChiamate = g.Count()
                })
                .OrderByDescending(x => x.MediaDurataSecondi)
                .ToList();

            // 2. Calcolo DURATA MEDIA delle chiamate IN USCITA (azienda --> esterne)
            var uscitaConRagioneSociale = recentCalls
                .Where(c => c.TipoChiamata?.Equals("uscita", StringComparison.OrdinalIgnoreCase) == true
                            && !string.IsNullOrEmpty(c.RagioneSocialeChiamante)
                            && c.DataArrivoChiamata != null
                            && c.DataFineChiamata != null
                            && c.DataFineChiamata > c.DataArrivoChiamata) // Esclude chiamate con durata negativa
                .GroupBy(c => c.RagioneSocialeChiamante)
                .Select(g => new
                {
                    RagioneSociale = g.Key,
                    MediaDurataSecondi = Math.Round(g.Average(c => (c.DataFineChiamata - c.DataArrivoChiamata).TotalSeconds)),
                    NumeroChiamate = g.Count()

                })
                .OrderByDescending(x => x.MediaDurataSecondi)
                .ToList();



            //foreach (var i in entrataConRagioneSociale.Take(50)) 
            //{
            //    MessageBox.Show($"RS: {i.RagioneSociale} [num. {i.NumeroChiamate}, {i.MediaDurataSecondi}s]");
            //}

            Application.Current.Dispatcher.Invoke(() =>
            {
                // === Grafico Durata Media in entrata ===
                Labels = entrataConRagioneSociale.Select(x => x.RagioneSociale).ToArray();
                SeriesCollection = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Durata media in entrata (sec)",
                        Values = new ChartValues<double>(entrataConRagioneSociale.Select(x => x.MediaDurataSecondi).Take(30)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString("F1")
                    }
                };

                // === Grafico Durata Media in uscita ===
                Labels2 = uscitaConRagioneSociale.Select(x => x.RagioneSociale).ToArray();
                SeriesCollection2 = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Durata media in uscita (sec)",
                        Values = new ChartValues<double>(uscitaConRagioneSociale.Select(x => x.MediaDurataSecondi).Take(25)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString("F1")
                    }
                };

                // === Grafico a torta 1 ===
                PieSeries = new SeriesCollection {
                    new PieSeries
                    {
                        Title = "In entrata",
                        Values = new ChartValues<double> { stats.Inbound },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                    },
                    new PieSeries
                    {
                        Title = "In uscita",
                        Values = new ChartValues<double> { stats.Outbound },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 180, 255))
                    }
                };

                // === Grafico a torta 2 ===
                PieSeries2 = new SeriesCollection {
                    new PieSeries
                    {
                        Title = $"Più chiamato: {stats.TopNumCallsInbound?.Key}",
                        Values = new ChartValues<double> { stats.TopNumCallsInbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 100, 100))
                    },
                    new PieSeries
                    {
                        Title = $"Ha chiamato di più: {stats.TopNumCallsOutbound?.Key}",
                        Values = new ChartValues<double> { stats.TopNumCallsOutbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 200, 100))
                    },
                    new PieSeries
                    {
                        Title = $"Più chiamato in uscita: {stats.MostCalledInOutbound?.Key}",
                        Values = new ChartValues<double> { stats.MostCalledInOutbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 140, 140))
                    },
                    new PieSeries
                    {
                        Title = $"Ha chiamato di più in entrata: {stats.TopCallerInInbound?.Key}",
                        Values = new ChartValues<double> { stats.TopCallerInInbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 255, 140))
                    }
                };

                // === Grafico a torta 2 ===
                //PieSeries3 = new SeriesCollection {
                //    new PieSeries
                //    {
                //        Title = $"Comune più attivo: {stats.TopComune?.Key}",
                //        Values = new ChartValues<double> { stats.TopComune?.Value ?? 0 },
                //        DataLabels = true,
                //        Fill = new SolidColorBrush(Color.FromRgb(255, 180, 0))
                //    }
                //};

                // === Grafico a torta (Tutti i comuni) ===
                //PieSeries3 = new SeriesCollection();

                //// Aggiungi i top 10 comuni
                //foreach (var comune in stats.ComuniChiamanti)
                //{
                //    PieSeries3.Add(new PieSeries
                //    {
                //        Title = comune.Key,
                //        Values = new ChartValues<double> { comune.Value },
                //        DataLabels = true,
                //        LabelPoint = point => $"{point.Y} ({point.Participation:P1})"
                //    });
                //}
                PieSeries3 = new SeriesCollection();
                foreach (var comune in stats.ComuniChiamanti)
                {
                    PieSeries3.Add(new PieSeries
                    {
                        Title = comune.Key,
                        Values = new ChartValues<double> { comune.Value },
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y} ({point.Participation:P1})"
                    });
                }

                // Aggiungi la voce "Altri" se ci sono comuni esclusi
                //if (stats.AltriComuni.Any())
                //{
                //    var altriTotal = stats.AltriComuni.Sum(c => c.Value);

                //    PieSeries3.Add(new PieSeries
                //    {
                //        Title = $"Altri comuni ({stats.AltriComuni.Count})",
                //        Values = new ChartValues<double> { altriTotal },
                //        Tag = stats.AltriComuni, // Memorizza la lista nel Tag
                //        DataLabels = true
                //    });
                //}

                // Assegna colori (con colore grigio per "Altri comuni")
                var colori = new List<System.Windows.Media.Color>
                {
                    System.Windows.Media.Color.FromRgb(255, 180, 0),  // arancione
                    System.Windows.Media.Color.FromRgb(0, 150, 255),   // blu
                    System.Windows.Media.Color.FromRgb(50, 200, 100),  // verde
                    System.Windows.Media.Color.FromRgb(200, 50, 150),  // rosa
                    System.Windows.Media.Color.FromRgb(150, 100, 255), // viola
                    System.Windows.Media.Color.FromRgb(255, 120, 0),   // arancione scuro
                    System.Windows.Media.Color.FromRgb(0, 100, 200),   // blu scuro
                    System.Windows.Media.Color.FromRgb(100, 180, 50),  // verde scuro
                    System.Windows.Media.Color.FromRgb(180, 30, 120),  // magenta
                    System.Windows.Media.Color.FromRgb(120, 80, 220),  // lilla
                    System.Windows.Media.Color.FromRgb(150, 150, 150)  // grigio (per "Altri comuni")
                };

                for (int i = 0; i < PieSeries3.Count; i++)
                {
                    var serie = (PieSeries)PieSeries3[i];
                    serie.Fill = new SolidColorBrush(colori[i % colori.Count]);
                }


            });



            // === Grafico temporale (per giorno) ===
            if (dailyStats.Any())
            {
                LabelsCallsPerDay = dailyStats.Select(d => d.Date.ToString("dd/MM")).ToArray();
                SeriesCollectionCallsPerDay = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Chiamate giornaliere",
                        Values = new ChartValues<int>(dailyStats.Select(d => d.Count)),
                        Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                        Fill = Brushes.Transparent,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        DataLabels = true
                    }
                };
            }

            //MessageBox.Show("Assegnazione dei dati ai grafici terminata.");
        }


        private async void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            int days = 1; // default
            TimeSpan? timeSpan = null;

            var selected = (CbTimeRange.SelectedItem as ComboBoxItem)?.Content?.ToString();

            switch (selected)
            {
                case "Ultima ora":
                    timeSpan = TimeSpan.FromHours(1);
                    break;
                case "Ultime 12 ore":
                    timeSpan = TimeSpan.FromHours(12);
                    break;
                case "Ultime 24 ore":
                    days = 1;
                    break;
                case "Ultimi 7 giorni":
                    days = 7;
                    break;
                case "Ultimo mese":
                    days = 30;
                    break;
                case "Ultimo trimestre":
                    days = 90;
                    break;
                case "Ultimo anno":
                    days = 365;
                    break;
            }

            await InitializeChartsAsync(days, timeSpan);
        }

        private string EstraiComune(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            var match = Regex.Match(input, @"Comune di\s+(.*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }


        // ---------------------------------------------------------------------------------------------

        // PANNELLO CHIAMATE --------------------------------------------------------------------------
        // Esportazione chiamate in un file
        private async void BtnExportCalls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Mostra dialog per scegliere il formato di esportazione
                var exportDialog = new ExportDialog();
                if (exportDialog.ShowDialog() == true)
                {
                    // Ottieni le chiamate filtrate
                    var calls = await GetFilteredCalls();

                    if (calls == null || !calls.Any())
                    {
                        MessageBox.Show("Nessuna chiamata da esportare.", "Info",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Crea il dialog per salvare il file
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = exportDialog.SelectedFormat == ExportFormat.CSV
                            ? "CSV file (*.csv)|*.csv"
                            : "Excel file (*.xlsx)|*.xlsx",
                        DefaultExt = exportDialog.SelectedFormat == ExportFormat.CSV
                            ? ".csv"
                            : ".xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // Esporta nel formato selezionato
                        if (exportDialog.SelectedFormat == ExportFormat.CSV)
                        {
                            ExportToCsv(calls, saveFileDialog.FileName);
                        }
                        else
                        {
                            ExportToExcel(calls, saveFileDialog.FileName);
                        }

                        MessageBox.Show($"Dati esportati con successo in: {saveFileDialog.FileName}",
                            "Esportazione completata", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'esportazione: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<List<Chiamata>> GetFilteredCalls()
        {
            // Ottieni le chiamate filtrate per numero se presente
            List<Chiamata> calls;
            string phoneNumber = TxtFilterNumber.Text.Trim();

            if (!string.IsNullOrEmpty(phoneNumber))
            {
                calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
            }
            else
            {
                calls = await _apiService.GetAllCallsAsync();
            }

            // Filtra per data se selezionata
            if (DatePickerFrom.SelectedDate != null || DatePickerTo.SelectedDate != null)
            {
                DateTime fromDate = DatePickerFrom.SelectedDate ?? DateTime.MinValue;
                DateTime toDate = DatePickerTo.SelectedDate ?? DateTime.MaxValue;

                calls = calls.Where(c => c.DataArrivoChiamata >= fromDate &&
                                       c.DataArrivoChiamata <= toDate).ToList();
            }

            return calls?.OrderByDescending(c => c.DataArrivoChiamata).ToList();
        }

        private void ExportToCsv(List<Chiamata> calls, string filePath)
        {
            var sb = new StringBuilder();

            // Intestazioni
            sb.AppendLine("Tipo;Numero Chiamante;Numero Chiamato;Data Arrivo;Data Fine;Località;Ragione Sociale Chiamante;Ragione Sociale Chiamato");

            // Dati
            foreach (var call in calls)
            {
                sb.AppendLine($"{EscapeCsv(call.TipoChiamata)};" +
                              $"{EscapeCsv(call.NumeroChiamante)};" +
                              $"{EscapeCsv(call.NumeroChiamato)};" +
                              $"{call.DataArrivoChiamata:dd/MM/yyyy HH:mm:ss};" +
                              $"{call.DataFineChiamata:dd/MM/yyyy HH:mm:ss};" +
                              $"{EscapeCsv(call.Locazione)};" +
                              $"{EscapeCsv(call.RagioneSocialeChiamante)};" +
                              $"{EscapeCsv(call.RagioneSocialeChiamato)}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(";") ? $"\"{value}\"" : value;
        }


        // Funzione per gestire il pulsante esporta chiamate tramite file
        private void ExportToExcel(List<Chiamata> calls, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Chiamate");

                // Intestazioni
                worksheet.Cell(1, 1).Value = "Tipo";
                worksheet.Cell(1, 2).Value = "Numero Chiamante";
                worksheet.Cell(1, 3).Value = "Numero Chiamato";
                worksheet.Cell(1, 4).Value = "Data Arrivo";
                worksheet.Cell(1, 5).Value = "Data Fine";
                worksheet.Cell(1, 6).Value = "Località";
                worksheet.Cell(1, 7).Value = "Ragione Sociale Chiamante";
                worksheet.Cell(1, 8).Value = "Ragione Sociale Chiamato";

                // Dati
                for (int i = 0; i < calls.Count; i++)
                {
                    var call = calls[i];
                    worksheet.Cell(i + 2, 1).Value = call.TipoChiamata;
                    worksheet.Cell(i + 2, 2).Value = call.NumeroChiamante;
                    worksheet.Cell(i + 2, 3).Value = call.NumeroChiamato;
                    worksheet.Cell(i + 2, 4).Value = call.DataArrivoChiamata;
                    worksheet.Cell(i + 2, 5).Value = call.DataFineChiamata;
                    worksheet.Cell(i + 2, 6).Value = call.Locazione;
                    worksheet.Cell(i + 2, 7).Value = call.RagioneSocialeChiamante;
                    worksheet.Cell(i + 2, 8).Value = call.RagioneSocialeChiamato;

                    // Formatta le date
                    worksheet.Cell(i + 2, 4).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                    worksheet.Cell(i + 2, 5).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                }

                // Auto-adatta le colonne
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }
        }
        // ---------------------------------------------------------------------------------------------



        // ALTRO ------------------------------------------------------------------------------
        // Funzione per gestire la visualizzazione dei contatti incompleti all'avvio dell'app
        private async Task ShowIncompleteContactsAsync()
        {
            var incompleteContacts = await _apiService.GetIncompleteContactsAsync();

            if (incompleteContacts.Any())
            {
                var window = new Window
                {
                    Title = "Attenzione - Contatti da completare",
                    Width = 800,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // Costruisci il messaggio
                var message = new StringBuilder();
                message.AppendLine("Contatti da completare:");
                message.AppendLine();
                message.AppendLine("Numero".PadRight(15) + "Interno".PadRight(10) + "Rag. Sociale".PadRight(25) + "Città");

                foreach (var contatto in incompleteContacts)
                {
                    message.AppendLine(
                        (contatto.NumeroContatto ?? "NULL").PadRight(15) +
                        contatto.Interno.ToString().PadRight(10) +
                        (contatto.RagioneSociale ?? "NULL").PadRight(25) +
                        (contatto.Citta ?? "NULL")
                    );
                }

                var textBlock = new TextBlock
                {
                    FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                    TextWrapping = TextWrapping.NoWrap,
                    Text = message.ToString()
                };

                var scrollViewer = new ScrollViewer
                {
                    Content = textBlock,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(10)
                };

                // Usa un Grid come contenitore principale
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Aggiungi lo ScrollViewer e il Button al Grid
                Grid.SetRow(scrollViewer, 0);
                grid.Children.Add(scrollViewer);

                var button = new Button
                {
                    Content = "OK",
                    Width = 80,
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                button.Click += (s, e) => window.Close();

                Grid.SetRow(button, 1);
                grid.Children.Add(button);

                window.Content = grid;
                window.ShowDialog();
            }
        }

        // Funzione per caricare il contatto quando si preme sul pulsante Aggiorna Rubrica nella
        // finestra notifica.
        public async Task CaricaContattoDaNumeroAsync(string numero)
        {
            try
            {
                var contatto = await _apiService.FindContactAsync(numero);
                if (contatto != null)
                {
                    //if (!string.IsNullOrEmpty(contatto.RagioneSociale) && !string.IsNullOrEmpty(contatto.Citta) && contatto.Interno != null)
                    //{
                    // Popolo i controlli della finestra con i dati del contatto
                    // Passo al tab CONTATTI
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TabControl.SelectedIndex = 1;

                        // Popolo i campi nella scheda CONTATTI con valori di default se null
                        TxtContactNumber.Text = numero;//contatto.NumeroContatto ?? string.Empty;
                        TxtContactCompany.Text = contatto.RagioneSociale ?? string.Empty;
                        TxtContactCity.Text = contatto.Citta ?? string.Empty;

                        //TxtContactInternal.Text = contatto.Interno?.ToString() ?? string.Empty;
                        if (contatto.Interno.HasValue)
                        {
                            foreach (ComboBoxItem item in CmbContactInternal.Items)
                            {
                                if (item?.Tag != null && contatto != null && contatto.Interno.HasValue && item.Tag.ToString() == contatto.Interno.Value.ToString())
                                {
                                    CmbContactInternal.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            CmbContactInternal.SelectedIndex = -1;
                        }


                        TxtSearchContact.Focus();
                    }), DispatcherPriority.Background);
                    //}
                    //else 
                    //{
                    //    MessageBox.Show("Contatto già presente in rubrica.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);
                    //}

                    // Ripristina colonne statiche per le chiamate se necessario
                    if (_isShowingContacts)
                    {
                        DgContactCalls.Columns.Clear(); // Elimina colonne dinamiche
                        _isShowingContacts = false;
                    }

                    SetDataGridColumnsCall();

                    var calls = await _apiService.GetCallsByNumberAsync(numero);
                    DgContactCalls.ItemsSource = calls;
                    DgContactCalls.Visibility = Visibility.Visible;
                }
                else
                {
                    // Contatto non trovato
                    MessageBox.Show("Contatto non trovato. Puoi inserire i dati e salvarlo.", "Informazione", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Popolo i controlli della finestra con i dati del contatto
                    // Passo al tab CONTATTI
                    await this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TabControl.SelectedIndex = 1;

                        // Popolo i campi nella scheda CONTATTI con valori di default se null
                        TxtContactNumber.Text = numero;//contatto.NumeroContatto ?? string.Empty;
                        //TxtContactCompany.Text = contatto.RagioneSociale ?? string.Empty;
                        //TxtContactCity.Text = contatto.Citta ?? string.Empty;
                        //TxtContactInternal.Text = contatto.Interno?.ToString() ?? string.Empty;

                        TxtSearchContact.Focus();
                    }), DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel caricamento del contatto da notifica.\n {ex.Message}");
            }
        }



        // IMPOSTAZIONI -----------------------------------------------------------------
        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            ProgressBarConnessione.Visibility = Visibility.Visible;
            BtnTestConnection.IsEnabled = false;

            var success = await _apiService.TestCustomConnection(TxtServerIP.Text, TxtServerPort.Text);

            BtnTestConnection.IsEnabled = true;
            ProgressBarConnessione.Visibility = Visibility.Collapsed;

            MessageBox.Show(success ? "Connessione riuscita!" : "Connessione fallita.", "Esito",
                MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Error);
        }



        private async void BtnSaveNetworkSettings_Click(object sender, RoutedEventArgs e)
        {
            string ip = TxtServerIP.Text;
            string port = TxtServerPort.Text;

            var testOk = await _apiService.TestCustomConnection(ip, port);
            if (!testOk)
            {
                MessageBox.Show("Impossibile salvare. Il server non è raggiungibile.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Se il test è andato bene, aggiorno l'endpoint
            _apiService.UpdateEndpoint(ip, port);
            TxtApiEndpoint.Text = $"http://{ip}:{port}/";

            MessageBox.Show("Configurazione salvata con successo!", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnIncomingCallReceived(IncomingCall call)
        {
            try
            {
                // Suono diverso per incoming call
                SystemSounds.Asterisk.Play();
                var incomingWindow = new IncomingCallNotificationWindow(_apiService, call);
                incomingWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nella gestione della notifica incoming: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleSessionExpired()
        {
            _notificationService.Stop();
        }
    }



}
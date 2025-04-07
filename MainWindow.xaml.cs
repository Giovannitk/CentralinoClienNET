using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ClientCentralino_vs2.Models;
using ClientCentralino_vs2.Services;
using System.Media;
using System.Windows.Threading;
using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Windows.Media;

namespace ClientCentralino_vs2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly CallNotificationService _notificationService;
        private Chiamata _selectedCall;

        // Proprietà per i dati dei grafici
        public SeriesCollection SeriesCollectionAvgDuration { get; set; }
        public SeriesCollection SeriesCollectionTopCallers { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

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

        public string[] LabelsByType { get; set; }
        public string[] LabelsAvgDuration { get; set; }
        public string[] LabelsTopCallers { get; set; }



        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _notificationService = new CallNotificationService(_apiService);

            //Test chart
            //TestCharts();

            // Avvia il servizio di notifica
            _notificationService.Start(OnNewCallReceived);


            // Inizializzazione Charts
            _ = InitializeChartsAsync();

            DataContext = this;

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

       private async Task LoadStatistics()
        {
            try
            {
                Console.WriteLine("Inizio LoadStatistics");

                var calls = await RefreshCalls();
                Console.WriteLine($"Chiamate caricate: {calls?.Count ?? 0}");

                if (calls == null || !calls.Any())
                {
                    MessageBox.Show("Nessuna chiamata disponibile per le statistiche",
                                  "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Console.WriteLine("Filtro date...");

                // Filtra per intervallo di date
                //if (DateFrom.SelectedDate != null || DateTo.SelectedDate != null)
                //{
                //    DateTime from = DateFrom.SelectedDate ?? calls.Min(c => c.DataArrivoChiamata);
                //    DateTime to = DateTo.SelectedDate?.AddDays(1) ?? calls.Max(c => c.DataArrivoChiamata).AddDays(1);
                //    calls = calls.Where(c => c.DataArrivoChiamata >= from && c.DataArrivoChiamata < to).ToList();
                //}

                // Carica i grafici
                LoadCallsByType(calls);
                LoadAvgDuration(calls);
                LoadTopCallers(calls);
                LoadCallsPerDay(calls);

                // Forza l'aggiornamento UI
                OnPropertyChanged(nameof(SeriesCollectionByType));
                OnPropertyChanged(nameof(SeriesCollectionAvgDuration));
                //OnPropertyChanged(nameof(SeriesCollectionTopCallers));
                OnPropertyChanged(nameof(SeriesCollectionCallsPerDay));
                OnPropertyChanged(nameof(LabelsByType));
                OnPropertyChanged(nameof(LabelsAvgDuration));
                OnPropertyChanged(nameof(LabelsTopCallers));
                OnPropertyChanged(nameof(LabelsCallsPerDay));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore in LoadStatistics: {ex}");
                MessageBox.Show($"Errore nel caricamento delle statistiche: {ex.Message}",
                              "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadCallsByType(List<Chiamata> calls)
        {
            var callsByType = calls
                .GroupBy(c => c.TipoChiamata ?? "Sconosciuto")
                .Select(g => new { Tipo = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Debug: verifica i dati
            Console.WriteLine("Chiamate per tipo:");
            foreach (var item in callsByType)
            {
                Console.WriteLine($"{item.Tipo}: {item.Count}");
            }

            LabelsByType = callsByType.Select(x => x.Tipo).ToArray();
            SeriesCollectionByType = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Chiamate",
                    Values = new ChartValues<int>(callsByType.Select(x => x.Count))
                }
            };
        }

        private void LoadAvgDuration(List<Chiamata> calls)
        {
            foreach (Chiamata c in calls) 
            {
                MessageBox.Show($"chiamata: {c}");
                break;
            }

            // Calcola la durata di ogni chiamata (in secondi)
            var callsWithDuration = calls
                .Select(c => new {
                    c.TipoChiamata,
                    Duration = (c.DataFineChiamata - c.DataArrivoChiamata).TotalSeconds
                })
                .ToList();

            // Calcola la durata media per tipo
            var avgDurationByType = callsWithDuration
                .GroupBy(c => c.TipoChiamata ?? "Sconosciuto")
                .Select(g => new {
                    Tipo = g.Key,
                    AvgDuration = g.Average(x => x.Duration)
                })
                .OrderByDescending(x => x.AvgDuration)
                .ToList();

            LabelsAvgDuration = avgDurationByType.Select(x => x.Tipo).ToArray();
            SeriesCollectionAvgDuration = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Durata Media (sec)",
                    Values = new ChartValues<double>(avgDurationByType.Select(x => x.AvgDuration))
                }
            };
        }

        private void LoadTopCallers(List<Chiamata> calls, int topCount = 10)
        {
            // Trova i numeri che chiamano più frequentemente
            var topCallers = calls
                .Where(c => !string.IsNullOrEmpty(c.NumeroChiamante))
                .GroupBy(c => c.NumeroChiamante)
                .Select(g => new {
                    Numero = g.Key,
                    Count = g.Count(),
                    RagioneSociale = g.First().RagioneSocialeChiamante
                })
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToList();

            // Crea etichette con numero e ragione sociale (se presente)
            LabelsTopCallers = topCallers
                .Select(x => string.IsNullOrEmpty(x.RagioneSociale)
                    ? x.Numero
                    : $"{x.RagioneSociale} ({x.Numero})")
                .ToArray();

            SeriesCollectionTopCallers = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Chiamate",
                    Values = new ChartValues<int>(topCallers.Select(x => x.Count))
                }
            };
        }

        private void LoadCallsPerDay(List<Chiamata> calls)
        {
            // Raggruppa per giorno
            var callsPerDay = calls
                .GroupBy(c => c.DataArrivoChiamata.Date)
                .Select(g => new {
                    Data = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Data)
                .ToList();

            // Se ci sono meno di 7 giorni, mostra tutti i giorni
            // Altrimenti mostra gli ultimi 7 giorni
            var daysToShow = callsPerDay.Count < 7 ? callsPerDay : callsPerDay.Skip(Math.Max(0, callsPerDay.Count - 7));

            LabelsCallsPerDay = daysToShow.Select(x => x.Data.ToString("dd/MM")).ToArray();
            SeriesCollectionCallsPerDay = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Chiamate",
                    Values = new ChartValues<int>(daysToShow.Select(x => x.Count)),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                }
            };
        }

        private async void BtnUpdateStats_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatistics();
        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TabControl.SelectedItem is TabItem selectedTab &&
                    selectedTab.Header.ToString() == "STATISTICHE")
                {
                    await LoadStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel caricamento delle statistiche: {ex.Message}",
                              "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestCharts()
        {
            // Dati di test
            LabelsByType = new[] { "Test1", "Test2", "Test3" };
            SeriesCollectionByType = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Test",
                    Values = new ChartValues<int> { 10, 20, 30 }
                }
            };

            // Notifica i cambiamenti
            OnPropertyChanged(nameof(LabelsByType));
            OnPropertyChanged(nameof(SeriesCollectionByType));
        }


        private async Task InitializeChartsAsync(int days = 7)
        {
            var calls = await _apiService.GetAllCallsAsync();

            // Filtro per giorni recenti
            var dateThreshold = DateTime.Now.AddDays(-days);
            var recentCalls = calls.Where(c => c.DataArrivoChiamata >= dateThreshold).ToList();

            // Calcolo statistiche
            var stats = new CallStatsDto
            {
                Inbound = recentCalls.Count(c => c.TipoChiamata?.ToLower() == "entrata"),
                Outbound = recentCalls.Count(c => c.TipoChiamata?.ToLower() == "uscita"),
                Missed = 0,
                Internal = 0
            };

            var call = calls[0].TipoChiamata; 

            MessageBox.Show($"inbound: {stats.Inbound} - outbound: {stats.Outbound} - calls: {calls.Count()} - first call: {call}");

            var dailyStats = recentCalls
                .GroupBy(c => c.DataArrivoChiamata.Date)
                .Select(g => new DailyCallStatsDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                // === Grafico a barre (per tipo) ===
                Labels = new[] { "In entrata", "In uscita", "Persa", "Interna" };
                SeriesCollection = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Chiamate",
                        Values = new ChartValues<int> { stats.Inbound, stats.Outbound, stats.Missed, stats.Internal },
                        Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                        DataLabels = true,
                        LabelPoint = point => point.Y.ToString()
                    }
                };

                    // === Grafico a torta ===
                PieSeries = new SeriesCollection
                {
                    new PieSeries { Title = "In entrata", Values = new ChartValues<double> { stats.Inbound }, DataLabels = true, Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204)) },
                    new PieSeries { Title = "In uscita", Values = new ChartValues<double> { stats.Outbound }, DataLabels = true, Fill = new SolidColorBrush(Color.FromRgb(100, 180, 255)) },
                    new PieSeries { Title = "Persa", Values = new ChartValues<double> { stats.Missed }, DataLabels = true, Fill = new SolidColorBrush(Color.FromRgb(200, 100, 100)) },
                    new PieSeries { Title = "Interna", Values = new ChartValues<double> { stats.Internal }, DataLabels = true, Fill = new SolidColorBrush(Color.FromRgb(100, 200, 100)) }
                };
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
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                        Fill = Brushes.Transparent,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        DataLabels = true
                    }
                };
            }

            MessageBox.Show("Assegnazione dei dati ai grafici terminata.");
        }


        private async void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            // Qui si dovrebbe implementare il refresh dei dati reali
            await InitializeChartsAsync();
        }
    }
}
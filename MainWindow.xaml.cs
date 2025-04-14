﻿using System;
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
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace ClientCentralino_vs2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly CallNotificationService _notificationService;
        private Chiamata _selectedCall;

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

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _notificationService = new CallNotificationService(_apiService);

            // Avvia il servizio di notifica
            _notificationService.Start(OnNewCallReceived);


            // Inizializzazione Charts
            _ = InitializeChartsAsync();

            // Visualizzazione contatti incompleti all'avvio dell'app
            _ = ShowIncompleteContactsAsync();

            // Visualizzazione contatti incomppleti tramite tooltip
            _ = UpdateIncompleteContactsTooltipAsync();

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
                MessageBox.Show($"Numero inserito: {phoneNumber}");

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    LoadAllCalls();
                    return;
                }

                var calls = await _apiService.GetCallsByNumberAsync(phoneNumber);
                MessageBox.Show($"Numero chiamate trovate: {calls.Count}");

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

        // Funzione ricerca contatto
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


                //string interno = TxtContactInternal.Text;

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
                    MessageBox.Show("Impossibile salvare il contatto. Campi mancanti (probabile il campo interno).", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel salvataggio del contatto: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // Funzione principale per visualizzare i grafici delle statistiche.
        private async Task InitializeChartsAsync(int days = 7)
        {
            var calls = await _apiService.GetAllCallsAsync();

            // Filtro per giorni recenti
            var dateThreshold = DateTime.Now.AddDays(-days);
            var recentCalls = calls.Where(c => c.DataArrivoChiamata >= dateThreshold).ToList();

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
                ComuniChiamanti = allComuni.Take(10).ToList()
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
                        Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    },
                    new PieSeries
                    {
                        Title = "In uscita",
                        Values = new ChartValues<double> { stats.Outbound },
                        DataLabels = true,
                        Fill = new SolidColorBrush(Color.FromRgb(100, 180, 255))
                    }
                };

                // === Grafico a torta 2 ===
                PieSeries2 = new SeriesCollection {
                    new PieSeries
                    {
                        Title = $"Più chiamato: {stats.TopNumCallsInbound?.Key}",
                        Values = new ChartValues<double> { stats.TopNumCallsInbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(Color.FromRgb(200, 100, 100))
                    },
                    new PieSeries
                    {
                        Title = $"Ha chiamato di più: {stats.TopNumCallsOutbound?.Key}",
                        Values = new ChartValues<double> { stats.TopNumCallsOutbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(Color.FromRgb(100, 200, 100))
                    },
                    new PieSeries
                    {
                        Title = $"Più chiamato in uscita: {stats.MostCalledInOutbound?.Key}",
                        Values = new ChartValues<double> { stats.MostCalledInOutbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(Color.FromRgb(255, 140, 140))
                    },
                    new PieSeries
                    {
                        Title = $"Ha chiamato di più in entrata: {stats.TopCallerInInbound?.Key}",
                        Values = new ChartValues<double> { stats.TopCallerInInbound?.Value ?? 0 },
                        DataLabels = true,
                        Fill = new SolidColorBrush(Color.FromRgb(140, 255, 140))
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
                PieSeries3 = new SeriesCollection();

                // Aggiungi i top 10 comuni
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
                if (stats.AltriComuni.Any())
                {
                    var altriTotal = stats.AltriComuni.Sum(c => c.Value);

                    PieSeries3.Add(new PieSeries
                    {
                        Title = $"Altri comuni ({stats.AltriComuni.Count})",
                        Values = new ChartValues<double> { altriTotal },
                        Tag = stats.AltriComuni, // Memorizza la lista nel Tag
                        DataLabels = true
                    });
                }

                // Assegna colori (con colore grigio per "Altri comuni")
                var colori = new List<Color>
                {
                    Color.FromRgb(255, 180, 0),  // arancione
                    Color.FromRgb(0, 150, 255),   // blu
                    Color.FromRgb(50, 200, 100),  // verde
                    Color.FromRgb(200, 50, 150),  // rosa
                    Color.FromRgb(150, 100, 255), // viola
                    Color.FromRgb(255, 120, 0),   // arancione scuro
                    Color.FromRgb(0, 100, 200),   // blu scuro
                    Color.FromRgb(100, 180, 50),  // verde scuro
                    Color.FromRgb(180, 30, 120),  // magenta
                    Color.FromRgb(120, 80, 220),  // lilla
                    Color.FromRgb(150, 150, 150)  // grigio (per "Altri comuni")
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
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
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
            int days = 7; // default

            var selected = (CbTimeRange.SelectedItem as ComboBoxItem)?.Content?.ToString();

            switch (selected)
            {
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

            // Implementazione refresh dei dati reali
            await InitializeChartsAsync(days);
        }

        private string EstraiComune(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            var match = Regex.Match(input, @"Comune di\s+(.*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }



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


        // Funzione per gestire la visualizzazione dei contatti incompleti all'avvio dell'app
        private async Task ShowIncompleteContactsAsync()
        {
            var incompleteContacts = await _apiService.GetIncompleteContactsAsync();

            if (incompleteContacts.Any())
            {
                var message = new StringBuilder();
                message.AppendLine("Contatti da completare:");
                message.AppendLine("");
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

                MessageBox.Show(message.ToString(), "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // Funzione per gestire la visualizzazione dei contatti incompleti 
        // tramite tooltip nella finestra Contatti.
        private async Task UpdateIncompleteContactsTooltipAsync()
        {
            var contatti = await _apiService.GetIncompleteContactsAsync();

            if (TooltipContattiIncompleti != null)
            {
                if (contatti.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Contatti da completare:\n");

                    foreach (var c in contatti)
                    {
                        sb.AppendLine($"• {c.NumeroContatto} | Int: {c.Interno} | {c.RagioneSociale ?? "NULL"} | {c.Citta ?? "NULL"}");
                    }

                    ((TextBlock)TooltipContattiIncompleti.Content).Text = sb.ToString();
                }
                else
                {
                    ((TextBlock)TooltipContattiIncompleti.Content).Text = "Tutti i contatti sono completi.";
                }
            }
        }

    }
}
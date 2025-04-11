using System;
using System.Windows;
using System.Windows.Controls;
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

            // Posiziona la finestra in basso a destra
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            _apiService = apiService;
            _call = call;
            _onOpenInMainApp = onOpenInMainApp;

            // Popola i dati della chiamata
            TxtCallerNumber.Text = _call.NumeroChiamante;
            TxtCallDate.Text = _call.DataArrivoChiamata.ToString("dd/MM/yyyy HH:mm:ss");
            TxtCallRS.Text = _call.RagioneSocialeChiamante ?? "N/D";

            // Estrai la località dalla ragione sociale (se presente)
            string extractedLocation = ExtractLocationFromRagioneSociale(_call.RagioneSocialeChiamante);
            TxtManualLocation.Text = extractedLocation ?? _call.Locazione ?? "";
            if (TxtManualLocation.Text.Equals(extractedLocation)) 
            { 
                _call.Locazione = extractedLocation;
            }

            // Inizializza la ComboBox con le località
            InitializeLocationsComboBox();
        }

        private async void InitializeLocationsComboBox()
        {
            // Località predefinite
            CmbLocations.Items.Add("Comune di Ali'");
            CmbLocations.Items.Add("Comune di Ali' Terme");
            CmbLocations.Items.Add("Comune di Antillo");
            CmbLocations.Items.Add("Comune di Barcellona Pozzo di Gotto");
            CmbLocations.Items.Add("Comune di Basico'");
            CmbLocations.Items.Add("Comune di Brolo");
            CmbLocations.Items.Add("Comune di Capizzi");
            CmbLocations.Items.Add("Comune di Capri Leone");
            CmbLocations.Items.Add("Comune di Capo D'Orlando");
            CmbLocations.Items.Add("Comune di Casalvecchio Siculo");
            CmbLocations.Items.Add("Comune di Falcone");
            CmbLocations.Items.Add("Comune di Furci");
            CmbLocations.Items.Add("Comune di Furnari");
            CmbLocations.Items.Add("Comune di Gallodoro");
            CmbLocations.Items.Add("Comune di Itala");
            CmbLocations.Items.Add("Comune di Leni");
            CmbLocations.Items.Add("Comune di Letojanni");
            CmbLocations.Items.Add("Comune di Limina");
            CmbLocations.Items.Add("Comune di Longi");
            CmbLocations.Items.Add("Comune di Mandanici");
            CmbLocations.Items.Add("Comune di Mazzarra S. Andrea");
            CmbLocations.Items.Add("Comune di Messina");
            CmbLocations.Items.Add("Comune di Milazzo");
            CmbLocations.Items.Add("Comune di Mistretta");
            CmbLocations.Items.Add("Comune di Mongiuffi Melia");
            CmbLocations.Items.Add("Comune di Montalbano Elicona");
            CmbLocations.Items.Add("Comune di Motta Camastra");
            CmbLocations.Items.Add("Comune di Nizza di Sicilia");
            CmbLocations.Items.Add("Comune di Novara di Sicilia");
            CmbLocations.Items.Add("Comune di Oliveri");
            CmbLocations.Items.Add("Comune di Reitano");
            CmbLocations.Items.Add("Comune di Pace del Mela");
            CmbLocations.Items.Add("Comune di Patti");
            CmbLocations.Items.Add("Comune di Roccafiorita");
            CmbLocations.Items.Add("Comune di Roccalumera");
            CmbLocations.Items.Add("Comune di Sambuca");
            CmbLocations.Items.Add("Comune di Santa Lucia del Mela");
            CmbLocations.Items.Add("Comune di Scaletta Zanclea");
            CmbLocations.Items.Add("Comune di Spadafora");
            CmbLocations.Items.Add("Comune di Terme Vigliatore");
            CmbLocations.Items.Add("Comune di Torrenova");
            CmbLocations.Items.Add("Comune di Tripi");
            CmbLocations.Items.Add("Comune di Venetico");

            CmbLocations.Items.Add("Altro");

            // Se la località esistente è tra quelle predefinite, selezionala (ignorando maiuscole/minuscole)
            // Metodo di supporto per normalizzare il testo
            string Normalize(string text) =>
                new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();

            if (!string.IsNullOrEmpty(_call.Locazione))
            {
                var locNorm = Normalize(_call.Locazione);

                foreach (var item in CmbLocations.Items)
                {
                    string itemText = item.ToString();
                    var itemNorm = Normalize(itemText);

                    if (locNorm == itemNorm)
                    {
                        CmbLocations.SelectedItem = item;
                        try
                        {
                            bool success = await _apiService.UpdateCallLocationAsync(_call.Id, itemText);
                        }
                        catch (Exception ex) 
                        {
                            MessageBox.Show($"Errore durante l'aggiornamento della località: {ex.Message}", "Errore",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                }

                // Se la località non è tra quelle predefinite, seleziona "Altro"
                CmbLocations.SelectedItem = "Altro";
                GridManualLocation.Visibility = Visibility.Visible;
                TxtManualLocation.Text = _call.Locazione;

                // Mostra suggerimento solo se l'utente dovesse modificarla dopo
                if (!Normalize(_call.Locazione).StartsWith("comunedi", StringComparison.OrdinalIgnoreCase))
                {
                    TxtSuggestion.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CmbLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLocations.SelectedItem?.ToString() == "Altro")
            {
                GridManualLocation.Visibility = Visibility.Visible;
                TxtManualLocation.Focus();
            }
            else
            {
                GridManualLocation.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnUpdateLocation_Click(object sender, RoutedEventArgs e)
        {
            string? location = CmbLocations.SelectedItem?.ToString() == "Altro"
                ? TxtManualLocation.Text.Trim()
                : CmbLocations.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(location))
            {
                MessageBox.Show("Selezionare o specificare una località.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Se è stato inserito manualmente ma non inizia con "Comune di ", chiedi conferma
            if (CmbLocations.SelectedItem?.ToString() == "Altro" &&
                !location.StartsWith("Comune di ", StringComparison.CurrentCultureIgnoreCase))
            {
                var result = MessageBox.Show("La località inserita non segue il formato consigliato 'Comune di ...'. " +
                                           "Vuoi procedere comunque?", "Conferma formato",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

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

        private void MostraFinestraPrincipale() 
        {
            if (Application.Current.MainWindow == null)
            {
                // La finestra è stata chiusa: la ricreo
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                Close();
            }
            else
            {
                // Esiste già: la mostro e la porto in primo piano
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                    Close();
                }

                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Topmost = true;  // Hack: lo porta in primo piano
                mainWindow.Topmost = false; // Reset per evitare comportamenti strani
                mainWindow.Focus();
            }
        }

        private void BtnOpenApp_Click(object sender, RoutedEventArgs e)
        {
            MostraFinestraPrincipale();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void TxtManualLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CmbLocations.SelectedItem?.ToString() == "Altro")
            {
                string input = TxtManualLocation.Text.Trim();

                // Mostra il suggerimento se:
                // 1. Il campo non è vuoto
                // 2. Non inizia con "Comune di " (case insensitive)
                if (!string.IsNullOrEmpty(input) &&
                    !input.StartsWith("Comune di ", StringComparison.CurrentCultureIgnoreCase))
                {
                    TxtSuggestion.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtSuggestion.Visibility = Visibility.Collapsed;
                }
            }
        }

        // Estrazione della locazione dalla Regione sociale
        private string ExtractLocationFromRagioneSociale(string ragioneSociale)
        {
            if (string.IsNullOrWhiteSpace(ragioneSociale))
                return null;

            // Se è presente un trattino, prova a considerare solo la parte successiva
            var parts = ragioneSociale.Split('-');
            foreach (var part in parts)
            {
                int index = part.IndexOf("Comune di", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    // Estrai il testo da "Comune di" in poi
                    string location = part.Substring(index).Trim();

                    //MessageBox.Show($"{location}");

                    // Normalizza il formato: "Comune di Xxx"
                    if (location.Length > "Comune di".Length)
                    {
                        location = "Comune di " + char.ToUpper(location["Comune di".Length]) +
                                  location.Substring("Comune di".Length + 1).ToLower();
                        //MessageBox.Show($"{location}");
                    }
                    else
                    {
                        location = "Comune di";
                    }

                    return location;
                }
            }

            return null;
        }

    }
}
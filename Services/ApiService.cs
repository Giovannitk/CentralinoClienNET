using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClientCentralino_vs2.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace ClientCentralino_vs2.Services
{
    public class ApiService
    {
        private HttpClient _client;
        private string _baseAddress;
        private string _authToken;
        private bool _isRefreshingToken = false;
        private readonly object _lockObject = new object();

        public event Action OnSessionExpired;

        public Task LoginTask { get; private set; }

        public ApiService()
        {
            _baseAddress = "http://10.36.150.250:5000/"; // Valore di default
            CreateHttpClient();
            // Perform automatic login on initialization
            //_ = PerformLoginAsync();
            LoginTask = PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            try
            {
                var loginData = new
                {
                    email = "piccirillo@alex.com",
                    password = "piccirillo123" // Replace with actual password
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(loginData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _client.PostAsync("api/auth/login", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                if (loginResponse.success)
                {
                    _authToken = loginResponse.token;
                    // Update the default request headers with the token
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    //MessageBox.Show("Login OK, token: " + _authToken);
                }
                else
                {
                    //MessageBox.Show("Errore durante il login automatico", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                    MessageBox.Show("Errore durante il login automatico: " + loginResponse.message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il login automatico: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> RefreshTokenAsync()
        {
            lock (_lockObject)
            {
                if (_isRefreshingToken)
                    return false;
                _isRefreshingToken = true;
            }

            try
            {
                await PerformLoginAsync();
                return !string.IsNullOrEmpty(_authToken);
            }
            finally
            {
                _isRefreshingToken = false;
            }
        }

        private async Task<T> ExecuteWithTokenRefreshAsync<T>(Func<Task<T>> apiCall)
        {
            try
            {
                return await apiCall();
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("401"))
            {
                // Token scaduto, prova a fare il refresh
                if (await RefreshTokenAsync())
                {
                    // Riprova la chiamata con il nuovo token
                    return await apiCall();
                }
                else
                {
                    // Se il refresh fallisce, mostra il messaggio e riavvia l'app
                    MessageBox.Show("Sessione scaduta. L'applicazione verrà riavviata.", "Token scaduto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RestartApplication();
                    throw; // Rilancia l'eccezione per gestirla nel chiamante
                }
            }
        }

        private void RestartApplication()
        {
            // Serve per notificare chi ascolta che la sessione è scaduta (es: MainWindow)
            OnSessionExpired?.Invoke();

            // Riavvia l'applicazione
            Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            Application.Current.Shutdown();
        }

        private void CreateHttpClient()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(_baseAddress),

                // Tempo di attesa della risposta del server
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        public void UpdateEndpoint(string ip, string port)
        {
            _baseAddress = $"http://{ip}:{port}/";
            CreateHttpClient(); // Ricrea l'HttpClient con il nuovo indirizzo
            // Perform login again with new endpoint
            _ = PerformLoginAsync();
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                var fullUrl = $"{_client.BaseAddress}api/call/test-connection";
                Console.WriteLine($"Testing connection to: {fullUrl}");
                MessageBox.Show($"Testing connection to: {fullUrl}");

                var response = await _client.GetAsync("api/call/test-connection");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Errore: {(int)response.StatusCode} - {response.ReasonPhrase}", "Errore server", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                //MessageBox.Show("Test Connessione riuscito!", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Errore HTTP: {ex.Message}", "Errore di rete", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Timeout nella connessione al server.", "Timeout", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore generico: {ex.Message}", "Errore sconosciuto", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        public async Task<bool> TestCustomConnection(string ip, string port)
        {
            // Evito che l'utente inserisca una porta non valida, differente da un numero tra 1 e 65535
            if (!int.TryParse(port, out int portNumber) || portNumber < 1 || portNumber > 65535)
            {
                MessageBox.Show("Porta non valida. Inserire un numero tra 1 e 65535.", "Errore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                using (var tempClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://{ip}:{port}/"),
                    Timeout = TimeSpan.FromSeconds(5)
                })
                {
                    // Copy the authentication token from the main client if it exists
                    if (!string.IsNullOrEmpty(_authToken))
                    {
                        tempClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    }

                    var response = await tempClient.GetAsync("api/call/test-connection");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il test di connessione: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        //public void UpdateEndpoint(string ip, string port)
        //{
        //    if (int.TryParse(port, out int portNumber))
        //    {
        //        _client.BaseAddress = new Uri($"http://{ip}:{portNumber}/");
        //    }
        //    else
        //    {
        //        throw new ArgumentException("Porta non valida");
        //    }
        //}


        public async Task<List<Chiamata>> GetAllCallsAsync()
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-all-calls");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Chiamata>>(json);
            });
        }

        public async Task<List<Chiamata>> GetCallsByNumberAsync(string phoneNumber)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/get-calls-by-number?phoneNumber={phoneNumber}");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Chiamata>>(json);
            });
        }

        public async Task<Contatto> FindContactAsync(string phoneNumber)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/find-contact?phoneNumber={phoneNumber}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Contatto>(json);
            });
        }

        public async Task<List<Contatto>> GetAllContactsAsync()
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/all-contacts");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Contatto>>(json);
            });
        }

        public async Task<bool> AddContactAsync(Contatto contact)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        NumeroContatto = contact.NumeroContatto,
                        RagioneSociale = contact.RagioneSociale,
                        Citta = contact.Citta,
                        Interno = contact.Interno
                    }),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await _client.PostAsync("api/Call/add-contact", content);
                response.EnsureSuccessStatusCode();
                return true;
            });
        }

        public async Task<Chiamata> FindCallAsync(int callId)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/find-call?callId={callId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Chiamata>(json);
            });
        }

        public async Task<bool> UpdateCallLocationAsync(int callId, string location)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { CallId = callId, Location = location }),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await _client.PutAsync("api/Call/update-call-location", content);
                response.EnsureSuccessStatusCode();
                return true;
            });
        }


        public async Task<List<Contatto>> GetIncompleteContactsAsync()
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-incomplete-contacts");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Contatto>>(json);
            });
        }


        public async Task<bool> DeleteContactAsync(string phoneNumber)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.DeleteAsync($"api/Call/delete-contact?phoneNumber={phoneNumber}");
                return response.IsSuccessStatusCode;
            });
        }

        public async Task<bool> DeleteChiamataByNumeriAsync(string callerNumber, string calledNumber, DateTime endCall)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                string formattedDate = endCall.ToString("o");
                HttpResponseMessage response = await _client.DeleteAsync(
                    $"api/Call/delete-chiamata?callerNumber={Uri.EscapeDataString(callerNumber)}" +
                    $"&calledNumber={Uri.EscapeDataString(calledNumber)}" +
                    $"&endCall={Uri.EscapeDataString(formattedDate)}");

                return response.IsSuccessStatusCode;
            });
        }

        public async Task<bool> DeleteChiamataByUniqueIdAsync(string uniqueId)
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.DeleteAsync(
                    $"api/Call/delete-chiamata-by-id?uniqueId={Uri.EscapeDataString(uniqueId)}");

                return response.IsSuccessStatusCode;
            });
        }

        public async Task<List<IncomingCall>> GetIncomingCallsAsync()
        {
            await LoginTask;
            CheckClient();
            return await ExecuteWithTokenRefreshAsync(async () =>
            {
                HttpResponseMessage response = await _client.GetAsync("api/call/incoming-calls");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<IncomingCall>>(json);
            });
        }

        private void CheckClient()
        {
            if (_client == null)
                throw new InvalidOperationException("HttpClient non inizializzato");
        }
    }

    // Add this class to deserialize the login response
    public class LoginResponse
    {
        public bool success { get; set; }
        public string token { get; set; }
        public string message { get; set; }
        public string role { get; set; }
    }
}
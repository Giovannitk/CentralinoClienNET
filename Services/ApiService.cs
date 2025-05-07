using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClientCentralino_vs2.Models;
using Newtonsoft.Json;

namespace ClientCentralino_vs2.Services
{
    public class ApiService
    {
        private HttpClient _client;
        private string _baseAddress;

        public ApiService()
        {
            _baseAddress = "http://10.36.150.250:5000/"; // Valore di default
            CreateHttpClient();
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

                    // Tempo di attesa della risposta del server
                    Timeout = TimeSpan.FromSeconds(5)
                })
                {
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
            try
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-all-calls");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(json);
                return JsonConvert.DeserializeObject<List<Chiamata>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return new List<Chiamata>();
            }
        }

        public async Task<List<Chiamata>> GetCallsByNumberAsync(string phoneNumber)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/get-calls-by-number?phoneNumber={phoneNumber}");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Chiamata>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return new List<Chiamata>();
            }
        }

        public async Task<Contatto> FindContactAsync(string phoneNumber)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/find-contact?phoneNumber={phoneNumber}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Contatto>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Contatto>> GetAllContactsAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/all-contacts");

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Contatto>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API nel recupero dei contatti: {ex.Message}");
                return new List<Contatto>(); // oppure null, ma meglio una lista vuota per evitare eccezioni
            }
        }




        public async Task<bool> AddContactAsync(Contatto contact)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return false;
            }
        }

        public async Task<Chiamata> FindCallAsync(int callId)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"api/Call/find-call?callId={callId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Chiamata>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateCallLocationAsync(int callId, string location)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { CallId = callId, Location = location }),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await _client.PutAsync("api/Call/update-call-location", content);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return false;
            }
        }


        public async Task<List<Contatto>> GetIncompleteContactsAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-incomplete-contacts");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Contatto>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API nel recupero contatti incompleti: {ex.Message}");
                return new List<Contatto>();
            }
        }


        public async Task<bool> DeleteContactAsync(string phoneNumber)
        {
            try
            {
                HttpResponseMessage response = await _client.DeleteAsync($"api/Call/delete-contact?phoneNumber={phoneNumber}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API nell'eliminazione del contatto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteChiamataByNumeriAsync(string callerNumber, string calledNumber, DateTime endCall)
        {
            try
            {
                // Formatta la data per l'URL
                string formattedDate = endCall.ToString("o"); // ISO 8601 format
                HttpResponseMessage response = await _client.DeleteAsync(
                    $"api/Call/delete-chiamata?callerNumber={Uri.EscapeDataString(callerNumber)}" +
                    $"&calledNumber={Uri.EscapeDataString(calledNumber)}" +
                    $"&endCall={Uri.EscapeDataString(formattedDate)}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API nell'eliminazione della chiamata: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteChiamataByUniqueIdAsync(string uniqueId)
        {
            try
            {
                HttpResponseMessage response = await _client.DeleteAsync(
                    $"api/Call/delete-chiamata-by-id?uniqueId={Uri.EscapeDataString(uniqueId)}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API nell'eliminazione della chiamata: {ex.Message}");
                return false;
            }
        }
    }
}
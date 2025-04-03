using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClientCentralino_vs2.Models;
using Newtonsoft.Json;

namespace ClientCentralino_vs2.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://localhost:7186/");
        }

        public async Task<List<Chiamata>> GetAllCallsAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-all-calls");
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
    }
}
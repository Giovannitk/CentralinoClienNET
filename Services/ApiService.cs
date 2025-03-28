using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClientCentralino.Models;
using Newtonsoft.Json;

namespace ClientCentralino.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        public ApiService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://localhost:7186/");
        }

        public async Task<List<Chiamata>> GetStatisticheAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("api/Call/get-all-calls");
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Risposta API: {json}"); // Logga il JSON
                return JsonConvert.DeserializeObject<List<Chiamata>>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore API: {ex.Message}");
                return new List<Chiamata>(); // Evita crash
            }
        }
    } 
}
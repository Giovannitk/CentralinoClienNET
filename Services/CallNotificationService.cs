using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ClientCentralino_vs2.Models;

namespace ClientCentralino_vs2.Services
{
    public class CallNotificationService : IDisposable
    {
        private readonly ApiService _apiService;
        private Timer _pollingTimer;
        private List<int> _processedCallIds = new List<int>();
        private const int PollingIntervalMs = 5000; // 5 secondi
        private Action<Chiamata> _onNewCallReceived;
        private Timer _incomingCallTimer;
        private List<string> _notifiedIncomingNumbers = new List<string>();
        private Action<IncomingCall> _onIncomingCallReceived;

        public CallNotificationService(ApiService apiService)
        {
            _apiService = apiService;
        }

        public void Start(Action<Chiamata> onNewCallReceived)
        {
            _onNewCallReceived = onNewCallReceived;
            _pollingTimer = new Timer(async (state) => await CheckForNewCalls(), null, 0, PollingIntervalMs);
        }

        private async Task CheckForNewCalls()
        {
            try
            {
                var allCalls = await _apiService.GetAllCallsAsync();
                if (allCalls == null) return;

                // Prende solo le chiamate recenti (ultimi 5 minuti)
                var recentTimeThreshold = DateTime.Now.AddMinutes(-5);
                var recentCalls = allCalls.Where(c =>
                    c.DataArrivoChiamata >= recentTimeThreshold &&
                    !_processedCallIds.Contains(c.Id)).ToList();

                foreach (var call in recentCalls)
                {
                    // Notifica le nuove chiamate tramite callback
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        _onNewCallReceived?.Invoke(call);
                    });

                    _processedCallIds.Add(call.Id);

                    // Limita il numero di ID memorizzati per evitare problemi di memoria
                    if (_processedCallIds.Count > 1000)
                    {
                        _processedCallIds.RemoveRange(0, 500);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il controllo delle nuove chiamate: {ex.Message}");
            }
        }

        public void StartIncomingCallPolling(Action<IncomingCall> onIncomingCallReceived)
        {
            _onIncomingCallReceived = onIncomingCallReceived;
            _incomingCallTimer = new Timer(async (state) => await CheckForIncomingCalls(), null, 0, PollingIntervalMs);
        }

        private async Task CheckForIncomingCalls()
        {
            try
            {
                var incomingCalls = await _apiService.GetIncomingCallsAsync();
                if (incomingCalls == null) return;

                foreach (var call in incomingCalls)
                {
                    // Notifica solo se non già notificato (usiamo numero + orario per evitare duplicati)
                    string uniqueKey = $"{call.NumeroChiamante}_{call.OrarioArrivo:O}";
                    if (!_notifiedIncomingNumbers.Contains(uniqueKey))
                    {
                        Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            _onIncomingCallReceived?.Invoke(call);
                        });
                        _notifiedIncomingNumbers.Add(uniqueKey);
                    }
                }
                // Pulisci la lista se diventa troppo lunga
                if (_notifiedIncomingNumbers.Count > 1000)
                    _notifiedIncomingNumbers.RemoveRange(0, 500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore polling incoming calls: {ex.Message}");
            }
        }

        public void Stop()
        {
            _pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _incomingCallTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _pollingTimer?.Dispose();
            _incomingCallTimer?.Dispose();
        }
    }
}
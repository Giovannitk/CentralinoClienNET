using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClientCentralino.Models;
using ClientCentralino.Services;

namespace ClientCentralino.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<Chiamata> _chiamate;
        private readonly ApiService _apiService;
        private DispatcherTimer _timer;

        public ObservableCollection<Chiamata> Chiamate
        {
            get => _chiamate;
            set
            {
                _chiamate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chiamate)));
            }
        }

        public MainViewModel()
        {
            _apiService = new ApiService();
            Chiamate = new ObservableCollection<Chiamata>();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Aggiorna ogni 5 secondi
            };
            _timer.Tick += async (s, e) => await CaricaDati();
            _timer.Start();

            _ = CaricaDati();
        }

        private async Task CaricaDati()
        {
            var chiamate = await _apiService.GetStatisticheAsync();
            if (chiamate != null && chiamate.Count > 0)
            {
                var ultime15 = chiamate
                    .OrderByDescending(c => c.DataArrivoChiamata) // Ordina per data più recente
                    .Take(15) // Prende solo le ultime 15
                    .ToList();

                Chiamate = new ObservableCollection<Chiamata>(ultime15);
            }
        }
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using ClientCentralino.Models;
using ClientCentralino.Services;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private ObservableCollection<Chiamata> _chiamate;
    private readonly ApiService _apiService;

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
        _ = CaricaDati();
    }

    private async Task CaricaDati()
    {
        var chiamate = await _apiService.GetStatisticheAsync();
        if (chiamate.Count > 0)
        {
            Chiamate = new ObservableCollection<Chiamata>(chiamate);
        }
    }
}
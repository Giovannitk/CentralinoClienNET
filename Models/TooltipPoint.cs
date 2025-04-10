using LiveCharts.Wpf;

namespace ClientCentralino_vs2.Models
{
    public class TooltipPoint
    {
        public PieSeries SeriesView { get; internal set; }
        public object ChartPoint { get; internal set; }
    }
}
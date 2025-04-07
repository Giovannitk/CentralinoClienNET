using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCentralino_vs2.Models
{
    public class CallStatsDto
    {
        public int Inbound { get; set; }
        public int Outbound { get; set; }
        public int Missed { get; set; }
        public int Internal { get; set; }
    }

    public class DailyCallStatsDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCentralino_vs2.Models
{
    public class NotificationSettings
    {
        public bool Enabled { get; set; }
        public int FilterType { get; set; } // 0=Tutti, 1=Numeri, 2=Ragioni sociali, 3=Rubrica
        public List<string>? NumbersOrNames { get; set; }
        public int DurationSeconds { get; set; }
        public int Position { get; set; } // 0=TopRight, 1=BottomRight, ecc.
    }
}

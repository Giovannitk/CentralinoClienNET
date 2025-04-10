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

        public KeyValuePair<string, int>? TopNumCallsInbound { get; set; }   // più chiamato in entrata
        public KeyValuePair<string, int>? TopNumCallsOutbound { get; set; }  // ha chiamato di più in uscita

        public KeyValuePair<string, int>? MostCalledInOutbound { get; set; } // più chiamato in uscita
        public KeyValuePair<string, int>? TopCallerInInbound { get; set; }   // ha chiamato di più in entrata

        public KeyValuePair<string, int>? TopComune { get; set; }

        public List<KeyValuePair<string, int>> ComuniChiamanti { get; set; } = new();
        public List<KeyValuePair<string, int>> AltriComuni { get; set; } = new();

    }



    public class DailyCallStatsDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TopNumCalled() 
    {
        public string? RagioneSocialeChiamante { get; set; }
        public string? RagioneSocialeChiamato { get; set; }
        public int NumCallsInbound {  get; set; }
        public int NumCallsOutbound { get; set; }
    }

}

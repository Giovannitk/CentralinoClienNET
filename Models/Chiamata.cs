using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCentralino_vs2.Models
{
    public class Chiamata
    {
        public int Id { get; set; }
        public string? NumeroChiamante { get; set; }
        public string? NumeroChiamato { get; set; }
        public string? RagioneSocialeChiamante { get; set; }
        public string? RagioneSocialeChiamato { get; set; }
        public DateTime DataArrivoChiamata { get; set; }
        public DateTime DataFineChiamata { get; set; }
        public string? TipoChiamata { get; set; }
        public string? Locazione { get; set; }
    }
}

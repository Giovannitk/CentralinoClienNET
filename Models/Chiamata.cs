using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCentralino.Models
{
    public class Chiamata
    {
        public int Id { get; set; }
        public int NumeroChiamanteID { get; set; }
        public int NumeroChiamatoID { get; set; }
        public string? TipoChiamata { get; set; }
        public DateTime DataArrivoChiamata { get; set; }
        public DateTime DataFineChiamata { get; set; }
    }
}

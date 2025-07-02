using System;

namespace ClientCentralino_vs2.Models
{
    public class IncomingCall
    {
        public string NumeroChiamante { get; set; }
        public string RagioneSociale { get; set; }
        public DateTime OrarioArrivo { get; set; }
    }
}
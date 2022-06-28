using System;
using System.Net;

namespace Server.Structures
{
    public struct StartParameters
    {
        public IPAddress IPAddress { get; set; }

        public short PortConect { get; set; }

        public short PortSendUdp { get; set; }

        public string FilePath { get; set; }

        public TimeSpan TimeoutConfirm { get; set; }
    }
}

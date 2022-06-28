using System.Net;

namespace Server.Structures
{
    public struct StartParameters
    {
        public IPAddress IPAddress { get; set; }

        public short Port { get; set; }

        public string FolderPath { get; set; }
    }
}

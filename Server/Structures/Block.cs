using System;

namespace Server.Structures
{
    [Serializable]
    public struct Block
    {
        public int Id { get; set; }

        public byte[] Data { get; set; }
    }
}

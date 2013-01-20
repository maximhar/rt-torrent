using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class HandshakeMessage:PeerMessage
    {
        private readonly int MessageLength = 68;

        public byte[] PeerID { get; private set; }
        public byte[] Reserved { get; private set; }
        public byte[] InfoHash { get; private set; }
        public string Protocol { get; private set; }

        public override byte[] ToBytes()
        {
            int offset = 0;
            byte[] buffer = new byte[MessageLength];
            offset += Write(buffer, offset, (byte)Protocol.Length);
            offset += WriteAscii(buffer, offset, Protocol);
            offset += Write(buffer, offset, Reserved);
            offset += Write(buffer, offset, InfoHash);
            offset += Write(buffer, offset, PeerID);
            return buffer;
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            ReadByte(buffer, offset);
            this.Protocol = ReadString(buffer, ref offset, 19);
            this.Reserved = ReadBytes(buffer, ref offset, 8);
            this.InfoHash = ReadBytes(buffer, ref offset, 20);
            this.PeerID = ReadBytes(buffer, ref offset, 20);
        }
    }
}

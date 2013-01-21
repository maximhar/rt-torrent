using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class PortMessage:PeerMessage
    {
        public static readonly int Id = 9;
        public ushort Port { get; private set; }

        public PortMessage()
        {
            this.Port = 0;
        }

        public PortMessage(ushort port)
        {
            this.Port = port;
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.Port = (ushort)ReadShort(buffer, ref offset);
        }

        public override int MessageLength
        {
            get { return 7; }
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)9);
            offset += Write(buffer, offset, Port);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Port message: {Port: {0}}", Port);
        }

        public override bool Equals(object obj)
        {
            PortMessage msg = obj as PortMessage;

            if (msg == null)
                return false;
            return this.Port == msg.Port;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Port.GetHashCode();
        }
    }
}

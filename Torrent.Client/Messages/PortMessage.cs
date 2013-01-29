using System;

namespace Torrent.Client.Messages
{
    internal class PortMessage : PeerMessage
    {
        public static readonly int Id = 9;

        public PortMessage()
        {
            Port = 0;
        }

        public PortMessage(ushort port)
        {
            Port = port;
        }

        public ushort Port { get; private set; }

        public override int MessageLength
        {
            get { return 7; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            Port = (ushort) ReadShort(buffer, ref offset);
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 5);
            offset += Write(buffer, offset, (byte) 9);
            offset += Write(buffer, offset, Port);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Port message: Port: {0}", Port);
        }

        public override bool Equals(object obj)
        {
            var msg = obj as PortMessage;

            if (msg == null)
                return false;
            return Port == msg.Port;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Port.GetHashCode();
        }
    }
}
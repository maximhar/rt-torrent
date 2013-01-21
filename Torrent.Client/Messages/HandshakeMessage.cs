using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class HandshakeMessage:PeerMessage
    {
        public string PeerID { get; private set; }
        public byte[] Reserved { get; private set; }
        public byte[] InfoHash { get; private set; }
        public string Protocol { get; private set; }

        public HandshakeMessage(string peerID, byte[] reserved, byte[] infoHash, string protocol)
        {
            this.PeerID = peerID;
            this.Reserved = reserved;
            this.InfoHash = infoHash;
            this.Protocol = protocol;
        }

        public override int MessageLength
        {
            get { return 68; }
        }
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (byte)Protocol.Length);
            offset += WriteAscii(buffer, offset, Protocol);
            offset += Write(buffer, offset, Reserved);
            offset += Write(buffer, offset, InfoHash);
            offset += WriteAscii(buffer, offset, PeerID);
            return offset - start;
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            ReadByte(buffer, offset);
            this.Protocol = ReadString(buffer, ref offset, 19);
            this.Reserved = ReadBytes(buffer, ref offset, 8);
            this.InfoHash = ReadBytes(buffer, ref offset, 20);
            this.PeerID = ReadString(buffer, ref offset, 20);
        }

        public override bool Equals(object obj)
        {
            HandshakeMessage msg = obj as HandshakeMessage;

            if (msg == null)
                return false;
            if (!CompareByteArray(this.InfoHash, msg.InfoHash) || !CompareByteArray(this.Reserved, msg.Reserved))
                return false;
            return (this.PeerID == msg.PeerID && this.Protocol == msg.Protocol);
        }

        public override int GetHashCode()
        {
            return Protocol.GetHashCode() ^ BitConverter.ToString(this.Reserved).GetHashCode() ^
                BitConverter.ToString(this.InfoHash).GetHashCode() ^ this.PeerID.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Handshake message: {Protocol: {0}, Reserved: {1}, InfoHash: {2}, PeerID: {3}",
                this.Protocol, BitConverter.ToString(this.Reserved), BitConverter.ToString(this.InfoHash), this.PeerID);
        }
    }
}

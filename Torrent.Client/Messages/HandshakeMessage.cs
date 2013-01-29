using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    /// <summary>
    /// Provides a container class for the HandshakeMessage data for peer communication.
    /// </summary>
    class HandshakeMessage:PeerMessage
    {
        public static int Length
        {
            get { return 68; }
        }
        /// <summary>
        /// A 20-byte string used as a unique ID for the client.
        /// <para>This is usually the same peer_id that is transmitted in tracker requests.</para>
        /// </summary>
        public string PeerID { get; private set; }
        /// <summary>
        /// An 8-byte array containing reserved bytes.
        /// <para>Usually all eight bytes are set to zero.</para>
        /// </summary>
        public byte[] Reserved { get; private set; }
        /// <summary>
        /// A 20-byte SHA1 hash of the info key in the metainfo file.
        /// <para>This is the same info_hash that is transmitted in tracker requests.</para>
        /// </summary>
        public InfoHash InfoHash { get; private set; }
        /// <summary>
        /// A string identifier of the protocol.
        /// </summary>
        public string Protocol { get; private set; }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.HandshakeMessage class.
        /// </summary>
        public HandshakeMessage()
        {
            this.PeerID = string.Empty;
            this.Reserved = null;
            this.InfoHash = null;
            this.Protocol = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.HandshakeMessage class.
        /// </summary>
        /// <param name="peerID">A 20-byte string used as a unique ID for the client.</param>
        /// <param name="reserved">An 8-byte array containing reserved bytes.</param>
        /// <param name="infoHash">A 20-byte array containing the 20-byte SHA1 hash of the info key in the metainfo file.</param>
        /// <param name="protocol">A string identifier of the protocol.</param>
        public HandshakeMessage(string peerID, byte[] reserved, byte[] infoHash, string protocol)
        {
            Contract.Requires(peerID != null);
            Contract.Requires(reserved.Length == 8);
            Contract.Requires(infoHash.Length == 20);
            Contract.Requires(protocol != null);

            this.PeerID = peerID;
            this.Reserved = reserved;
            this.InfoHash = infoHash;
            this.Protocol = protocol;
        }

        /// <summary>
        /// The length of the HandshakeMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return Length; }
        }

        /// <summary>
        /// Writes the HandshakeMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
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

        /// <summary>
        /// Sets the HandshakeMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count < 68)
                throw new ArgumentException("Message not of sufficient length.");
            ReadByte(buffer, ref offset);
            this.Protocol = ReadString(buffer, ref offset, 19);
            this.Reserved = ReadBytes(buffer, ref offset, 8);
            this.InfoHash = ReadBytes(buffer, ref offset, 20);
            this.PeerID = new string(ReadBytes(buffer, ref offset, 20).Select(b=>(char)b).ToArray());
        }

        /// <summary>
        /// Determines wheteher this HandshakeMessage instance and a specified object, which also must be a HandshakeMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The HandshakeMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            HandshakeMessage msg = obj as HandshakeMessage;

            if (msg == null)
                return false;
            if (!CompareByteArray(this.InfoHash, msg.InfoHash) || !CompareByteArray(this.Reserved, msg.Reserved))
                return false;
            return (this.PeerID == msg.PeerID && this.Protocol == msg.Protocol);
        }

        /// <summary>
        /// Returns the hash code for this HandshakeMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the HandshakeMessage class.</returns>
        public override int GetHashCode()
        {
            return Protocol.GetHashCode() ^ BitConverter.ToString(this.Reserved).GetHashCode() ^
                BitConverter.ToString(this.InfoHash).GetHashCode() ^ this.PeerID.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the content of the HandshakeMessage object.
        /// </summary>
        /// <returns>The string containing the HandshakeMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Handshake message: Protocol: {0}, Reserved: {1}, InfoHash: {2}, PeerID: {3}",
                this.Protocol, BitConverter.ToString(this.Reserved), BitConverter.ToString(this.InfoHash), this.PeerID);
        }
    }
}

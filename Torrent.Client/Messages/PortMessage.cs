using System;

namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the PortMessage data for peer communication.
    /// </summary>
    internal class PortMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 9;

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.PortMessage class.
        /// </summary>
        public PortMessage()
        {
            Port = 0;
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.PortMessage class.
        /// </summary>
        /// <param name="port">The listen port is the port this peer's DHT node is listening on.</param>
        public PortMessage(ushort port)
        {
            Port = port;
        }

        /// <summary>
        /// The listen port is the port this peer's DHT node is listening on.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The length of the PortMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 7; }
        }

        /// <summary>
        /// Sets the PortMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            Port = (ushort) ReadShort(buffer, ref offset);
        }

        /// <summary>
        /// Writes the PortMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 5);
            offset += Write(buffer, offset, (byte) 9);
            offset += Write(buffer, offset, Port);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the PortMessage object.
        /// </summary>
        /// <returns>The string containing the PortMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Port message: Port: {0}", Port);
        }

        /// <summary>
        /// Determines wheteher this PortMessage instance and a specified object, which also must be a PortMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The PortMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var msg = obj as PortMessage;

            if (msg == null)
                return false;
            return Port == msg.Port;
        }

        /// <summary>
        /// Returns the hash code for this PortMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the PortMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Port.GetHashCode();
        }
    }
}
namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the KeepAliveMessage data for peer communication.
    /// </summary>
    internal class KeepAliveMessage : PeerMessage
    {
        /// <summary>
        /// The length of the KeepAliveMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 1; }
        }

        /// <summary>
        /// Sets the KeepAliveMessage properties via a byte array.
        /// <para>This method has no use for the KeepAliveMessage class.</para>
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
        }

        /// <summary>
        /// Writes the KeepAliveMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (byte) 0);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the KeepAliveMessage object.
        /// </summary>
        /// <returns>The string containing the KeepAliveMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("KeepAlive message");
        }

        /// <summary>
        /// Determines wheteher this KeepAliveMessage instance and a specified object, which also must be a KeepAliveMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The KeepAliveMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is KeepAliveMessage;
        }

        /// <summary>
        /// Returns the hash code for this KeepAliveMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the KeepAliveMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode();
        }
    }
}
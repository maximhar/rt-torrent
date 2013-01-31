namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the UnchokeMessage data for peer communication.
    /// </summary>
    internal class UnchokeMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 1;

        /// <summary>
        /// The length of the UnchokeMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 5; }
        }

        /// <summary>
        /// Sets the ChokeMessage properties via a byte array.
        /// <para>This method has no use for the UnchokeMessage class.</para>
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
        }

        /// <summary>
        /// Writes the UnchokeMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 1);
            offset += Write(buffer, offset, (byte) 1);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the UnchokeMessage object.
        /// </summary>
        /// <returns>The string containing the UnchokeMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Unchoke message");
        }

        /// <summary>
        /// Determines wheteher this UnchokeMessage instance and a specified object, which also must be a UnchokeMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The UnchokeMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is UnchokeMessage;
        }

        /// <summary>
        /// Returns the hash code for this UnchokeMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the UnchokeMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}
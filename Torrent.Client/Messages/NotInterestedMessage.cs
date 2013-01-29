namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the NotInterestedMessage data for peer communication.
    /// </summary>
    internal class NotInterestedMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 3;

        /// <summary>
        /// The length of the NotInterestedMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 5; }
        }

        /// <summary>
        /// Sets the NotInterestedMessage properties via a byte array.
        /// <para>This method has no use for the NotInterestedMessage class.</para>
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
        }

        /// <summary>
        /// Writes the NotInterestedMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 1);
            offset += Write(buffer, offset, (byte) 3);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the NotInterestedMessage object.
        /// </summary>
        /// <returns>The string containing the NotInterestedMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("NotInterested message");
        }

        /// <summary>
        /// Determines wheteher this NotInterestedMessage instance and a specified object, which also must be a NotInterestedMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The NotInterestedMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is NotInterestedMessage;
        }

        /// <summary>
        /// Returns the hash code for this NotInterestedMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the NotInterestedMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}
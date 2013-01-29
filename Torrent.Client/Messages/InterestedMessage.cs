namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the InterestedMessage data for peer communication.
    /// </summary>
    class InterestedMessage:PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 2;

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.InterestedMessage class.
        /// </summary>
        public InterestedMessage() { }

        /// <summary>
        /// The length of the InterestedMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 5; }
        }

        /// <summary>
        /// Sets the InterestedMessage properties via a byte array.
        /// <para>This method has no use for the InterestedMessage class.</para>
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        { }

        /// <summary>
        /// Writes the InterestedMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)1);
            offset += Write(buffer, offset, (byte)2);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the InterestedMessage object.
        /// </summary>
        /// <returns>The string containing the InterestedMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Interested message");
        }

        /// <summary>
        /// Determines wheteher this InterestedMessage instance and a specified object, which also must be a InterestedMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The InterestedMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is InterestedMessage;
        }

        /// <summary>
        /// Returns the hash code for this InterestedMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the InterestedMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}

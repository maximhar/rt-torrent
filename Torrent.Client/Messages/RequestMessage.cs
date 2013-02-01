namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the RequestMessage data for peer communication.
    /// </summary>
    internal class RequestMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 6;

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.RequestMessage class.
        /// </summary>
        public RequestMessage()
        {
            Index = -1;
            Offset = -1;
            Length = -1;
        }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.RequestMessage class.
        /// </summary>
        /// <param name="index">The zero-based piece index.</param>
        /// <param name="offset">The zero-based byte offset within the piece.</param>
        /// <param name="length">The requested length.</param>
        public RequestMessage(int index, int offset, int length)
        {
            Index = index;
            Offset = offset;
            Length = length;
        }

        /// <summary>
        /// The zero-based piece index.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The zero-based byte offset within the piece.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// The requested length.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The length of the RequestMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 17; }
        }

        /// <summary>
        /// Sets the RequestMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            ReadInt(buffer, ref offset);
            ReadByte(buffer, ref offset);
            Index = ReadInt(buffer, ref offset);
            Offset = ReadInt(buffer, ref offset);
            Length = ReadInt(buffer, ref offset);
        }

        /// <summary>
        /// Writes the RequestMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 13);
            offset += Write(buffer, offset, (byte) 6);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Offset);
            offset += Write(buffer, offset, Length);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the RequestMessage object.
        /// </summary>
        /// <returns>The string containing the RequestMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Request message: Index: {0}, Offset: {1}, Length: {2}", Index, Offset, Length);
        }

        /// <summary>
        /// Determines wheteher this RequestMessage instance and a specified object, which also must be a RequestMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The RequestMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var msg = obj as RequestMessage;

            if (msg == null)
                return false;
            return Index == msg.Index && Offset == msg.Offset && Length == msg.Length;
        }

        /// <summary>
        /// Returns the hash code for this RequestMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the RequestMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Offset.GetHashCode() ^
                   Length.GetHashCode();
        }
    }
}
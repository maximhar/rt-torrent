namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the PieceMessage data for peer communication.
    /// </summary>
    public class PieceMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 7;

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.PieceMessage class.
        /// </summary>
        public PieceMessage()
        {
            Index = -1;
            Offset = -1;
            Data = null;
        }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.PieceMessage class.
        /// </summary>
        /// <param name="index">The zero-based piece index.</param>
        /// <param name="offset">The zero-based byte offset within the piece.</param>
        /// <param name="data">A block of data, which is a subset of the piece specified by index.</param>
        public PieceMessage(int index, int offset, byte[] data)
        {
            Index = index;
            Offset = offset;
            Data = data;
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
        /// A block of data, which is a subset of the piece specified by index.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The length of the PieceMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 13 + Data.Length; }
        }

        /// <summary>
        /// Sets the PieceMessage properties via a byte array.
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
            Data = ReadBytes(buffer, ref offset, count - offset);
        }

        /// <summary>
        /// Writes the PieceMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 5);
            offset += Write(buffer, offset, (byte) 7);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Offset);
            offset += Write(buffer, offset, Data);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the PieceMessage object.
        /// </summary>
        /// <returns>The string containing the PieceMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Block message: Index: {0}, Offset: {1}, Block.Length: {2}", Index, Offset, Data.Length);
        }

        /// <summary>
        /// Determines wheteher this PieceMessage instance and a specified object, which also must be a PieceMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The PieceMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var msg = obj as PieceMessage;

            if (msg == null)
                return false;
            if (!CompareByteArray(Data, msg.Data))
                return false;
            return Index == msg.Index && Offset == msg.Offset;
        }

        /// <summary>
        /// Returns the hash code for this PieceMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the PieceMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Offset.GetHashCode() ^
                   Data.GetHashCode();
        }
    }
}
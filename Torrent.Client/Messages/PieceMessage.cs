namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the PieceMessage data for peer communication.
    /// </summary>
    internal class PieceMessage : PeerMessage
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
            Begin = -1;
            Block = null;
        }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.PieceMessage class.
        /// </summary>
        /// <param name="index">The zero-based piece index.</param>
        /// <param name="begin">The zero-based byte offset within the piece.</param>
        /// <param name="block">A block of data, which is a subset of the piece specified by index.</param>
        public PieceMessage(int index, int begin, byte[] block)
        {
            Index = index;
            Begin = begin;
            Block = block;
        }

        /// <summary>
        /// The zero-based piece index.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The zero-based byte offset within the piece.
        /// </summary>
        public int Begin { get; private set; }

        /// <summary>
        /// A block of data, which is a subset of the piece specified by index.
        /// </summary>
        public byte[] Block { get; private set; }

        /// <summary>
        /// The length of the PieceMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 13 + Block.Length; }
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
            Begin = ReadInt(buffer, ref offset);
            Block = ReadBytes(buffer, ref offset, count - offset);
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
            offset += Write(buffer, offset, Begin);
            offset += Write(buffer, offset, Block);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the PieceMessage object.
        /// </summary>
        /// <returns>The string containing the PieceMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Piece message: Index: {0}, Begin: {1}, Block.Length: {2}", Index, Begin, Block.Length);
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
            if (!CompareByteArray(Block, msg.Block))
                return false;
            return Index == msg.Index && Begin == msg.Begin;
        }

        /// <summary>
        /// Returns the hash code for this PieceMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the PieceMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^
                   Block.GetHashCode();
        }
    }
}
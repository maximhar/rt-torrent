using System.Diagnostics.Contracts;

namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the HaveMessage data for peer communication.
    /// </summary>
    class HaveMessage:PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 4;
        /// <summary>
        /// The zero-based index of a piece that has been successfully downloaded and verified via the hash.
        /// </summary>
        public int PieceIndex { get; private set; }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.HaveMessage class.
        /// </summary>
        public HaveMessage() 
        {
            PieceIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.HaveMessage class.
        /// </summary>
        /// <param name="pieceIndex">The zero-based index of a piece that has been successfully downloaded.</param>
        public HaveMessage(int pieceIndex)
        {
            Contract.Requires(pieceIndex >= 0);

            this.PieceIndex = pieceIndex;
        }

        /// <summary>
        /// The length of the HaveMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 9; }
        }

        /// <summary>
        /// Sets the HaveMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            ReadInt(buffer, ref offset);
            ReadByte(buffer, ref offset);
            this.PieceIndex = ReadInt(buffer, ref offset);
        }

        /// <summary>
        /// Writes the HaveMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)4);
            offset += Write(buffer, offset, PieceIndex);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the HaveMessage object.
        /// </summary>
        /// <returns>The string containing the HaveMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Have message: PieceIndex: {0}", PieceIndex);
        }

        /// <summary>
        /// Determines wheteher this HaveMessage instance and a specified object, which also must be a HaveMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The HaveMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            HaveMessage msg = obj as HaveMessage;

            if (msg == null)
                return false;
            return this.PieceIndex == msg.PieceIndex;
        }

        /// <summary>
        /// Returns the hash code for this HaveMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the HaveMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ PieceIndex.GetHashCode();
        }
    }
}

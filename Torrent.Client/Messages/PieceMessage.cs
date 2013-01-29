namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the BitfieldMessage data for peer communication.
    /// </summary>
    internal class PieceMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 7;

        public PieceMessage()
        {
            Index = -1;
            Begin = -1;
            Block = null;
        }

        public PieceMessage(int index, int begin, byte[] block)
        {
            Index = index;
            Begin = begin;
            Block = block;
        }

        public int Index { get; private set; }
        public int Begin { get; private set; }
        public byte[] Block { get; private set; }

        public override int MessageLength
        {
            get { return 13 + Block.Length; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            ReadInt(buffer, ref offset);
            ReadByte(buffer, ref offset);
            Index = ReadInt(buffer, ref offset);
            Begin = ReadInt(buffer, ref offset);
            Block = ReadBytes(buffer, ref offset, count - offset);
        }

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

        public override string ToString()
        {
            return string.Format("Piece message: Index: {0}, Begin: {1}, Block.Length: {2}", Index, Begin, Block.Length);
        }

        public override bool Equals(object obj)
        {
            var msg = obj as PieceMessage;

            if (msg == null)
                return false;
            if (!CompareByteArray(Block, msg.Block))
                return false;
            return Index == msg.Index && Begin == msg.Begin;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^
                   Block.GetHashCode();
        }
    }
}
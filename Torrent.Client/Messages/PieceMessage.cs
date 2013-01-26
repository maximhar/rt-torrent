using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    /// <summary>
    /// Provides a container class for the BitfieldMessage data for peer communication.
    /// </summary>
    class PieceMessage:PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 7;
        public int Index { get; private set; }
        public int Begin { get; private set; }
        public byte[] Block { get; private set; }

        public PieceMessage()
        {
            this.Index = -1;
            this.Begin = -1;
            this.Block = null;
        }

        public PieceMessage(int index, int begin, byte[] block)
        {
            this.Index = index;
            this.Begin = begin;
            this.Block = block;
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.Index = ReadInt(buffer, ref offset);
            this.Begin = ReadInt(buffer, ref offset);
            this.Block = ReadBytes(buffer, ref offset, count);
        }

        public override int MessageLength
        {
            get { return 13 + Block.Length; }
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)7);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Begin);
            offset += Write(buffer, offset, Block);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Piece message: {Index: {0}, Begin: {1}, Block.Length: {2}}", Index, Begin, Block.Length);
        }

        public override bool Equals(object obj)
        {
            PieceMessage msg = obj as PieceMessage;

            if (msg == null)
                return false;
            if (!CompareByteArray(this.Block, msg.Block))
                return false;
            return this.Index == msg.Index && this.Begin == msg.Begin;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^ Block.GetHashCode();
        }
    }
}

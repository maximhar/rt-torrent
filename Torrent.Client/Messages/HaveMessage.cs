using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class HaveMessage:PeerMessage
    {
        public static readonly int Id = 4;
        public int PieceIndex { get; private set; }

        public HaveMessage() 
        {
            PieceIndex = -1;
        }

        public HaveMessage(int pieceIndex)
        {
            this.PieceIndex = pieceIndex;
        }

        public override int MessageLength
        {
            get { return 9; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.PieceIndex = ReadInt(buffer, ref offset);
        }
        
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)4);
            offset += Write(buffer, offset, PieceIndex);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Have message: {PieceIndex: {0}}", PieceIndex);
        }

        public override bool Equals(object obj)
        {
            HaveMessage msg = obj as HaveMessage;

            if (msg == null)
                return false;
            return this.PieceIndex == msg.PieceIndex;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ PieceIndex.GetHashCode();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class CancelMessage:PeerMessage
    {
        public static readonly int Id = 8;
        public int Index { get; private set; }
        public int Begin { get; private set; }
        public int Length { get; private set; }

        public CancelMessage() 
        {
            this.Index = -1;
            this.Begin = -1;
            this.Length = -1;
        }

        public CancelMessage(int index, int begin, int length)
        {
            this.Index = index;
            this.Begin = begin;
            this.Length = length;
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.Index = ReadInt(buffer, ref offset);
            this.Begin = ReadInt(buffer, ref offset);
            this.Length = ReadInt(buffer, ref offset);
        }

        public override int MessageLength
        {
            get { return 17; }
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)8);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Begin);
            offset += Write(buffer, offset, Length);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Cancel message: {Index: {0}, Begin: {1}, Length: {2}}", Index, Begin, Length);
        }

        public override bool Equals(object obj)
        {
            CancelMessage msg = obj as CancelMessage;

            if (msg == null)
                return false;
            return this.Index == msg.Index && this.Begin == msg.Begin && this.Length == msg.Length;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^ Length.GetHashCode();
        }
    }
}

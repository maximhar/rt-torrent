using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class BitfieldMessage:PeerMessage
    {
        public static readonly int Id = 5;
        public byte[] Bitfield { get; private set; }
        
        public BitfieldMessage()
        {
            Bitfield = null;
        }

        public BitfieldMessage(byte[] bitfield)
        {
            this.Bitfield = bitfield;
        }

        public override int MessageLength
        {
            get { return 4+1+Bitfield.Length; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.Bitfield = ReadBytes(buffer, ref offset, count);
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)5);
            offset += Write(buffer, offset, Bitfield);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Bitfield message: {Bitfield.Length: {0}}", Bitfield.Length);
        }

        public override bool Equals(object obj)
        {
            BitfieldMessage msg = obj as BitfieldMessage;

            if (msg == null)
                return false;
            return CompareByteArray(this.Bitfield, msg.Bitfield);
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Bitfield.GetHashCode();
        }
    }
}

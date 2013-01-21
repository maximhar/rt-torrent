using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class InterestedMessage:PeerMessage
    {
        public static readonly int Id = 2;

        public InterestedMessage() { }

        public override int MessageLength
        {
            get { return 5; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        { }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)1);
            offset += Write(buffer, offset, (byte)2);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Interested message");
        }

        public override bool Equals(object obj)
        {
            return obj is InterestedMessage;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}

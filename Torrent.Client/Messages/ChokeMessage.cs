using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    class ChokeMessage:PeerMessage
    {
        public static readonly int Id = 0;

        public override int MessageLength
        {
            get { return 1; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {   }
        
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (byte)1);
            offset += Write(buffer, offset, (byte)0);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Choke message");
        }

        public override bool Equals(object obj)
        {
            return obj is ChokeMessage;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}

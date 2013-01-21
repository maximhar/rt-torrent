using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class KeepAliveMessage:PeerMessage
    {

        public KeepAliveMessage() { }

        public override int MessageLength
        {
            get { return 1; }
        }
        
        public override void FromBytes(byte[] buffer, int offset, int count)
        {  }
        
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (byte)0);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("KeepAlive message");
        }

        public override bool Equals(object obj)
        {
            return obj is KeepAliveMessage;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class KeepAliveMessage:PeerMessage
    {

        public KeepAliveMessage() { }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {  }

        public override int MessageLength
        {
            get { return 0; }
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            return 0;
        }

        public override string ToString()
        {
            return string.Format("Keep Alive message");
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

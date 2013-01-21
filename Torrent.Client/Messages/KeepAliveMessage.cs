using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class KeepAliveMessage:PeerMessage
    {
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
    }
}

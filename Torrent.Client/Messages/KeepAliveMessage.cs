using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class KeepAliveMessage:PeerMessage
    {
        public override byte[] ToBytes()
        {
            throw new NotImplementedException();
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

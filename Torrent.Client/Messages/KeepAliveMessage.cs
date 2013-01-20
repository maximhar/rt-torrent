using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class KeepAliveMessage:PeerMessage
    {
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override int MessageLength
        {
            get { throw new NotImplementedException(); }
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }
    }
}

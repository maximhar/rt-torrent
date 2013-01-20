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

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

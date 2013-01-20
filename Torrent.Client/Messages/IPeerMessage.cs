using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    interface IPeerMessage
    {
        byte[] ToBytes();
        void FromBytes(byte[] buffer, int offset, int count);
    }
}

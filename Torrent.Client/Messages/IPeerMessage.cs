using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    interface IPeerMessage
    {
        int MessageLength { get; }
        byte[] ToBytes();
        int ToBytes(byte[] buffer, int offset);
        void FromBytes(byte[] buffer, int offset, int count);
    }
}

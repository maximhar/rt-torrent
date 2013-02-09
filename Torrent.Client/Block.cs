using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public class Block
    {
        public byte[] Data { get; private set; }
        public BlockInfo Info { get; private set; }
        public Block(byte[] data, int pieceIndex, int offset, int length)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(pieceIndex>=0);

            this.Data = data;
            this.Info = new BlockInfo(pieceIndex, offset, length);
        }

        static public BlockInfo FromAbsoluteAddress(long byteOffset, int pieceSize, int length, long maxAbsoluteOffset = 0)
        {
            if (maxAbsoluteOffset == 0) maxAbsoluteOffset = byteOffset + length;
            long offset;
            int block = (int)Math.DivRem(byteOffset, pieceSize, out offset);
            length = (int)Math.Min(maxAbsoluteOffset - byteOffset, length);
            Debug.Assert(block >= 0);
            return new BlockInfo(block, (int)offset, length);
        }

        static public long GetAbsoluteAddress(int pieceIndex, int offset, int pieceSize)
        {
            return (long)pieceIndex*(long)pieceSize + offset;
        }
    }
}

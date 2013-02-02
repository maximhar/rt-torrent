using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public class Piece
    {
        public byte[] Data { get; private set; }
        public PieceInfo Info { get; private set; }
        public Piece(byte[] data, int block, int offset, int length)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(block>=0);

            this.Data = data;
            this.Info = new PieceInfo(block, offset, length);
        }

        static public PieceInfo FromAbsoluteAddress(long byteOffset, int blockSize, int length, long maxAbsoluteOffset = 0)
        {
            if (maxAbsoluteOffset == 0) maxAbsoluteOffset = byteOffset + length;
            long offset;
            int block = (int)Math.DivRem(byteOffset, blockSize, out offset);
            length = (int)Math.Min(maxAbsoluteOffset - byteOffset, length);
            Debug.Assert(block >= 0);
            return new PieceInfo(block, (int)offset, length);
        }

        static public long GetAbsoluteAddress(int index, int offset, int blockSize)
        {
            return index*blockSize + offset;
        }
    }
}

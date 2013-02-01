using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public class Piece
    {
        public byte[] Data { get; private set; }
        public long Offset { get; private set; }
        public long Block { get; private set; }
        public int Length { get; private set; }
        public Piece(byte[] data, long block, long offset, int length)
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(block>=0);
            this.Data = data;
            this.Offset = offset;
            this.Block = block;
            this.Length = length;
        }
    }
}

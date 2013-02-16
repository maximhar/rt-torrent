using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Torrent.Client.Extensions;

namespace Torrent.Client
{
    public class BlockStrategist
    {
        public BitArray Bitfield { get; private set; }
        private object syncRoot = new object();
        private readonly int blockSize;
        private readonly int pieceSize;
        private readonly int blocksPerPiece;
        private readonly long totalSize;
        private readonly int blockCount;
        private readonly BlockAddressCollection<int> unavailable;
        private readonly int[] pieces;
        private int available = 0;

        public BlockStrategist(TorrentData data, int blockSize = 16*1024)
        {
            this.blockSize = blockSize;
            pieceSize = data.PieceLength;
            blocksPerPiece = pieceSize/this.blockSize;
            totalSize = data.Files.Sum(f => f.Length);
            blockCount = (int)Math.Ceiling((float)totalSize/blockSize);
            pieces = new int[data.PieceCount];
            Bitfield = new BitArray(data.PieceCount);
            unavailable = new BlockAddressCollection<int>();
            for (int i = 0; i < blockCount; i++)
                unavailable.Add(i);

            for(int i = 0; i < data.PieceCount-1; i++)
            {
                pieces[i]= data.PieceLength;
            }
            int lastLength = (int)(data.TotalLength - (data.PieceLength*data.PieceCount - 1));
            pieces[pieces.Length - 1] = lastLength;
        }

        public BlockInfo Next(BitArray bitfield)
        {
            if (available == blockCount)
                return BlockInfo.Empty;
            BlockInfo block;
            int counter = 0;
            do
            {
                counter++;
                int index;
                lock(unavailable)
                {
                    if (unavailable.Any())
                        index = unavailable.Random();
                    else return BlockInfo.Empty;
                }
                
                block = Block.FromAbsoluteAddress((long)index*blockSize, pieceSize, blockSize,
                                                 totalSize);
                if (counter > 10)
                    return block;
            } while (!bitfield[block.Index]);

            Debug.WriteLine("Strategist requested block " + block.Index);
            return block;
        }

        public bool Complete()
        {
            lock (unavailable)
                return !unavailable.Any();
        }

        public bool EndGame()
        {
            return unavailable.Count < (blockCount/100);
        }

        public bool Received(BlockInfo block)
        {
            int address = (int)(Block.GetAbsoluteAddress(block.Index, block.Offset, pieceSize)/blockSize);
            lock (unavailable)
            {
                if(unavailable.Contains(address) && block.Length > 0)
                {
                    Debug.WriteLine("Needed block incoming:" + address);
                    unavailable.Remove(address);
                    available++;
                    pieces[block.Index] -= block.Length;
                    if(pieces[block.Index]<=0)
                    {
                        Bitfield.Set(block.Index, true);
                    }
                    return true;
                }
                Debug.WriteLine("Unneeded block incoming:" + address);
                return false;
            }
        }
    }

    public class BlockAddressCollection<T>:KeyedCollection<int,int>
    {
        protected override int GetKeyForItem(int item)
        {
            return item;
        }
    }

    
}
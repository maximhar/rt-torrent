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
    public class PieceStrategist
    {
        private object syncRoot = new object();
        private readonly int pieceSize;
        private readonly int blockSize;
        private readonly int piecesPerBlock;
        private readonly long totalSize;
        private readonly int pieceCount;
        private readonly PieceAddressCollection<int> unavailable; 
        private int available = 0;

        public PieceStrategist(TorrentData data, int pieceSize = 16*1024)
        {
            this.pieceSize = pieceSize;
            blockSize = data.PieceLength;
            piecesPerBlock = blockSize/this.pieceSize;
            totalSize = data.Files.Sum(f => f.Length);
            pieceCount = (int)Math.Ceiling((float)totalSize/pieceSize);
            unavailable = new PieceAddressCollection<int>();
            for (int i = 0; i < pieceCount; i++)
                unavailable.Add(i);
        }

        public PieceInfo Next(BitArray bitfield)
        {
            if (available == pieceCount)
                return PieceInfo.Empty;
            PieceInfo piece;
            int counter = 0;
            do
            {
                counter++;
                int index;
                lock(unavailable)
                {
                    index = unavailable.Random();
                }
                
                piece = Piece.FromAbsoluteAddress((long)index*pieceSize, blockSize, pieceSize,
                                                 totalSize);
                if (counter > 100) 
                    return PieceInfo.Empty;
            } while (!bitfield[piece.Index]);

            //Debug.WriteLine("Strategist requested piece " + piece.Index);
            return piece;
        }

        public bool Complete()
        {
            return available == pieceCount;
        }

        public bool EndGame()
        {
            return unavailable.Count < (pieceCount/100);
        }

        public void Received(PieceInfo piece)
        {
            int address = (int)(Piece.GetAbsoluteAddress(piece.Index, piece.Offset, blockSize)/pieceSize);
            lock (unavailable)
            {
                if(unavailable.Contains(address))
                {
                    unavailable.Remove(address);
                    available++;
                }
            }
        }

        public bool Need(PieceInfo piece)
        {
            long address = Piece.GetAbsoluteAddress(piece.Index, piece.Offset, blockSize) / pieceSize;
            
            bool res = unavailable.Contains((int)address);
            Debug.WriteLine("Piece incoming:" + address + " | need: " + res);
            return res;
        }
    }

    public class PieceAddressCollection<T>:KeyedCollection<int,int>
    {
        protected override int GetKeyForItem(int item)
        {
            return item;
        }
    }
}
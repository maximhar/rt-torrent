using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public class HashingMode:TorrentMode
    {
        private readonly SHA1 hasher = new SHA1Cng();
        private byte[] pieceBuffer;
        private int remainingPieces;

        public HashingMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
                                base(manager, strategist, metadata, monitor)
        {}

        public override void Start()
        {
            base.Start();
            Task.Factory.StartNew(StartTask);
        }

        private void StartTask()
        {
            if(!Directory.Exists(Metadata.Name))
            {
                Stop(true);
                OnHashingComplete();
                return;
            }
            pieceBuffer = new byte[Metadata.PieceLength];
            remainingPieces = Metadata.PieceCount;
            int lastPieceLength = (int) (Metadata.TotalLength - (Metadata.PieceLength*(Metadata.PieceCount - 1)));

            for(int i = 0; i < Metadata.PieceCount - 1; i++)
            {
                if(Stopping) return;
                try
                {
                    BlockManager.GetBlock(pieceBuffer, i, 0, Metadata.PieceLength, PieceRead, i);
                }
                catch
                {
                    Trace.Write("Block " + i + " unavailable (catch)");
                }
            }
            try
            {
                BlockManager.GetBlock(pieceBuffer, Metadata.PieceCount - 1, 0, lastPieceLength, PieceRead, Metadata.PieceCount - 1);
            }
            catch
            {
                Trace.Write("Block " + (Metadata.PieceCount-1) + " unavailable (catch)");
            }
        }

        private void PieceRead(bool success, Block block, object state)
        {
            if (Stopping) return;
            Interlocked.Decrement(ref remainingPieces);
            int piece = (int)state;
            if (success)
            {
                if(HashCheck(block))
                {
                    MarkAvailable(piece);
                }
            }

            if(remainingPieces == 0)
            {
                Stop(true);
                OnHashingComplete();
            }
        }

        private bool HashCheck(Block block)
        {
            var hash = hasher.ComputeHash(block.Data, 0, block.Info.Length);
            return hash.SequenceEqual(Metadata.Checksums[block.Info.Index]);
        }

        private void MarkAvailable(int piece)
        {
            int blocksPerPiece = Metadata.PieceLength/Global.Instance.BlockSize;
            int blockSize = Global.Instance.BlockSize;
            for(int i=0;i<blocksPerPiece;i++)
            {
                BlockStrategist.Received(new BlockInfo(piece, blockSize*i, blockSize));
            }
        }

        protected override void HandleRequest(RequestMessage request, PeerState peer) {}

        protected override void HandlePiece(PieceMessage piece, PeerState peer) {}

        public event EventHandler HashingComplete;

        private void OnHashingComplete()
        {
            EventHandler handler = HashingComplete;
            if(handler != null) handler(this, new EventArgs());
        }
    }
}

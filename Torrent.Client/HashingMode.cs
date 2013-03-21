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
            if(!Directory.Exists(BlockManager.MainDirectory))
            {
                Stop(true);
                OnHashingComplete();
                return;
            }
            pieceBuffer = new byte[Metadata.PieceLength];
            remainingPieces = Metadata.PieceCount;
            int lastPieceLength = (int) (Metadata.TotalLength - (Metadata.PieceLength*(Metadata.PieceCount - 1)));
            //цикъл за проверка всеки блок (без последния, който може да бъде с произволен размер)
            for(int i = 0; i < Metadata.PieceCount - 1; i++)
            {
                if(Stopping) return;
                try
                {   //асинхронна заявка за прочитане на блок от файловата система
                    BlockManager.GetBlock(pieceBuffer, i, 0, Metadata.PieceLength, PieceRead, i);
                }
                catch
                {
                    Trace.WriteLine("Block " + i + " unavailable (catch)");
                }
            }
            try
            {
                BlockManager.GetBlock(pieceBuffer, Metadata.PieceCount - 1, 0, lastPieceLength, PieceRead, Metadata.PieceCount - 1);
            }
            catch
            {
                Trace.WriteLine("Block " + (Metadata.PieceCount-1) + " unavailable (catch)");
            }
        }

        private void PieceRead(bool success, Block block, object state)
        {
            if (Stopping) return; 
            Interlocked.Decrement(ref remainingPieces);//безопасно декрементиране на брояча
            int piece = (int)state;
            if (success)
            {   //заключване на прочетения блок
                lock(block.Data)
                if(HashCheck(block))
                {   //ако хеш проверката мине, блока се маркира като наличен
                    MarkAvailable(piece);
                }
                else
                {   //в противен случай, на trace изхода се извежда съобщение
                    Trace.WriteLine("Block " + piece + " unavailable (hash)");
                }
            }
            else
            {
                Trace.WriteLine("Block " + piece + " unavailable (!success)");
            }
            //ако остават 0 парчета за проверяване, процесът е завършил
            //спиране и съобщение за приключване
            if(remainingPieces == 0)
            {
                Stop(true);
                OnHashingComplete();
            }
        }

        private bool HashCheck(Block block)
        {   //изчисляване на хеш стойността на блока
            var hash = hasher.ComputeHash(block.Data, 0, block.Info.Length);
            //сверяване на получения хеш код със този, указан в метаданните
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

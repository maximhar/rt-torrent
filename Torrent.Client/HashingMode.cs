using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    class HashingMode:TorrentMode
    {
        public HashingMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
                                base(manager, strategist, metadata, monitor)
        {
            int lastPieceLength = (int)(Metadata.TotalLength - (Metadata.PieceLength * (Metadata.PieceCount - 1)));
            for(int i=0;i<Metadata.PieceCount-1;i++)
            {
                try
                {
                    BlockManager.GetBlock(i,0,Metadata.PieceLength,PieceRead,i);
                }
                catch
                {
                    MarkUnavailable(i);
                }
            }
            try
            {
                BlockManager.GetBlock(Metadata.PieceCount - 1, 0, lastPieceLength, PieceRead, Metadata.PieceCount - 1);
            }
            catch(Exception)
            {
                MarkUnavailable(Metadata.PieceCount-1);
            }
            
        }

        private void MarkUnavailable(int piece)
        {
            throw new NotImplementedException();
        }

        private void PieceRead(bool success, Block block, object state)
        {
            int piece = (int)state;
            if(!success)
            {
                MarkUnavailable(piece);
            }
            else
            {
                HashCheck(block);
            }
        }

        private void HashCheck(Block block)
        {
            throw new NotImplementedException();
        }

        private void MarkAvailable(int piece)
        {
            throw new NotImplementedException();
        }


        protected override void HandleRequest(RequestMessage request, PeerState peer) {}

        protected override void HandlePiece(PieceMessage piece, PeerState peer) {}


    }
}

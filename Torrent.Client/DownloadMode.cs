using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public class DownloadMode:TorrentMode
    {
        private const int RequestsQueueLength = 10;
        private bool enableEndGame = true;

        public BitArray Bitfield { get; private set; }

        public DownloadMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
                                base(manager, strategist, metadata, monitor)
        {
            Timer endGameTimer = new Timer(10000);
            endGameTimer.Elapsed += (o, e) => enableEndGame = true;
        }

        protected override void HandleRequest(RequestMessage request, PeerState peer)
        {
            if(!peer.IsChoked)
            {
                BlockManager.GetBlock(request.Index, request.Offset, request.Length, BlockRead, peer);
            }
        }

        protected override void HandlePiece(PieceMessage piece, PeerState peer)
        {
            var blockInfo = new BlockInfo(piece.Index, piece.Offset, piece.Data.Length);
            if(BlockStrategist.Received(blockInfo))
            {
                var block = new Block(piece.Data, piece.Index, piece.Offset, piece.Data.Length);
                BlockManager.AddBlock(block, BlockWritten, block);
            }
            peer.PendingBlocks--;
            if (BlockStrategist.EndGame()) DoEndGame();
            SendBlockRequests(peer);
        }

        protected override void HandleUnchoke(UnchokeMessage unchoke, PeerState peer)
        {
            base.HandleUnchoke(unchoke, peer);
            SendBlockRequests(peer);
        }

        protected override void HandleBitfield(BitfieldMessage bitfield, PeerState peer)
        {
            base.HandleBitfield(bitfield, peer);

            if(!peer.NoBlocks)
            {
                SendMessage(peer, new InterestedMessage());
            }
        }

        private void SendBlockRequests(PeerState peer)
        {
            int count = RequestsQueueLength - peer.PendingBlocks;
            for(int i=0;i<count;i++)
            {
                var block = BlockStrategist.Next(peer.Bitfield);
                if (block != BlockInfo.Empty) 
                {
                    SendMessage(peer, new RequestMessage(block.Index, block.Offset, block.Length));
                    peer.PendingBlocks++;
                }
                else if (BlockStrategist.Complete())
                {
                    OnDownloadComplete();
                    return;
                }
            }
        }

        private void DoEndGame()
        {
            if (!enableEndGame) return;
            enableEndGame = false;

            foreach (var peerState in Peers.Values)
            {
                SendBlockRequests(peerState);
                if(peerState.AmChoked)
                    SendMessage(peerState, new InterestedMessage());
            }
        }

        private void BlockWritten(bool success, object state)
        {
            var block = (Block)state;
            if(success) Monitor.Written(block.Info.Length);
            if (Monitor.BytesWritten >= Metadata.TotalLength)
                AllWrittenToDisk();
        }

        private void BlockRead(bool success, Block block, object state)
        {
            var peer = (PeerState)state;
            try
            {
                if (success)
                {
                    Monitor.Read(block.Info.Length);
                    SendMessage(peer, new PieceMessage(block.Info.Index, block.Info.Offset, block.Data));
                }
            }
            catch(Exception e)
            {
                OnRaisedException(e);
            }
        }

        private void AllWrittenToDisk()
        {
            Stop(true);
            OnFlushedToDisk();
        }

        private void OnDownloadComplete()
        {
            EventHandler handler = DownloadComplete;
            if(handler != null) handler(this, new EventArgs());
        }

        private void OnFlushedToDisk()
        {
            EventHandler handler = FlushedToDisk;
            if(handler != null) handler(this, new EventArgs());
        }

        public event EventHandler DownloadComplete;
        public event EventHandler FlushedToDisk;
    }
}

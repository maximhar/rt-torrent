using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public class DownloadMode:TorrentMode
    {
        private const int RequestsQueueLength = 10;
        private long pendingWrites = 0;

        public DownloadMode(HashingMode hashMode):this(new BlockManager(hashMode.Metadata, hashMode.BlockManager.MainDirectory),
            hashMode.BlockStrategist, hashMode.Metadata, hashMode.Monitor)
        { 
        }

        public DownloadMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
                                base(manager, strategist, metadata, monitor)
        {
            strategist.HavePiece += (sender, args) => SendHaveMessages(args.Value);
        }

        public override void Start()
        {
            base.Start();
            if(BlockStrategist.Complete)
            {
                OnDownloadComplete();
                OnFlushedToDisk();
                Stop(true);
                return;
            }
            
            PeerListener.Register(Metadata.InfoHash, peer => SendHandshake(peer, DefaultHandshake));
        }

        public override void Stop(bool closeStreams)
        {
            base.Stop(closeStreams);
            PeerListener.Deregister(Metadata.InfoHash);
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
                WriteBlock(piece);
            }
            peer.PendingBlocks--;
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

        protected override bool AddPeer(PeerState peer)
        {
            SendBitfield(peer);
            return base.AddPeer(peer);
        }

        private void SendBitfield(PeerState peer)
        {
            SendMessage(peer, new BitfieldMessage(BlockStrategist.Bitfield));
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
                else if (BlockStrategist.Complete)
                {
                    OnDownloadComplete();
                    return;
                }
            }
        }

        private void SendHaveMessages(int piece)
        {
            foreach(var peer in Peers.Values)
            {
                if(!peer.Bitfield[piece])
                    SendMessage(peer, new HaveMessage(piece));
            }
        }

        private void WriteBlock(PieceMessage piece)
        {
            try
            {
                var block = new Block(piece.Data, piece.Index, piece.Offset, piece.Data.Length);
                BlockManager.AddBlock(block, BlockWritten, block);
                Interlocked.Add(ref pendingWrites, piece.Data.Length);
            }
            catch(Exception e)
            {
                OnRaisedException(e);
            }
        }

        private void BlockWritten(bool success, object state)
        {
            var block = (Block)state;
            if(success)
            {
                Monitor.Written(block.Info.Length);
                Interlocked.Add(ref pendingWrites, -block.Info.Length);
            }
            if (BlockStrategist.Complete && pendingWrites==0)
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

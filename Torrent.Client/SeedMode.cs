using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    class SeedMode:TorrentMode
    {
        private const int MaxConnectedPeers = 100;

        public SeedMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor) :
                                base(manager, strategist, metadata, monitor) {}

        public SeedMode(TorrentMode oldMode):this(new BlockManager(oldMode.Metadata, oldMode.BlockManager.MainDirectory),
            oldMode.BlockStrategist, oldMode.Metadata, oldMode.Monitor)
        {

        }

        public override void Start()
        {
            base.Start();
            PeerListener.Register(Metadata.InfoHash, peer => SendHandshake(peer, DefaultHandshake));
        }

        public override void Stop(bool closeStreams)
        {
            base.Stop(closeStreams);
            PeerListener.Deregister(Metadata.InfoHash);
        }

        protected override void HandleRequest(RequestMessage request, PeerState peer)
        {
            if (!peer.IsChoked && request.Length <= Global.Instance.BlockSize)
            {
                BlockManager.GetBlock(new byte[request.Length], request.Index, request.Offset, request.Length, BlockRead, peer);
            }
        }

        protected override bool AddPeer(PeerState peer)
        {
            if (Peers.Count >= MaxConnectedPeers) return false;

            SendBitfield(peer);
            return base.AddPeer(peer);
        }

        private void SendBitfield(PeerState peer)
        {
            SendMessage(peer, new BitfieldMessage(BlockStrategist.Bitfield));
        }

        protected override void HandleInterested(InterestedMessage interested, PeerState peer)
        {
            base.HandleInterested(interested, peer);
            peer.IsChoked = false;
            SendMessage(peer, new UnchokeMessage());
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
            catch (Exception e)
            {
                OnRaisedException(e);
            }
        }

        protected override void HandlePiece(PieceMessage piece, PeerState peer) {}
    }
}

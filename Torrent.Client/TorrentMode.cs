using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Torrent.Client.Events;
using Torrent.Client.Messages;
using Torrent.Client.Extensions;
namespace Torrent.Client
{
    public abstract class TorrentMode
    {
        public ConcurrentDictionary<string, PeerState> Peers { get; private set; }
        public BlockManager BlockManager { get; private set; }
        public BlockStrategist BlockStrategist { get; private set; }
        public TorrentData Metadata { get; private set; }
        public TransferMonitor Monitor { get; private set; }

        protected HandshakeMessage DefaultHandshake;
        protected bool Stopping = false;

        protected TorrentMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor)
        {
            Monitor = monitor;
            BlockManager = manager;
            BlockStrategist = strategist;
            Metadata = metadata;
            DefaultHandshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], Metadata.InfoHash, "BitTorrent protocol");
            Peers = new ConcurrentDictionary<string, PeerState>();
            manager.RaisedException += (s, e) => OnRaisedException(e.Value);
        }

        public virtual void Start()
        {
            if (!Stopping) return;
            Stopping = false;
            PeerListener.Register(Metadata.InfoHash, peer => SendHandshake(peer, DefaultHandshake));
        }

        public virtual void Stop(bool force)
        {
            if (Stopping) return;
            Stopping = true;
            Peers.Clear();
            PeerListener.Deregister(Metadata.InfoHash);
            if(force)
                BlockManager.Dispose();
        }

        public virtual void AddEndpoints(IEnumerable<IPEndPoint> endpoints)
        {
            foreach(var ep in endpoints)
            {
                ConnectPeer(ep);
            }
        }

        protected virtual void HandleMessage(PeerMessage message, PeerState peer)
        {
            if (message is HandshakeMessage) HandleHandshake((HandshakeMessage)message, peer);
            else if (message is ChokeMessage) HandleChoke((ChokeMessage)message, peer);
            else if (message is UnchokeMessage) HandleUnchoke((UnchokeMessage)message, peer);
            else if (message is InterestedMessage) HandleInterested((InterestedMessage)message, peer);
            else if (message is NotInterestedMessage) HandleNotInterested((NotInterestedMessage)message, peer);
            else if (message is BitfieldMessage) HandleBitfield((BitfieldMessage)message, peer);
            else if (message is HaveMessage) HandleHave((HaveMessage)message, peer);
            else if (message is PieceMessage) HandlePiece((PieceMessage)message, peer);
            else if (message is RequestMessage) HandleRequest((RequestMessage)message, peer);

            ReceiveMessage(peer);
        }

        protected abstract void HandleRequest(RequestMessage request, PeerState peer);
        protected abstract void HandlePiece(PieceMessage piece, PeerState peer);

        protected virtual void HandleHandshake(HandshakeMessage handshake, PeerState peer)
        {
            peer.ReceivedHandshake = true;
            peer.ID = handshake.PeerID;
            if (!peer.SentHandshake) SendHandshake(peer, DefaultHandshake);
            else AddPeer(peer);
        }

        protected virtual void HandleChoke(ChokeMessage choke, PeerState peer)
        {
            peer.AmChoked = true;
        }

        protected virtual void HandleUnchoke(UnchokeMessage unchoke, PeerState peer)
        {
            peer.AmChoked = false;
        }

        protected virtual void HandleInterested(InterestedMessage interested, PeerState peer)
        {
            peer.IsInterested = true;
        }

        protected virtual void HandleNotInterested(NotInterestedMessage notInterested, PeerState peer)
        {
            peer.IsInterested = false;
        }

        protected virtual void HandleBitfield(BitfieldMessage bitfield, PeerState peer)
        {
            bitfield.Bitfield.CopyTo(peer.Bitfield, 0, 0, Metadata.PieceCount);
        }

        protected virtual void HandleHave(HaveMessage have, PeerState peer)
        {
            if(have.PieceIndex < peer.Bitfield.Count)
                peer.Bitfield[have.PieceIndex] = true;
        }

        protected virtual void ConnectPeer(IPEndPoint ep)
        {
            if (Stopping) return;
            var peerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            NetworkIO.Connect(peerSocket, ep, peerSocket, PeerConnected);
        }

        protected virtual void PeerConnected(bool success, int transmitted, object state)
        {
            if (Stopping) return;
            var socket = (Socket)state;
            if(success)
            {
                var newPeer = PeerState.FromSocket(socket, Metadata.PieceCount);
                SendHandshake(newPeer, DefaultHandshake);
            }
        }

        protected virtual bool AddPeer(PeerState peer)
        {
            return Peers.TryAdd(peer.ID, peer);
        }

        protected virtual void SendHandshake(PeerState peer, HandshakeMessage handshake)
        {
            if (Stopping) return;
            MessageIO.SendMessage(peer.Socket, handshake, peer, HandshakeSent);
        }

        protected virtual void HandshakeSent(bool success, int sent, object state)
        {
            if (Stopping) return;
            var peer = (PeerState)state;
            if (success)
            {
                Monitor.Sent(sent);
                peer.SentHandshake = true;
                peer.Bitfield = new BitArray(Metadata.PieceCount);
                if (!peer.ReceivedHandshake) ReceiveHandshake(peer);
                else AddPeer(peer);
            }
        }

        protected virtual void ReceiveHandshake(PeerState peer)
        {
            if (Stopping) return;
            MessageIO.ReceiveHandshake(peer.Socket, peer, MessageReceived);
        }

        protected virtual void SendMessage(PeerState peer, PeerMessage message)
        {
            if (Stopping) return;
            MessageIO.SendMessage(peer.Socket, message, peer, MessageSent);
        }

        protected virtual void MessageSent(bool success, int sent, object state)
        {
            if (Stopping) return;
            if (success) Monitor.Sent(sent);
        }

        protected virtual void ReceiveMessage(PeerState peer)
        {
            if (Stopping) return;
            MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
        }

        protected virtual void MessageReceived(bool success, PeerMessage message, object state)
        {
            if (Stopping) return;
            var peer = (PeerState)state;
            try
            {
                if (success)
                {
                    Monitor.Received(message.MessageLength);
                    HandleMessage(message, peer);
                }
                else
                {
                    CloseSocket(peer);
                    ConnectPeer(peer.EndPoint);
                }
            }
            catch(Exception e)
            {
                OnRaisedException(e);
            }
        }

        protected virtual void CloseSocket(PeerState peer)
        {
            try
            {
                lock (peer)
                    if (peer.Socket != null)
                    {
                        Debug.WriteLine("Closing socket with " + peer.Socket.RemoteEndPoint);
                        peer.Socket.Shutdown(SocketShutdown.Both);
                        peer.Socket.Close();
                        RemovePeer(peer.ID);
                    }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e, "TransferManager.ClosePeerSocket");
            }
        }

        protected virtual void RemovePeer(string id)
        {
            if (id == null) return;
            PeerState removed;
            Peers.TryRemove(id, out removed);
        }

        public event EventHandler<EventArgs<Exception>> RaisedException;

        public void OnRaisedException(Exception e)
        {
            if (Stopping) return;
            EventHandler<EventArgs<Exception>> handler = RaisedException;
            if(handler != null) handler(this, new EventArgs<Exception>(e));
        }
    }
}

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Torrent.Client.Events;
using Torrent.Client.Messages;
using Torrent.Client.Extensions;
namespace Torrent.Client
{
    public delegate Piece RequestPieceDelegate(PieceInfo pieceInfo);

    public delegate void ObtainedPieceDelegate(Piece piece);

    public class TransferManager : IDisposable
    {
        private readonly ObtainedPieceDelegate obtainedPiece;
        private int queueLimit = 10;
        private readonly PieceStrategist strategist;
        private readonly TorrentData torrentData;
        private bool disposed;
        private HandshakeMessage handshake;
        private volatile bool stop;
        public List<PeerState> tops;
        public int peersWhoChokedMe;
        private Timer firePeersEvent;
        public TransferManager(TorrentData data, RequestPieceDelegate requestHandler,
                               ObtainedPieceDelegate obtainedHandler)
        {
            Peers = new ConcurrentDictionary<string, PeerState>();
            torrentData = data;
            strategist = new PieceStrategist(data);
            obtainedPiece = obtainedHandler;
            handshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], data.InfoHash, "BitTorrent protocol");
            tops = new List<PeerState>();
        }

        public ConcurrentDictionary<string, PeerState> Peers { get; private set; }

        public TransferState State { get; private set; }

        public void Start(IEnumerable<IPEndPoint> peerEndpoints)
        {
            if(disposed) throw new TorrentException("Transfer manager disposed.");
            stop = false;
            ChangeState(TransferState.Downloading);
            handshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], torrentData.InfoHash,
                                             "BitTorrent protocol");
            Connect(peerEndpoints);
            firePeersEvent = new Timer(FirePeers);
            firePeersEvent.Change(200, 1000);
        }

        private void FirePeers(object state)
        {
            if (stop) return;
            if(strategist.EndGame())
            {
                foreach(var peerState in Peers.Values)
                {
                    SendRequests(peerState);
                }
            }
            foreach(var peerState in Peers.Values)
            {
                if(peerState.AmChoked)
                    SelectPeer(peerState);

            }
        }

        public void Stop(bool success)
        {
            if(disposed) throw new TorrentException("Transfer manager disposed.");
            stop = true;
            if(firePeersEvent!=null)
                firePeersEvent.Dispose();
            OnStopping(success);
        }

        public void AddEndpoints(IEnumerable<IPEndPoint> peerEndpoints)
        {
            if(disposed) throw new TorrentException("Transfer manager disposed.");
            Connect(peerEndpoints);
        }

        public void AddNewPeer(PeerState peer)
        {
            HandshakePeer(peer);
        }

        private void Connect(IEnumerable<IPEndPoint> peerEndpoints)
        {
            foreach(IPEndPoint peerEndpoint in peerEndpoints)
            {
                if(stop) break;
                PeerState peer = InitializePeer(peerEndpoint);
                NetworkIO.Connect(peer.Socket, peer.EndPoint, peer, PeerConnected);
            }
        }

        private PeerState InitializePeer(IPEndPoint peerEndpoint)
        {
            var peer = new PeerState(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), peerEndpoint)
                           {Bitfield = new BitArray(torrentData.Checksums.Count)};
            return peer;
        }

        private void PeerConnected(bool success, int transmitted, object state)
        {
            if(stop) return;
            var peer = (PeerState)state;
            if(success)
            {
                HandshakePeer(peer);
            }
        }

        private void HandshakePeer(PeerState peer)
        {
            SendMessage(peer.Socket, handshake, peer, HandshakeSent);
        }

        private void HandshakeReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState)state;
            var handshakeMessage = (HandshakeMessage)message;
            if(success)
            {
                peer.ReceivedHandshake = true;
                peer.ID = handshakeMessage.PeerID;

                if(peer.ID == Global.Instance.PeerId) return;

                if(!peer.SentHandshake)
                {
                    SendMessage(peer.Socket, handshake, peer, HandshakeSent);
                }
                else
                {
                    HandshakeCompleted(peer);
                }
            }
        }

        private void HandshakeSent(bool success, int sent, object state)
        {
            var peer = (PeerState)state;
            if(success)
            {
                peer.SentHandshake = true;
                if(peer.ID == Global.Instance.PeerId) return;

                if(!peer.ReceivedHandshake)
                {
                    MessageIO.ReceiveHandshake(peer.Socket, peer, HandshakeReceived);
                }
                else
                {
                    HandshakeCompleted(peer);
                }
            }
        }

        private void HandshakeCompleted(PeerState peer)
        {
            peer.Bitfield = new BitArray(torrentData.Checksums.Count);
            AddPeer(peer);
            MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
        }

        private void MessageReceived(bool success, PeerMessage message, object state)
        {
            if (stop) return;
            var peer = (PeerState)state;
            if(!success) Debug.WriteLine("Failed to receive message.");
            if(success)
            {
                if(message is BitfieldMessage)
                {
                    HandleBitfield(message, peer);
                    SelectPeer(peer);
                }
                else if(message is HaveMessage)
                {
                    HandleHave(message, peer);
                }
                else if(message is UnchokeMessage)
                {
                    peer.AmChoked = false;
                    RemovePeerWhoChokedMe(peer);
                    if(peer.AmInterested)
                    {
                        SendRequests(peer);
                    }

                }
                else if(message is InterestedMessage)
                {
                    peer.IsInterested = true;
                }
                else if(message is ChokeMessage)
                {
                    peer.AmChoked = true;
                    AddPeerWhoChokedMe(peer);
                }
                else if(message is NotInterestedMessage)
                {
                    peer.IsInterested = false;
                }
                else if(message is PieceMessage)
                {
                    var pieceMessage = message as PieceMessage;
                    HandlePiece(pieceMessage, peer);
                }
                MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
            }
            else if(!peer.Socket.Connected)
            {
                ClosePeerSocket(peer);
            }
            
        }

        private void RemovePeerWhoChokedMe(PeerState peer)
        {
            if (peersWhoChokedMe == 0) return;
            peersWhoChokedMe--;
            OnPeersWhoChokedMeChanged();
        }

        private void AddPeerWhoChokedMe(PeerState peer)
        {
            peersWhoChokedMe++;
            OnPeersWhoChokedMeChanged();
        }

        private void SelectPeer(PeerState peer)
        {
            SendMessageTo(peer, new InterestedMessage());
            peer.AmInterested = true;
        }

        private void HandlePiece(PieceMessage pieceMessage, PeerState peer)
        {
            peer.PiecesReceived++;
            peer.LastReceived = DateTime.Now;
            var info = new PieceInfo(pieceMessage.Index, pieceMessage.Offset, pieceMessage.Data.Length);
            if (strategist.Received(info))
            {
                obtainedPiece(new Piece(pieceMessage.Data, pieceMessage.Index, pieceMessage.Offset,
                                        pieceMessage.Data.Length));
            }
            peer.PendingPieces--;
            SendRequests(peer);
        }

        private void SendRequests(PeerState peer)
        {
            int count = queueLimit - peer.PendingPieces;
            for(int i = 0; i < count; i++)
            {
                if(stop) return;
                PieceInfo requestData = strategist.Next(peer.Bitfield);
                if (strategist.Complete())
                {
                    Stop(true);
                    ChangeState(TransferState.Finished);
                }
                if (requestData != PieceInfo.Empty)
                {
                    SendMessageTo(peer,
                                  new RequestMessage(requestData.Index, requestData.Offset, requestData.Length));
                    peer.PendingPieces++;
                }
            }
        }

        private void MessageSent(bool success, int sent, object state)
        {
            if(!success) 
            {
                Debug.WriteLine("Failed to send message.");
                ClosePeerSocket(state as PeerState);
            }
        }

        private void ClosePeerSocket(PeerState peer)
        {
            try
            {
                lock(peer)
                    if(peer.Socket != null)
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

        private void HandleHave(PeerMessage message, PeerState peer)
        {
            var have = (HaveMessage)message;
            peer.Bitfield.Set(have.PieceIndex, true);
        }

        private void HandleBitfield(PeerMessage message, PeerState peer)
        {
            peer.Bitfield = ((BitfieldMessage)message).Bitfield;
        }

        private void SendMessageTo(PeerState peer, PeerMessage message)
        {
            SendMessage(peer.Socket, message, peer, MessageSent);
        }

        private void SendMessage(Socket socket, PeerMessage message, PeerState state, MessageSentCallback callback)
        {
            MessageIO.SendMessage(socket, message, state, callback);
        }

        private void RemovePeer(string id)
        {
            PeerState peer;
            if(Peers.TryRemove(id, out peer))
                OnPeerListChanged();
        }

        private void AddPeer(PeerState peer)
        {
            Peers.TryAdd(peer.ID, peer);
            OnPeerListChanged();
        }

        private void ChangeState(TransferState state)
        {
            State = state;
            OnStateChanged(state);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    Stop(true);
                    if(Peers != null)
                        foreach(var peerState in Peers)
                        {
                            peerState.Value.Socket.Dispose();
                        }
                }
                disposed = true;
            }
        }

        #endregion

        private void OnPeerListChanged()
        {
            if (PeerListChanged != null)
                PeerListChanged(this, EventArgs.Empty);
        }

        public event EventHandler PeerListChanged;
        public event EventHandler Stopping;
        public event EventHandler PeersWhoChokedMeChanged;
        public event EventHandler<EventArgs<TransferState>> StateChanged;

        public void OnStateChanged(TransferState e)
        {
            EventHandler<EventArgs<TransferState>> handler = StateChanged;
            if(handler != null) handler(this, new EventArgs<TransferState>(e));
        }


        private void OnPeersWhoChokedMeChanged()
        {
            if (PeersWhoChokedMeChanged != null)
                PeersWhoChokedMeChanged(this, EventArgs.Empty);
        }

        public void OnStopping(bool success)
        {
            EventHandler handler = Stopping;
            if(handler != null) handler(this, new EventArgs<bool>(success));
        }
    }
}
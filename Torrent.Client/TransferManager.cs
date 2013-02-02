using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public delegate Piece RequestPieceDelegate(PieceInfo pieceInfo);

    public delegate void ObtainedPieceDelegate(Piece piece);

    public class TransferManager : IDisposable
    {
        private readonly ObtainedPieceDelegate obtainedPiece;
        private readonly PieceStrategist strategist;
        private readonly TorrentData torrentData;
        private bool disposed;
        private HandshakeMessage handshake;
        private RequestPieceDelegate requestPiece;
        private volatile bool stop;
        private PeerState[] tops;
        public TransferManager(TorrentData data, RequestPieceDelegate requestHandler,
                               ObtainedPieceDelegate obtainedHandler)
        {
            Peers = new ConcurrentDictionary<string, PeerState>();
            torrentData = data;
            strategist = new PieceStrategist(data);
            requestPiece = requestHandler;
            obtainedPiece = obtainedHandler;
            handshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], data.InfoHash, "BitTorrent protocol");
            tops = new PeerState[0];
        }

        public ConcurrentDictionary<string, PeerState> Peers { get; private set; }

        public void Start(IEnumerable<IPEndPoint> peerEndpoints)
        {
            if(disposed) throw new TorrentException("Transfer manager disposed.");
            stop = false;
            handshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], torrentData.InfoHash,
                                             "BitTorrent protocol");
            Connect(peerEndpoints);
            Timer calculateTopPeers = new Timer(CalculateTopPeers);
            calculateTopPeers.Change(5000, 500);
        }

        private void CalculateTopPeers(object state)
        {
            var top = Peers.Values.OrderByDescending(p => p.PiecesReceived).Take(4);
            tops = top.ToArray();
            foreach(var peerState in Peers)
            {
                peerState.Value.Top = false;
            }
            foreach(var peerState in top)
            {
                peerState.Top = true;
            }
            OnPeerListChanged();
        }

        public void Stop()
        {
            if(disposed) throw new TorrentException("Transfer manager disposed.");
            stop = true;
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
            var peer = new PeerState(new Socket(SocketType.Stream, ProtocolType.Tcp), peerEndpoint)
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
            Debug.WriteLine("Successful handshake.");
            AddPeer(peer);

            MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
        }

        private void MessageReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState)state;
            Debug.WriteLine("Message received " + message);
            if(success)
            {
                if(message is BitfieldMessage)
                {
                    HandleBitfield(message, peer);
                    peer.AmInterested = true;
                    SendMessage(peer.Socket, new InterestedMessage(), peer, MessageSent);
                }
                else if(message is HaveMessage)
                {
                    HandleHave(message, peer);
                }
                else if(message is UnchokeMessage)
                {
                    peer.AmChoked = false;
                }
                else if(message is InterestedMessage)
                {
                    peer.IsInterested = true;
                }
                else if(message is ChokeMessage)
                {
                    peer.AmChoked = true;
                }
                else if(message is NotInterestedMessage)
                {
                    peer.IsInterested = false;
                }
                else if(message is PieceMessage)
                {
                    var pieceMessage = message as PieceMessage;
                    var info = new PieceInfo(pieceMessage.Index, pieceMessage.Offset, pieceMessage.Data.Length);
                    if (strategist.Need(info))
                    {
                        peer.PiecesReceived++;
                        strategist.Received(info);
                        obtainedPiece(new Piece(pieceMessage.Data, pieceMessage.Index, pieceMessage.Offset,
                                                pieceMessage.Data.Length));
                    }
                }
                Thread.Sleep(50);
                
                if(!peer.AmChoked)
                {
                    int count = 1;
                    if (tops.Contains(peer)) count = 10;
                    if (!tops.Any()) count = 5;
                    for (int i = 0; i < count; i++)
                    {
                        PieceInfo requestData = strategist.Next();
                        if (requestData != PieceInfo.Empty && peer.Bitfield[requestData.Index])
                            SendMessageTo(peer,
                                          new RequestMessage(requestData.Index, requestData.Offset, requestData.Length));
                    }
                }
            }
            else if(!peer.Socket.Connected)
            {
                ClosePeerSocket(peer);
            }
            MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
        }

        private void MessageSent(bool success, int sent, object state)
        {
            Debug.WriteLine("I sent some message, success: " + success);
            //if(!success) RemovePeer((state as PeerState).ID);
        }

        private void ClosePeerSocket(PeerState peer)
        {
            lock(peer)
            if(peer.Socket != null && peer.Socket.Connected)
            {
                Debug.WriteLine("Closing socket with " + peer.Socket.RemoteEndPoint);
                peer.Socket.Shutdown(SocketShutdown.Both);
                peer.Socket.Close();
                RemovePeer(peer.ID);
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
                    Stop();
                    if(Peers != null)
                        foreach(var peerState in Peers)
                        {
                            peerState.Value.Socket.Close();
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
    }
}
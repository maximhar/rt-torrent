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
using System.Linq;
using System.Threading;
namespace Torrent.Client
{
    public abstract class TorrentMode
    {
        public ConcurrentDictionary<string, PeerState> Peers { get; protected set; }
        public BlockManager BlockManager { get; private set; }
        public BlockStrategist BlockStrategist { get; private set; }
        public TorrentData Metadata { get; private set; }
        public TransferMonitor Monitor { get; private set; }

        protected Timer KeepAliveTimer;
        protected HandshakeMessage DefaultHandshake;
        protected bool Stopping = false;

        protected TorrentMode(BlockManager manager, BlockStrategist strategist, TorrentData metadata, TransferMonitor monitor)
        {
            //инициализация на обект за следене на пренесените данни
            Monitor = monitor;
            //обект за управляване на записа на парчета върху файловата система
            BlockManager = manager;
            //обект за управление на заявките на парчета към пиърите
            BlockStrategist = strategist;
            //обект, съдържаш метаданните на торента
            Metadata = metadata;
            //съобщение за здрависване, което се използва от този TorrentMode
            DefaultHandshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], Metadata.InfoHash, "BitTorrent protocol");
            //конкурентен речник за съхранение на състоянието на активните пиъри
            Peers = new ConcurrentDictionary<string, PeerState>();
            //прикачане на събитието за изключения на BlockManager-а
            manager.RaisedException += (s, e) => HandleException(e.Value);
            //инициализация на теймера за изпращане на KeepAlive съобщения
            KeepAliveTimer = new Timer(SendKeepAlives);
        }



        public virtual void Start()
        {
            Stopping = false;
            KeepAliveTimer.Change(10000, 30000);
        }

        public virtual void Stop(bool closeStreams)
        {
            //ако вече спираме, не можем да спрем отново
            if (Stopping) return;
            Stopping = true;
            //изчистване на речника с пиъри
            Peers.Clear();
            //освобождаване на таймера за изпращане на KeepAlive 
            KeepAliveTimer.Dispose();
            //ако closeStreams е true, освобождаваме BlockManager,
            //което затваря отворените файлове
            if(closeStreams)
                BlockManager.Dispose();
        }

        public virtual void AddEndpoints(IEnumerable<IPEndPoint> endpoints)
        {
            foreach(var ep in endpoints)
            {
                if(!Peers.Values.Any(p=>p.EndPoint.Equals(ep)))
                    ConnectPeer(ep);
            }
        }

        protected virtual void SendKeepAlives(object state)
        {
            foreach (var peer in Peers.Values)
                SendMessage(peer, new KeepAliveMessage());
        }

        protected virtual void HandleMessage(PeerMessage message, PeerState peer)
        {   //проверка на типа съобщение и извикване на съответния обработващ метод
            if (message is HandshakeMessage) HandleHandshake((HandshakeMessage)message, peer);
            else if (message is ChokeMessage) HandleChoke((ChokeMessage)message, peer);
            else if (message is UnchokeMessage) HandleUnchoke((UnchokeMessage)message, peer);
            else if (message is InterestedMessage) HandleInterested((InterestedMessage)message, peer);
            else if (message is NotInterestedMessage) HandleNotInterested((NotInterestedMessage)message, peer);
            else if (message is BitfieldMessage) HandleBitfield((BitfieldMessage)message, peer);
            else if (message is HaveMessage) HandleHave((HaveMessage)message, peer);
            else if (message is PieceMessage) HandlePiece((PieceMessage)message, peer);
            else if (message is RequestMessage) HandleRequest((RequestMessage)message, peer);
            //приемане на следващото съобщение от пиъра
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
            var peer = (PeerState)state;
            if (Stopping) return;
            if (success) Monitor.Sent(sent);
            else
            {
                CloseSocket(peer);
                ConnectPeer(peer.EndPoint);
            }
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

        protected virtual void HandleException(Exception e)
        {
            OnRaisedException(e);
        }

        public event EventHandler<EventArgs<Exception>> RaisedException;


        private void OnRaisedException(Exception e)
        {
            if (Stopping) return;
            EventHandler<EventArgs<Exception>> handler = RaisedException;
            if(handler != null) handler(this, new EventArgs<Exception>(e));
        }
    }
}

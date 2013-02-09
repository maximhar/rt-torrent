using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Torrent.Client.Events;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public delegate void PeerConnectedCallback(PeerState peer);

    public static class PeerListener
    {
        private static readonly ConcurrentDictionary<InfoHash, PeerConnectedCallback> innerDictionary;
        private static readonly Socket listenSocket;

        static PeerListener()
        {
            listenSocket = Global.Instance.Listener;
            innerDictionary = new ConcurrentDictionary<InfoHash, PeerConnectedCallback>();
            BeginListening();
        }

        public static bool Register(InfoHash infoHash, PeerConnectedCallback callback)
        {
            return innerDictionary.TryAdd(infoHash, callback);
        }

        public static bool Deregister(InfoHash infoHash)
        {
            PeerConnectedCallback callback;
            return innerDictionary.TryRemove(infoHash, out callback);
        }

        private static void BeginListening()
        {
            try
            {
                listenSocket.BeginAccept(EndAccept, listenSocket);
            }
            catch (Exception e)
            {
                RaiseException(e);
            }
        }

        private static void EndAccept(IAsyncResult ar)
        {
            try
            {
                var socket = (Socket)ar.AsyncState;
                Socket newsocket = socket.EndAccept(ar);
                var peer = new PeerState(newsocket, (IPEndPoint)newsocket.RemoteEndPoint);
                MessageIO.ReceiveHandshake(newsocket, peer, HandshakeReceived);
                BeginListening();
            }
            catch (Exception e)
            {
                RaiseException(e);
            }
        }

        private static void HandshakeReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState) state;
            var handshake = (HandshakeMessage) message;
            if (success)
            {
                peer.ReceivedHandshake = true;
                peer.ID = handshake.PeerID;

                if (peer.ID == Global.Instance.PeerId) return;
                PeerConnectedCallback callback;
                if (!innerDictionary.TryGetValue(handshake.InfoHash, out callback)) ClosePeerSocket(peer);
                else callback(peer);
            }
            else ClosePeerSocket(peer);
        }

        private static void ClosePeerSocket(PeerState peer)
        {
            if (peer != null && peer.Socket != null && peer.Socket.Connected)
            {
                peer.Socket.Shutdown(SocketShutdown.Both);
                peer.Socket.Close();
            }
        }

        private static void RaiseException(Exception e)
        {
            if (RaisedException != null)
            {
                RaisedException(null, new EventArgs<Exception>(e));
            }
        }

        public static event EventHandler<EventArgs<Exception>> RaisedException;
    }
}
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Torrent.Client.Events;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public delegate void PeerConnectedCallback(PeerState peer);

    /// <summary>
    /// Static class used for listening for incoming peer connections and redirecting them to the corresponding delegate.
    /// </summary>
    public static class PeerListener
    {
        private static readonly ConcurrentDictionary<InfoHash, PeerConnectedCallback> InnerDictionary;
        private static readonly Socket ListenSocket;

        static PeerListener()
        {
            ListenSocket = Global.Instance.Listener;
            InnerDictionary = new ConcurrentDictionary<InfoHash, PeerConnectedCallback>();
            BeginListening();
        }

        public static bool Register(InfoHash infoHash, PeerConnectedCallback callback)
        {
            return InnerDictionary.TryAdd(infoHash, callback);
        }

        public static bool Deregister(InfoHash infoHash)
        {
            PeerConnectedCallback callback;
            return InnerDictionary.TryRemove(infoHash, out callback);
        }

        private static void BeginListening()
        {
            try
            {
                ListenSocket.BeginAccept(EndAccept, ListenSocket);
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
                if (!InnerDictionary.TryGetValue(handshake.InfoHash, out callback)) ClosePeerSocket(peer);
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
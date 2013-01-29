using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    public delegate void PeerConnectedCallback(PeerState peer);
    static class PeerListener
    {
        private static ConcurrentDictionary<InfoHash, PeerConnectedCallback> innerDictionary;
        private static Socket listenSocket;

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
                Debug.WriteLine("PeerListener is listening!");
                listenSocket.BeginAccept(EndAccept, listenSocket);
            }
            catch(Exception e)
            {
                RaiseException(e);
            }
        }

        private static void EndAccept(IAsyncResult ar)
        {
            var socket = (Socket)ar.AsyncState;//this is very werid :D
            var newsocket = socket.EndAccept(ar);
            var peer = new PeerState(newsocket, (IPEndPoint)newsocket.RemoteEndPoint);
            MessageIO.ReceiveHandshake(newsocket, peer, HandshakeReceived);
            BeginListening();
        }

        private static void HandshakeReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState)state;
            var handshake = (HandshakeMessage)message;
            if (success)
            {
                peer.ReceivedHandshake = true;
                peer.ID = handshake.PeerID;

                if (peer.ID == Global.Instance.PeerId) return;
                PeerConnectedCallback callback;
                if(!innerDictionary.TryGetValue(handshake.InfoHash,out callback)) ClosePeerSocket(peer);
                else callback(peer);
            }
            else ClosePeerSocket(peer);
        }

        private static void ClosePeerSocket(PeerState peer)
        {
            if (peer != null && peer.Socket != null && peer.Socket.Connected)
            {
                Debug.WriteLine("Closing socket with (PEERLISTENER)" + peer.Socket.RemoteEndPoint);
                peer.Socket.Shutdown(SocketShutdown.Both);
                peer.Socket.Close();
            }
        }

        private static void RaiseException(Exception e)
        {
            if (RaisedException != null)
            {
                RaisedException(null, e);
            }
        }

        public static event EventHandler<Exception> RaisedException;
    }
}

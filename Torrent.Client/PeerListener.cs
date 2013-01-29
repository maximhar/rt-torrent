using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            BeginListening();
        }

        public static bool Register(InfoHash infoHash, PeerConnectedCallback callback)
        {
            throw new NotImplementedException();
        }
        public static bool Deregister(InfoHash infoHash)
        {
            throw new NotImplementedException();
        }

        private static void BeginListening()
        {
            throw new NotImplementedException();
        }

        
    }
}

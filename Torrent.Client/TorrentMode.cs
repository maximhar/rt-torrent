using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public abstract class TorrentMode : IMode
    {
        public ConcurrentDictionary<string, PeerState> Peers
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }
    
        public void HandleMessage()
        {
            throw new NotImplementedException();
        }

        public void AddEndpoints()
        {
            throw new System.NotImplementedException();
        }
    }
}

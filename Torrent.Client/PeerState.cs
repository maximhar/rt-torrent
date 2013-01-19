using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    public class PeerState
    {
        public bool AmChoked {get; set;}
        public bool AmInterested {get; set;}
        public bool IsChoked {get; set;}
        public bool IsInterested { get; set; }

        public PeerState(bool amChocked, bool amInterested, bool isChocked, bool isInterested)
        {
            this.AmChoked = amChocked;
            this.AmInterested = amInterested;
            this.IsChoked = isChocked;
            this.IsInterested = isInterested;
        }
    }
}

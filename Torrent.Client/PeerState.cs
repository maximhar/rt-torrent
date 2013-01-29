using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        public Socket Socket { get; set; }
        public bool ReceivedHandshake { get; set; }
        public bool SentHandshake { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public string ID { get; set; }
        public BitArray Bitfield { get; set; }
        public PeerState(Socket socket, IPEndPoint endpoint)
        {
            this.Socket = socket;
            this.EndPoint = endpoint;
            this.AmChoked = true;
            this.IsChoked = true;
        }

        public override string ToString()
        {
            return string.Format("{0}, sent: {1}, received: {2}", EndPoint, SentHandshake, ReceivedHandshake);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client.Bencoding;

namespace Torrent.Client
{
    class PeerEndpoint
    {
        public string PeerID { get; private set; }
        public System.Net.IPAddress IP { get; private set; }
        public short Port { get; private set; }

        public PeerEndpoint(BencodedDictionary peer)
        {
            if (!peer.ContainsKey("peer id"))
            {
                throw new TorrentException("Peer does not have an ID.");
            }

            PeerID = (BencodedString)peer["peer id"];

            if (!peer.ContainsKey("ip"))
            {
                throw new TorrentException("Peer does not have an IP.");
            }

            IP = new System.Net.IPAddress(System.Text.Encoding.ASCII.GetBytes((BencodedString)peer["ip"]));

            if (!peer.ContainsKey("port"))
            {
                throw new TorrentException("Peer does not have a port.");
            }

            Port = (short)(BencodedInteger)peer["port"];
        }

        public PeerEndpoint(Byte[] peer)
        {
            IP = new System.Net.IPAddress(peer.Take(4).ToArray());
            Port = BitConverter.ToInt16(peer, 4); // Converts byte[] to short
        }
    }
}

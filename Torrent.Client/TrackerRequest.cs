using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public class TrackerRequest
    {
        public byte[] InfoHash { get; private set; }
        public byte[] PeerId { get; private set; }
        public short Port { get; private set; }
        public long Uploaded { get; private set; }
        public long Downloaded { get; private set; }
        public long Left { get; private set; }
        public bool Compact { get; private set; }
        public bool OmitPeerIds { get; private set; }
        public EventType Event { get; private set; }

        public TrackerRequest(byte[] infoHash, byte[] peerId, short port, long uploaded, long downloaded,
            long left, bool compact, bool omitPeerIds, EventType @event = EventType.None)
        {
            Contract.Requires(infoHash != null);
            Contract.Requires(peerId != null);
            Contract.Requires(port > 0);
            Contract.Requires(uploaded >= 0);
            Contract.Requires(downloaded >= 0);
            Contract.Requires(left >= 0);
            Contract.Requires(@event != null);
             
            this.InfoHash = infoHash;
            this.PeerId = peerId;
            this.Port = port;
            this.Uploaded = uploaded;
            this.Downloaded = downloaded;
            this.Left = left;
            this.Compact = compact;
            this.OmitPeerIds = omitPeerIds;
            this.Event = @event;
        }
    }
}

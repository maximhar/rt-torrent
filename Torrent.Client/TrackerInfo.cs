using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Torrent.Client.Events;

namespace Torrent.Client
{
    public class TrackerInfo
    {
        private readonly TrackerClient client;

        public DateTime LastAnnounced { get; private set; }
        public string Url { get; private set; }
        public AnnounceState LastState { get; private set; }
        public TimeSpan Period { get; private set; }

        public TrackerInfo(string url)
        {
            Url = url;
            LastAnnounced = DateTime.MinValue;
            LastState = AnnounceState.None;
            client = new TrackerClient(new[]{Url});
        }

        public void Started(InfoHash hash, long downloaded, long uploaded, long remaining)
        {
            Announce(hash, downloaded, uploaded, remaining, EventType.Started);
        }

        public void Stopped(InfoHash hash, long downloaded, long uploaded, long remaining)
        {
            Announce(hash, downloaded, uploaded, remaining, EventType.Stopped);
        }

        public void Completed(InfoHash hash, long downloaded, long uploaded, long remaining)
        {
            Announce(hash, downloaded, uploaded, remaining, EventType.Completed);
        }

        public void Regular(InfoHash hash, long downloaded, long uploaded, long remaining)
        {
            Announce(hash, downloaded, uploaded, remaining, EventType.None);
        }

        private void Announce(InfoHash hash, long downloaded, long uploaded, long remaining, EventType type)
        {
            var request = new TrackerRequest(hash, Global.Instance.PeerId, Global.Instance.ListeningPort,
                                             uploaded, downloaded, remaining, true, false, type);
            SendRequest(request);
        }

        private void SendRequest(TrackerRequest request)
        {
            var response = client.GetResponse(request);
            LastAnnounced = DateTime.Now;
            if(response == null || response.FailureReason != null)
            {
                LastState = AnnounceState.Success;
                Period = TimeSpan.FromSeconds(20);
            }
            else
            {
                LastState = AnnounceState.Success;
                Period = TimeSpan.FromSeconds(response.Interval);
                OnPeersReceived(response.Endpoints);
            }
        }

        public event EventHandler<EventArgs<IEnumerable<IPEndPoint>>> PeersReceived;

        public void OnPeersReceived(IEnumerable<IPEndPoint> e)
        {
            EventHandler<EventArgs<IEnumerable<IPEndPoint>>> handler = PeersReceived;
            if(handler != null) handler(this, new EventArgs<IEnumerable<IPEndPoint>>(e));
        }
    }
}
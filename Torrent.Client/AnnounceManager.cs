using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torrent.Client.Events;

namespace Torrent.Client
{
    public class AnnounceManager:IDisposable
    {
        private bool disposed;
        private List<TrackerInfo> trackers;
        private Timer regularTimer;

        public IEnumerable<TrackerInfo> Trackers { get { return trackers.AsEnumerable(); } }
        public TransferMonitor Monitor { get; private set; }
        public TorrentData Data { get; private set; }

        public AnnounceManager(IEnumerable<string> announceUrls, TransferMonitor monitor, TorrentData data)
        {
            trackers = new List<TrackerInfo>();
            trackers.AddRange(announceUrls.Where(u=>u.StartsWith("http")).Select(u=>new TrackerInfo(u)));
            trackers.ForEach(t => t.PeersReceived += (sender, args) => OnPeersReceived(args.Value));

            Monitor = monitor;
            Data = data;
            regularTimer = new Timer(Regular);
            regularTimer.Change(1000, 5000);
        }

        public void Started()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Started(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        public void Completed()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Completed(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        public void Stopped()
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    tracker.Stopped(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        private void Regular(object o)
        {
            Task.Factory.StartNew(() =>
            {
                foreach (var tracker in Trackers)
                {
                    if(tracker.LastAnnounced.Add(tracker.Period) < DateTime.Now && tracker.LastState != AnnounceState.None)
                        tracker.Regular(Data.InfoHash, Monitor.BytesReceived,
                                    Monitor.BytesSent, Monitor.TotalBytes - Monitor.BytesReceived);
                }
            });
        }

        public event EventHandler<EventArgs<IEnumerable<IPEndPoint>>> PeersReceived;

        public void OnPeersReceived(IEnumerable<IPEndPoint> e)
        {
            if (disposed) return;
            EventHandler<EventArgs<IEnumerable<IPEndPoint>>> handler = PeersReceived;
            if(handler != null) handler(this, new EventArgs<IEnumerable<IPEndPoint>>(e));
        }

        public void Dispose()
        {
            if(!disposed)
            {
                regularTimer.Dispose();
                disposed = true;
            }
        }
    }
}

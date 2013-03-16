using System;
using System.Collections.Generic;
using System.Linq;
namespace Torrent.Client.Events
{
    public class StatsEventArgs:EventArgs
    {
        public long DownloadedBytes { get; private set; }
        public int TotalPeers { get; private set; }
        public int ChokedBy { get; private set; }
        public int QueuedRequests { get; private set; }
        public StatsEventArgs(long downloadedBytes, int totalPeers, int chokedBy, int queued)
        {
            this.DownloadedBytes = downloadedBytes;
            this.TotalPeers = totalPeers;
            this.ChokedBy = chokedBy;
            this.QueuedRequests = queued;
            IEnumerable<string> list = new List<string> { "cat", "dog", "frog" };
            foreach (var animal in list.Reverse()) ;
        }
    }
}
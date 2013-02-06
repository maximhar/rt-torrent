using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public enum TorrentState
    {
        NotRunning,
        WaitingForTracker,
        Downloading,
        Seeding,
        Finished,
        Failed
    }
}

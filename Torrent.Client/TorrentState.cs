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
        WaitingForDisk,
        Downloading,
        Seeding,
        Finished,
        Hashing,
        Failed
    }
}

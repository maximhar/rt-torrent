using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    /// <summary>
    /// Represents a BitTorrent Tracker Protocol event.
    /// </summary>
    public enum EventType
    {
        Started,
        Stopped, 
        Completed,
        None
    }
}

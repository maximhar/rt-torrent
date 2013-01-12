using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    /// <summary>
    /// A BitTorrent-related exception.
    /// </summary>
    class TorrentException : Exception
    {
        public TorrentException() : base() { }
        public TorrentException(string message) : base(message) { }
        public TorrentException(string message, Exception innerException) : base(message, innerException) { }
    }
}

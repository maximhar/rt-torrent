using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    class TorrentException : Exception
    {
        public TorrentException() : base() { }
        public TorrentException(string message) : base(message) { }
        public TorrentException(string message, Exception innerException) : base(message, innerException) { }
    }
}

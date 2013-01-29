using System;

namespace Torrent.Client
{
    /// <summary>
    /// A BitTorrent-related exception.
    /// </summary>
    internal class TorrentException : Exception
    {
        public TorrentException()
        {
        }

        public TorrentException(string message) : base(message)
        {
        }

        public TorrentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
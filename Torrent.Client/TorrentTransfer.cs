using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    /// <summary>
    /// Represents a BitTorrent data transfer.
    /// </summary>
    public class TorrentTransfer
    {
        /// <summary>
        /// The metadata decribing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        /// <summary>
        /// Initialize a torrent transfer with metadata from a file on the filesystem.
        /// </summary>
        /// <param name="torrentPath">Path to the torrent file.</param>
        public TorrentTransfer(string torrentPath):this(File.OpenRead(torrentPath))
        {
            Contract.Requires(torrentPath != null);
        }

        /// <summary>
        /// Initialize a torrent transfer with metadata read from the specified stream.
        /// </summary>
        /// <param name="torrentStream">The stream to read the torrent metadata from.</param>
        public TorrentTransfer(Stream torrentStream)
        {
            Contract.Requires(torrentStream != null);
            using (torrentStream)
            using (var reader = new BinaryReader(torrentStream))
            {
                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                this.Data = new TorrentData(bytes);
            }

        }
    }
}

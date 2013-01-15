using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Torrent.Client
{
    /// <summary>
    /// Represents a BitTorrent data transfer.
    /// </summary>
    public class TorrentTransfer
    {
        object locker = new object();
        private Thread torrentThread;

        /// <summary>
        /// The metadata decribing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        public List<PeerEndpoint> PeerEndpoints { get; private set; }

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

            PeerEndpoints = new List<PeerEndpoint>();

            using (torrentStream)
            using (var reader = new BinaryReader(torrentStream))
            {
                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                this.Data = new TorrentData(bytes);
            }
        }
        /// <summary>
        /// Starts the torrent transfer on a new thread.
        /// </summary>
        public void Start()
        {
            torrentThread = new Thread(StartThread);
            torrentThread.Start();
        }
        private void StartThread()
        {
            try
            {
                var announces = new List<string>();
                announces.Add(Data.AnnounceURL);
                if (Data.AnnounceList != null)
                    announces.AddRange(Data.AnnounceList);

                var request = new TrackerRequest(this.Data.InfoHash,
                            Encoding.ASCII.GetBytes("-UT3230-761290182730"), 8910, 0, 0, (long)this.Data.Files.Sum(f => f.Length),
                            false, false, numWant: 200, @event: EventType.Started);
                bool successfullyConnected = false;
                string failureReason = null;
                foreach (var url in announces)
                {
                    try
                    {
                        var client = new TrackerClient(url);
                        var response = client.GetResponse(request);
                        if ((failureReason = response.FailureReason) != null) continue;
                        PeerEndpoints.AddRange(response.PeerEndpoints);
                        successfullyConnected = true;
                        break;
                    }
                    catch { continue; }
                }
                if (!successfullyConnected) throw new TorrentException(string.Format("Unable to connect to tracker. {0}", failureReason ?? string.Empty));

                OnGotPeers();
            }
            catch (Exception e)
            {
                OnRaisedException(e);
                return;
            }
        }

        private void OnGotPeers()
        {
            if (GotPeers != null)
            {
                GotPeers(this, EventArgs.Empty);
            }
        }

        private void OnRaisedException(Exception e)
        {
            if (RaisedException != null)
            {
                RaisedException(this, e);
            }
        }
        /// <summary>
        /// Fires when the torrent receives peers from the tracker.
        /// </summary>
        public event EventHandler GotPeers;
        /// <summary>
        /// Fires when an exception occurs in the transfer thread.
        /// </summary>
        public event EventHandler<Exception> RaisedException;
    }
}

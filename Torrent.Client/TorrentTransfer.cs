using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using Torrent.Client.Events;

namespace Torrent.Client
{
    /// <summary>
    /// Represents a BitTorrent data transfer.
    /// </summary>
    public class TorrentTransfer
    {
        private bool stopping;
        private readonly TrackerClient tracker;
        private volatile bool stop;
        private Timer statsReportTimer;
        private TrackerResponse trackerData;

        public TorrentMode Mode { get; private set; }

        /// <summary>
        /// Initialize a torrent transfer with metadata from a file on the filesystem.
        /// </summary>
        /// <param name="torrentPath">Path to the torrent file.</param>
        public TorrentTransfer(string torrentPath) : this(File.OpenRead(torrentPath))
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
                byte[] bytes = reader.ReadBytes((int) reader.BaseStream.Length);
                Data = new TorrentData(bytes);
            }

            tracker = new TrackerClient(Data.Announces);
            statsReportTimer = new Timer(o => OnStatsReport());
        }

        /// <summary>
        /// The metadata describing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        public bool Running { get; private set; }

        public TorrentState State { get; private set; }

        /// <summary>
        /// Starts the torrent transfer on a new thread.
        /// </summary>
        public void Start()
        {
            if (State != TorrentState.NotRunning) throw new TorrentException("Already started.");

            var torrentThread = new Thread(StartThread) {IsBackground = true};
            torrentThread.Start();
        }

        /// <summary>
        /// Stops all torrent activity and shuts down the thread.
        /// </summary>
        public void Stop()
        {
            stopping = true;
        }

        private void StartThread()
        {
            StartActions();
            try
            {
                StartTransfer();
                WaitForStop();
            }
            catch (Exception e)
            {
                OnRaisedException(e);
            }
            StopActions();
        }

        private void StartTransfer()
        {
            
            ChangeState(TorrentState.Hashing);
            var hashingMode = new HashingMode(new BlockManager(Data, Data.Name),
                                              new BlockStrategist(Data), Data, new TransferMonitor(Data.InfoHash));
            hashingMode.RaisedException += (s, e) => OnRaisedException(e.Value);
            hashingMode.HashingComplete += (sender, args) => HashingComplete();
            Mode = hashingMode;
            statsReportTimer.Change(0, 250);
            Mode.Start();
        }

        private void HashingComplete()
        {

            var mode = new DownloadMode((HashingMode)Mode);
            mode.RaisedException += (s, e) => OnRaisedException(e.Value);
            mode.FlushedToDisk += (s, e) => Stop();
            mode.DownloadComplete += (s, e) => ChangeState(TorrentState.WaitingForDisk);
            mode.Start();

            Mode = mode;
            ChangeState(TorrentState.Downloading);
            trackerData = tracker.AnnounceStart(Data.InfoHash, Global.Instance.PeerId,
                                                Global.Instance.ListeningPort,
                                                0, 0, Data.Files.Sum(f => f.Length));
            mode.AddEndpoints(trackerData.Endpoints);
        }

        private void StartActions()
        {
            stop = false;
            Running = true;
        }

        private void StopActions()
        {
            OnStatsReport();
            ChangeState(TorrentState.NotRunning);
            statsReportTimer.Dispose();
            if(Mode!=null) 
                Mode.Stop(true);
            stop = true;
            Running = false;
        }

        private void WaitForStop()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (stopping) break;
            }
        }

        private void ChangeState(TorrentState state)
        {
            if (State != state)
            {
                State = state;
                OnStateChanged(state);
            }
        }


        #region Events

        private void OnRaisedException(Exception e)
        {
            if(e.InnerException is IOException)
                Stop();
            if (RaisedException != null)
            {
                RaisedException(this, new EventArgs<Exception>(e));
            }
        }

        /// <summary>
        /// Fires when an exception occurs in the transfer thread.
        /// </summary>
        public event EventHandler<EventArgs<Exception>> RaisedException;

        public event EventHandler<StatsEventArgs> ReportStats;

        public void OnStatsReport()
        {
            if (Mode == null) return;

            var downloaded = Mode.BlockStrategist.Available*Global.Instance.BlockSize;
            var totalPeers = Mode.Peers.Count;
            var chokedBy = Mode.Peers.Count(p => p.Value.AmChoked);

            var stats = new StatsEventArgs(downloaded, totalPeers, chokedBy, 0);
            EventHandler<StatsEventArgs> handler = ReportStats;
            if (handler != null) handler(this, stats);
        }

        public event EventHandler<EventArgs<TorrentState>> StateChanged;

        public void OnStateChanged(TorrentState e)
        {
            EventHandler<EventArgs<TorrentState>> handler = StateChanged;
            if(handler != null) handler(this, new EventArgs<TorrentState>(e));
        }

        #endregion
    }
}
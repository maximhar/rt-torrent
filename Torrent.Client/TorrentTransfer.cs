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
        private TransferMonitor monitor;

        public AnnounceManager AnnounceManager { get; private set; }

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
            monitor = new TransferMonitor(Data.InfoHash, Data.TotalLength);
            AnnounceManager = new AnnounceManager(Data.Announces, monitor, Data);
            AnnounceManager.PeersReceived += (sender, args) => { if(Mode != null) 
                Mode.AddEndpoints(args.Value); };
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
            AnnounceManager.Stopped();
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
            EnterHashingMode();
        }

        private void EnterHashingMode()
        {
            ChangeState(TorrentState.Hashing);
            var hashingMode = new HashingMode(new BlockManager(Data, Path.Combine(Global.Instance.DownloadFolder, Data.Name)),
                                              new BlockStrategist(Data), Data, monitor);
            hashingMode.RaisedException += (s, e) => OnRaisedException(e.Value);
            hashingMode.HashingComplete += (sender, args) => HashingComplete();
            Mode = hashingMode;
            statsReportTimer.Change(0, 250);
            Mode.Start();
        }

        private void HashingComplete()
        {
            if(!Mode.BlockStrategist.Complete)
            {
                EnterDownloadMode();
            }
            else
            {
                EnterSeedMode();
            }
        }

        private void EnterSeedMode()
        {
            ChangeState(TorrentState.Seeding);
            var mode = new SeedMode(Mode);
            mode.RaisedException += (s, e) => OnRaisedException(e.Value);
            mode.Start();
            Mode = mode;
            AnnounceManager.Started();
        }

        private void EnterDownloadMode()
        {
            ChangeState(TorrentState.Downloading);
            var mode = new DownloadMode((HashingMode)Mode);
            mode.RaisedException += (s, e) => OnRaisedException(e.Value);
            mode.FlushedToDisk += (s, e) => FlushedToDisk();
            mode.DownloadComplete += (s, e) => DownloadCompleted();
            mode.Start();
            Mode = mode;
            AnnounceManager.Started();
        }

        private void FlushedToDisk()
        {
            EnterHashingMode();
        }

        private void DownloadCompleted()
        {
            ChangeState(TorrentState.WaitingForDisk);
            AnnounceManager.Completed();
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
            AnnounceManager.Stopped();
            AnnounceManager.Dispose();
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

            var downloaded = (long)Mode.BlockStrategist.Available*Global.Instance.BlockSize;
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
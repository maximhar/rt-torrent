using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    /// <summary>
    /// Represents a BitTorrent data transfer.
    /// </summary>
    public class TorrentTransfer
    {
        private const int PSTR_LENGTH = 19;
        private readonly HandshakeMessage localHandshake;
        private readonly TrackerClient tracker;
        private List<IPEndPoint> Endpoints;
        private volatile bool stop;
        private PieceManager pieceManager;
        private TransferManager transfer;
        private long downloaded = 0;
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

            Endpoints = new List<IPEndPoint>();

            using (torrentStream)
            using (var reader = new BinaryReader(torrentStream))
            {
                byte[] bytes = reader.ReadBytes((int) reader.BaseStream.Length);
                Data = new TorrentData(bytes);
            }

            tracker = new TrackerClient(Data.Announces);
            Peers = new ConcurrentDictionary<string, PeerState>();
            localHandshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], Data.InfoHash,
                                                  "BitTorrent protocol");
            pieceManager = new PieceManager(Data);
            transfer = new TransferManager(Data, PieceRequested, PieceDownloaded);
        }

        /// <summary>
        /// The metadata decribing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        public ConcurrentDictionary<string, PeerState> Peers { get; private set; }

        public bool Running { get; private set; }

        /// <summary>
        /// Starts the torrent transfer on a new thread.
        /// </summary>
        public void Start()
        {
            if (Running) throw new TorrentException("Already started.");

            var torrentThread = new Thread(StartThread);
            torrentThread.IsBackground = true;
            torrentThread.Start();
        }

        /// <summary>
        /// Stops all torrent activity and shuts down the thread.
        /// </summary>
        public void Stop()
        {
            stop = true;
        }

        private void StartThread()
        {
            StartActions();
            try
            {
                RegisterForListen();
                HandshakeTracker();
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
            transfer.PeerListChanged += transfer_PeerListChanged;
            transfer.Start(Endpoints);
        }

        void transfer_PeerListChanged(object sender, EventArgs e)
        {
            OnPeersChanged(transfer.tops.ToArray());
        }

        private void StartActions()
        {
            stop = false;
            Running = true;
        }

        private void RegisterForListen()
        {
            PeerListener.RaisedException += PeerListener_RaisedException;
            PeerListener.Register(Data.InfoHash, ReceivedPeer);
        }

        private void PieceDownloaded(Piece piece)
        {
            pieceManager.AddPiece(piece, PieceWritten, piece);
        }

        private void PieceWritten(bool success, object state)
        {
            var piece = state as Piece;
            Interlocked.Add(ref downloaded, piece.Data.Length);
            OnWrotePiece(piece);
            OnDownloadedBytes(downloaded);
        }

        private Piece PieceRequested(PieceInfo pieceinfo)
        {
            throw new NotImplementedException();
        }

        private void PeerListener_RaisedException(object sender, Exception e)
        {
            OnRaisedException(e);
            Stop();
        }

        private void ReceivedPeer(PeerState peer)
        {
            transfer.AddNewPeer(peer);
        }

        private void HandshakeTracker()
        {
            TrackerResponse info = tracker.AnnounceStart(Data.InfoHash, Global.Instance.PeerId,
                                                         Global.Instance.ListeningPort,
                                                         0, 0, Data.Files.Sum(f => f.Length));
            Endpoints = info.Endpoints;
        }

        private void StopActions()
        {
            OnStopping();
            DeregisterFromListen();
            Running = false;
        }

        private void DeregisterFromListen()
        {
            PeerListener.Deregister(Data.InfoHash);
            PeerListener.RaisedException -= PeerListener_RaisedException;
        }

        private void WaitForStop()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (stop) break;
            }
        }

        #region Events

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

        private void OnStopping()
        {
            if (Stopping != null)
            {
                Stopping(this, EventArgs.Empty);
            }
        }

        private void OnSentMessage(PeerMessage msg)
        {
            if (SentMessage != null)
            {
                SentMessage(this, msg);
            }
        }

        private void OnReceivedMessage(PeerMessage msg)
        {
            if (ReceivedMessage != null)
            {
                ReceivedMessage(this, msg);
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

        /// <summary>
        /// Fires just prior to the transfer's complete stop.
        /// </summary>
        public event EventHandler Stopping;

        public event EventHandler<PeerMessage> ReceivedMessage;

        public event EventHandler<PeerMessage> SentMessage;

        public event EventHandler<Piece> WrotePiece;

        public void OnWrotePiece(Piece e)
        {
            EventHandler<Piece> handler = WrotePiece;
            if(handler != null) handler(this, e);
        }

        public event EventHandler<IEnumerable<PeerState>> PeersChanged;

        public void OnPeersChanged(IEnumerable<PeerState> e)
        {
            EventHandler<IEnumerable<PeerState>> handler = PeersChanged;
            if(handler != null) handler(this, e);
        }

        public event EventHandler<long> DownloadedBytes;

        public void OnDownloadedBytes(long e)
        {
            EventHandler<long> handler = DownloadedBytes;
            if(handler != null) handler(this, e);
        }

        #endregion
    }
}
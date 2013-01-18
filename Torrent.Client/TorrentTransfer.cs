using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Torrent.Client
{
    public enum MessageType
    {
        Handshake,
        Unknown
    }

    /// <summary>
    /// Represents a BitTorrent data transfer.
    /// </summary>
    public class TorrentTransfer
    {
        private const int PSTR_LENGTH = 19;
        private volatile bool stop = false;

        /// <summary>
        /// The metadata decribing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        public List<PeerEndpoint> PeerEndpoints { get; private set; }

        public bool Running { get; private set; }

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
            if (Running) throw new TorrentException("Already started.");

            stop = false;
            Running = true;

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
            try
            {
                HandshakeTracker();
                Task.Factory.StartNew(SendHandshakes);
                var listen = Task.Factory.StartNew(Listen);

                while (true)
                {
                    Thread.Sleep(200);
                    if (stop) break;
                }
            }
            catch (Exception e)
            {
                OnRaisedException(e);
            }

            OnStopping();
            Running = false;
        }

        private void Listen()
        {
            var listener = new TcpListener(IPAddress.Any, LocalInfo.Instance.ListeningPort);
            listener.Start();
            while (true)
            {
                if (stop) return;
                var client = listener.AcceptTcpClient();
                var endpoint = client.Client.RemoteEndPoint;
                var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int count;
                using (var bstr = new MemoryStream())
                {
                    while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        bstr.Write(buffer, 0, count);
                    }
                    client.Close();
                    if (stop) return;
                    ProcessMessage(bstr.ToArray(), endpoint);
                }
            }
        }

        private void ProcessMessage(byte[] msg, EndPoint peer)
        {
            MessageType type = GetMessageType(msg);
            switch (type)
            {
                case MessageType.Handshake:
                    OnReceivedHandshake(peer);
                    break;
                default:
                    OnGotTcpMessage(Encoding.UTF8.GetString(msg));
                    break;
            }
        }

        private MessageType GetMessageType(byte[] msg)
        {
            if (msg.Length>PSTR_LENGTH && msg[0] == PSTR_LENGTH)
                return MessageType.Handshake;

            return MessageType.Unknown;
        }
        private void SendHandshakes()
        {
            Parallel.ForEach(PeerEndpoints, (peer) =>
            {
                try
                {
                    var client = new TcpClient();
                    client.Connect(new IPEndPoint(peer.IP, peer.Port));
                    var stream = client.GetStream();

                    var pstr = "BitTorrent protocol";
                    var pstrlen = pstr.Length;
                    var reserved = new byte[8];
                    var info_hash = Data.InfoHash;
                    var peer_id = LocalInfo.Instance.PeerId;

                    var msg = new List<byte>();
                    msg.AddRange(BitConverter.GetBytes(pstrlen));
                    msg.AddRange(Encoding.UTF8.GetBytes(pstr));
                    msg.AddRange(reserved);
                    msg.AddRange(info_hash);
                    msg.AddRange(peer_id);

                    stream.Write(msg.ToArray(), 0, msg.Count);
                    client.Close();
                    OnSentHandshake(peer);
                }
                catch { }
            });
        }
        private void HandshakeTracker()
        {
            var announces = new List<string>();
            announces.Add(Data.AnnounceURL);
            if (Data.AnnounceList != null)
                announces.AddRange(Data.AnnounceList);

            var request = new TrackerRequest(this.Data.InfoHash,
                        LocalInfo.Instance.PeerId, LocalInfo.Instance.ListeningPort, 0, 0, (long)this.Data.Files.Sum(f => f.Length),
                        false, false, numWant: 200, @event: EventType.Started);
            bool successfullyConnected = false;
            string failureReason = null;
            foreach (var url in announces)
            {
                try
                {
                    if (stop) return;

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

        private void OnGotTcpMessage(string msg)
        {
            if (GotTcpMessage != null)
            {
                GotTcpMessage(this, msg);
            }
        }

        private void OnSentHandshake(PeerEndpoint peer)
        {
            if (SentHandshake != null)
            {
                SentHandshake(this, peer);
            }
        }

        private void OnReceivedHandshake(EndPoint peer)
        {
            if (ReceivedHandshake != null)
            {
                ReceivedHandshake(this, peer);
            }
        }

        private void OnStopping()
        {
            if (Stopping != null)
            {
                Stopping(this, EventArgs.Empty);
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

        public event EventHandler<string> GotTcpMessage;

        public event EventHandler<PeerEndpoint> SentHandshake;

        public event EventHandler<EndPoint> ReceivedHandshake;
    }
}

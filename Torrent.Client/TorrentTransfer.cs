using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                //var send = Task.Factory.StartNew(Send);
                //var listen = Task.Factory.StartNew(Listen);
                HandshakeTracker();
                foreach (var peer in PeerEndpoints)
                {
                    Task.Factory.StartNew(()=>StartConnection(peer));
                }
                WaitForStop();
            }
            catch (Exception e)
            {
                OnRaisedException(e);
            }
            StopActions();
        }

        private void StartConnection(PeerEndpoint peer)
        {
            var pstr = "BitTorrent protocol";
            var pstrlen = pstr.Length;
            var reserved = new byte[8];
            var info_hash = Data.InfoHash;
            var peer_id = LocalInfo.Instance.PeerId;

            var msgList = new List<byte>();
            msgList.Add((byte)pstrlen);
            msgList.AddRange(Encoding.UTF8.GetBytes(pstr));
            msgList.AddRange(reserved);
            msgList.AddRange(info_hash);
            msgList.AddRange(peer_id);

            var handshakeMessage = msgList.ToArray();
            try
            {
                if (stop) return;
                Debug.WriteLine("Creating client and connecting to " + peer.IP);
                var client = new TcpClient();
                client.Connect(new IPEndPoint(peer.IP, peer.Port));
                var stream = client.GetStream();

                SendMessage(handshakeMessage, stream);
                IPeerMessage response = ReadMessage(stream);
                Debug.WriteLine(response.ToString(), "Response");
                Debug.WriteLine("Successful " + peer.IP);
                if (stop) return;
                OnSentHandshake(peer);
                if (response is HandshakeMessage)
                    OnReceivedHandshake(new IPEndPoint(peer.IP, peer.Port));
            }
            catch (Exception e)
            {
                OnRaisedException(e);
            }
        }

        private IPeerMessage ReadMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int read = 0;
            int count = 0;

            var first = stream.ReadByte(); read++;
            buffer[0] = (byte)first;

            if (first == -1) return null;
            if (first == 0) return PeerMessage.CreateFromBytes(buffer, 0, 1);
            if (first == 19)
            {
                stream.Read(buffer, 1, 67);
                return PeerMessage.CreateFromBytes(buffer, 0, 68);
            }

            using (var mstr = new MemoryStream(buffer))
            {
                buffer[0] = 0;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    mstr.Write(buffer, 0, count);
                }
                var message = mstr.ToArray();
                return PeerMessage.CreateFromBytes(message, 0, message.Length);
            }

        }

        private void StartActions()
        {
            stop = false;
            Running = true;
        }

        private void StopActions()
        {
            OnStopping();
            Running = false;
        }

        private void WaitForStop()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (stop) break;
            }
        }

        private void Listen()
        {
            var listener = new TcpListener(IPAddress.Any, LocalInfo.Instance.ListeningPort);
            listener.Start();

            while (true)
            {
                try
                {
                    if (stop) break;
                    if (!listener.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var client = listener.AcceptTcpClient();
                    var endpoint = client.Client.RemoteEndPoint;
                    var stream = client.GetStream();
                    var message = ReadMessage(stream);
                    client.Close();
                    //ProcessMessage(message, endpoint);
                }
                catch (Exception e)
                {
                    OnRaisedException(e);
                }
            }
            listener.Stop();
        }     

        private void SendMessage(byte[] msg, NetworkStream stream)
        {
            stream.Write(msg, 0, msg.Length);
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
                catch(Exception e) { continue; }
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

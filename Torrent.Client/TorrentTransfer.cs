using System;
using System.Collections.Concurrent;
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
        private TrackerClient tracker;
        private List<IPEndPoint> Endpoints;
        private NetworkCallback PeerConnectedCallback;
        private Socket listenSocket;
        private HandshakeMessage localHandshake;
        /// <summary>
        /// The metadata decribing the torrent.
        /// </summary>
        public TorrentData Data { get; private set; }

        public ConcurrentDictionary<string, PeerState> Peers { get; private set; }

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

            Endpoints = new List<IPEndPoint>();

            using (torrentStream)
            using (var reader = new BinaryReader(torrentStream))
            {
                var bytes = reader.ReadBytes((int)reader.BaseStream.Length);
                this.Data = new TorrentData(bytes);
            }

            tracker = new TrackerClient(this.Data.Announces);
            this.Peers = new ConcurrentDictionary<string, PeerState>();
            this.PeerConnectedCallback = PeerConnected;
            listenSocket = Global.Instance.Listener;
            localHandshake = new HandshakeMessage(Global.Instance.PeerId, new byte[8], Data.InfoHash, "BitTorrent protocol");
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
                RegisterForListen();
                HandshakeTracker();
                ConnectToPeers();
                WaitForStop();
            }
            catch (Exception e)
            {
                OnRaisedException(e);
            }
            StopActions();
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

        void PeerListener_RaisedException(object sender, Exception e)
        {
            OnRaisedException(e);
            Stop();
        }

        private void ReceivedPeer(PeerState peer)
        {
            Debug.WriteLine("I got a peer who connected to me first!");
            InitializePeer(peer);
            SendMessage(peer.Socket, localHandshake, peer, HandshakeSent);
        }

        private void HandshakeTracker()
        {
            var info = tracker.AnnounceStart(Data.InfoHash, Global.Instance.PeerId, Global.Instance.ListeningPort,
                0, 0, (long)this.Data.Files.Sum(f => f.Length));
            Endpoints = info.Endpoints;//gotta pee k
            
        }


        private void ConnectToPeers()
        {
            foreach (var peerEndpoint in Endpoints)
            {
                if (stop) break;
                var peer = InitializePeer(peerEndpoint);
                NetworkIO.Connect(peer.Socket, peer.EndPoint, peer, PeerConnected);
            }
        }

        private PeerState InitializePeer(IPEndPoint peerEndpoint)
        {
            var peer = new PeerState(new Socket(SocketType.Stream, ProtocolType.Tcp), peerEndpoint);
            peer.Bitfield = new System.Collections.BitArray(Data.Checksums.Count);
            return peer;
        }

        private PeerState InitializePeer(PeerState peer)
        {
            peer.Bitfield = new System.Collections.BitArray(Data.Checksums.Count);
            return peer;
        }

        private void PeerConnected(bool success, int transmitted, object state)
        {
            if (stop) return;
            var peer = (PeerState)state;
            if (success)
            {
                SendMessage(peer.Socket, localHandshake, peer, HandshakeSent);
            }
        }

        

        private void StopActions()
        {
            OnStopping();
            ClosePeerSockets();
            DeregisterFromListen();
            Running = false;
        }

        private void DeregisterFromListen()
        {
            PeerListener.Deregister(Data.InfoHash);
            PeerListener.RaisedException -= PeerListener_RaisedException;
        }

        private void ClosePeerSockets()
        {
            foreach (var peer in Peers)
            {
                ClosePeerSocket(peer.Value);
            }
        }

        private void WaitForStop()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (stop) break;
            }
        }

        private void HandshakeReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState)state;
            var handshake = (HandshakeMessage)message;
            if(success)
            {            
                OnReceivedMessage(message);
                peer.ReceivedHandshake = true;
                peer.ID = handshake.PeerID;
                
                if (peer.ID == Global.Instance.PeerId) return;

                if (!peer.SentHandshake)
                {
                    SendMessage(peer.Socket, localHandshake, peer, HandshakeSent);
                }
                else
                {
                    HandshakeCompleted(peer);
                }
            }
        }

        private void HandshakeCompleted(PeerState peer)
        {
            Debug.WriteLine("Successful handshake.");
            Peers.AddOrUpdate(peer.ID, peer, (id, s) => s);
            OnGotPeers();

            MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
        }

        private void MessageReceived(bool success, PeerMessage message, object state)
        {
            var peer = (PeerState)state;
            if (success)
            {
                
                if (message is BitfieldMessage)
                {
                    HandleBitfield(message, peer);
                    peer.AmInterested = true;
                    SendMessage(peer.Socket, new InterestedMessage(), peer, MessageSent);
                }
                else if (message is HaveMessage)
                {
                    HandleHave(message, peer);
                }
                else if (message is UnchokeMessage)
                {
                    peer.AmChoked = false;
                    SendMessageTo(peer, new RequestMessage(2, 0, 1024 * 16));
                }
                else if (message is InterestedMessage)
                {
                    peer.IsInterested = true;
                }
                else if (message is ChokeMessage)
                {
                    peer.AmChoked = true;
                }
                else if (message is NotInterestedMessage)
                {
                    peer.IsInterested = false;
                }
                MessageIO.ReceiveMessage(peer.Socket, peer, MessageReceived);
                OnReceivedMessage(message);
            }
            else
            {
                ClosePeerSocket(peer);
            }
        }

        private void HandleHave(PeerMessage message, PeerState peer)
        {
            var have = (HaveMessage)message;
            peer.Bitfield.Set(have.PieceIndex, true);
        }

        private void HandleBitfield(PeerMessage message, PeerState peer)
        {
            peer.Bitfield = ((BitfieldMessage)message).Bitfield;
        }

        private static void ClosePeerSocket(PeerState peer)
        {
            if (peer!=null && peer.Socket!=null && peer.Socket.Connected)
            {
                Debug.WriteLine("Closing socket with " + peer.Socket.RemoteEndPoint);
                peer.Socket.Shutdown(SocketShutdown.Both);
                peer.Socket.Close();
            }
        }

        private void HandshakeSent(bool success, int sent, object state)
        {
            var peer = (PeerState)state;
            if (success)
            {
                peer.SentHandshake = true;
                if (peer.ID == Global.Instance.PeerId) return;

                if (!peer.ReceivedHandshake)
                {
                    MessageIO.ReceiveHandshake(peer.Socket, peer, HandshakeReceived);
                }
                else
                {
                    HandshakeCompleted(peer);
                }
            }
        }

        private void MessageSent(bool success, int sent, object state)
        {
            Debug.WriteLine("I sent some message, success: " + success);
        }

        private void SendMessageTo(PeerState peer, PeerMessage message)
        {
            SendMessage(peer.Socket, message, peer, MessageSent);
        }

        private void SendMessage(Socket socket, PeerMessage message, PeerState state, MessageSentCallback callback)
        {
            MessageIO.SendMessage(socket, message, state, callback);
            OnSentMessage(message);
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

        private void OnGotTcpMessage(string msg)
        {
            if (GotTcpMessage != null)
            {
                GotTcpMessage(this, msg);
            }
        }

        private void OnSentHandshake(EndPoint peer)
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

        public event EventHandler<string> GotTcpMessage;

        public event EventHandler<EndPoint> SentHandshake;

        public event EventHandler<EndPoint> ReceivedHandshake;

        public event EventHandler<PeerMessage> ReceivedMessage;

        public event EventHandler<PeerMessage> SentMessage;
        #endregion
    }
}

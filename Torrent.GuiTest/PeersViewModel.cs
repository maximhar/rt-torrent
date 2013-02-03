using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Torrent.Client;
using MoreLinq;
using System.Windows.Threading;
using System.Net;
using Torrent.Client.Messages;

namespace Torrent.GuiTest
{
    /// <summary>
    /// A class providing human-readable representation of a .torrent file.
    /// </summary>
    class PeersViewModel:INotifyPropertyChanged
    {
        ObservableCollection<PeerState> peers;
        ObservableCollection<FileEntry> files;
        ObservableCollection<string> announces;
        ObservableCollection<string> messages;
        string hash;
        Window mainWindow;
        Dispatcher dispatcher;
        string name;
        
        int pieceLength;
        string announceURL;
        private int pieceCount;
        private string downloaded;
        private long filesSize;
        private string totalSize;
        private string percentDone;
        private string speed;
        private long oldSize;
        private DateTime past;

        /// <summary>
        /// Gets or sets an ObservableCollection of FileEntries representing the files in the the torrent.
        /// </summary>
        public ObservableCollection<FileEntry> Files
        {
            get { return files; }
            set
            {
                files = value;
                OnPropertyChanged("Files");
            }
        }

        /// <summary>
        /// Gets or sets an ObservableCollection of strings representing the announce URLs of the torrent.
        /// </summary>
        public ObservableCollection<string> Announces
        {
            get { return announces; }
            set
            {
                announces = value;
                OnPropertyChanged("Announces");
            }
        }

        public ObservableCollection<string> Messages
        {
            get { return messages; }
            set
            {
                messages = value;
                OnPropertyChanged("Messages");
            }
        }

        /// <summary>
        /// Gets or sets the number of pieces of the torrent.
        /// </summary>
        public int PieceCount
        {
            get { return pieceCount; }
            set
            {
                pieceCount = value;
                OnPropertyChanged("PieceCount");
            }
        }

        /// <summary>
        /// Gets or sets the Announce URL of the torrent.
        /// </summary>
        public string AnnounceURL
        {
            get { return announceURL; }
            set
            {
                announceURL = value;
                OnPropertyChanged("AnnounceURL");
            }
        }

        /// <summary>
        /// Gets or sets the length of a peice of the torrent.
        /// </summary>
        public int PieceLength
        {
            get { return pieceLength; }
            set
            {
                pieceLength = value;
                OnPropertyChanged("PieceLength");
            }
        }
        
        /// <summary>
        /// Gets or sets the name of the torrent
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public string Downloaded
        {
            get { return downloaded + " / " + totalSize + " ( "+percentDone+" ) Speed: " + speed; }
            set
            {
                downloaded = value;
                OnPropertyChanged("Downloaded");
            }
        }

        /// <summary>
        /// Gets or sets and ObservableCollection of PeerEndpoints of the torrent.
        /// </summary>
        public ObservableCollection<PeerState> Peers
        {
            get { return peers; }
            set
            {
                peers = value;
                OnPropertyChanged("Peers");
            }
        }

        /// <summary>
        /// Gets or sets the hash of the torrent.
        /// </summary>
        public string Hash
        {
            get { return hash; }
            set
            {
                hash = value;
                OnPropertyChanged("Hash");
            }
        }

        private TorrentTransfer torrent;

        /// <summary>
        /// Initializes an instance of the Torrent.GuiTest.PeersViewModel class vie a Window object.
        /// </summary>
        /// <param name="window">The Window object.</param>
        public PeersViewModel(Window window)
        {
            Peers = new ObservableCollection<PeerState>();
            Files = new ObservableCollection<FileEntry>();
            Announces = new ObservableCollection<string>();
            Messages = new ObservableCollection<string>();

            mainWindow = window;
            dispatcher = window.Dispatcher;
        }

        public void GetPeers(string path)
        {
            try
            {
                Peers.Clear();
                Announces.Clear();
                Files.Clear();

                torrent = new TorrentTransfer(path);
                PieceLength = torrent.Data.PieceLength;
                PieceCount = torrent.Data.Checksums.Count;
                Name=torrent.Data.Name;
                Hash = BitConverter.ToString(torrent.Data.InfoHash).Replace("-", string.Empty);
                PieceLength = torrent.Data.PieceLength;
                AnnounceURL = torrent.Data.AnnounceURL;

                torrent.Data.Files.ForEach(f => Files.Add(f));
                if (torrent.Data.AnnounceList != null)
                    torrent.Data.AnnounceList.ForEach(a => Announces.Add(a));

                filesSize = 0;
                foreach (var file in files)
                {
                    filesSize += file.Length;
                }
                totalSize = Global.Instance.FileSizeFormat(filesSize);

                torrent.GotPeers += torrent_GotPeers;
                torrent.RaisedException += torrent_RaisedException;
                torrent.Stopping += torrent_Stopping;
                torrent.ReceivedMessage +=torrent_ReceivedMessage;
                torrent.SentMessage += torrent_SentMessage;
                torrent.PeersChanged += torrent_PeersChanged;
                torrent.DownloadedBytes += torrent_DownloadedBytes;
                torrent.Start();
                
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        void torrent_DownloadedBytes(object sender, long e)
        {
            if ((DateTime.Now - past).TotalSeconds > 1)
            {
                speed = Global.Instance.FileSizeFormat((long)((double)(e - oldSize) / (DateTime.Now - past).TotalSeconds)) + "/s";
                past = DateTime.Now;
                oldSize = e;
            }
            Downloaded = Global.Instance.FileSizeFormat(e);
            percentDone = String.Format("{0:0.0}%", (double)e * 100 / (double)filesSize);

        }

        void torrent_PeersChanged(object sender, IEnumerable<PeerState> e)
        {
            Peers = new ObservableCollection<PeerState>(e);
        }

        void torrent_SentMessage(object sender, PeerMessage e)
        {
            dispatcher.Invoke(() => AddMessage("Sent: " + e));
        }

        private void torrent_ReceivedMessage(object sender, PeerMessage e)
        {
            dispatcher.Invoke(() => AddMessage("Received: " + e));
        }

        public void Stop()
        {
            if (torrent != null && torrent.Running)
            {
                AddMessage("Stopping...");
                torrent.Stop();
            }
            else
            {
                AddMessage("Torrent not running.");
            }
        }

        void torrent_Stopping(object sender, EventArgs e)
        {
            dispatcher.Invoke(() => AddMessage("Torrent said it's stopping."));
        }

        private void AddMessage(string message)
        {
            Messages.Add(message);
        }

        private void HandleException(Exception e)
        {
            AddMessage(string.Format("Ooops. Something bad happened. {0}", e.Message));
        }

        void torrent_RaisedException(object sender, Exception e)
        {
            dispatcher.Invoke(new Action(() => HandleException(e)));
        }

        void torrent_GotPeers(object sender, EventArgs e)
        {
            if (torrent.Peers != null)
            {
                dispatcher.Invoke(() =>
                    {
                        Peers.Clear();
                        torrent.Peers.ForEach(p => Peers.Add(p.Value));
                    });
            }
        }

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

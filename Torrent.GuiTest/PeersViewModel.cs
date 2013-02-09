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
using System.Windows.Threading;
using System.Net;
using Torrent.Client.Events;
using Torrent.Client.Extensions;
using Torrent.Client.Messages;
using System.Globalization;

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
        private string averageSpeed;
        private long oldSize;
        private DateTime past;
        private DateTime begin = DateTime.Now;
        private string totalTime;

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
        private int chokedBy;
        private int totalPeers;
        public string Downloaded
        {
            get
            {
                return downloaded + " / " + totalSize + " ( " + percentDone + " ) Speed: " + speed + " Average: " + averageSpeed + " Total peers: "
                    + totalPeers + " | Choked by " + chokedBy + " peers, queued: " + queued + " Total time: " + totalTime;
            }
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
                AddMessage("Attempting to open torrent file.");
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

                torrent.RaisedException += torrent_RaisedException;
                torrent.Stopping += torrent_Stopping;
                torrent.PeersChanged += torrent_PeersChanged;
                torrent.ReportStats += torrent_ReportStatsChanged;
                torrent.StateChanged += torrent_StateChanged;
                torrent.Start();
                
            }
            catch (Exception e)
            {
                HandleException(e);
                MessageBox.Show("Damn: " + e);
            }
        }

        void torrent_StateChanged(object sender, EventArgs<TorrentState> e)
        {
            dispatcher.Invoke(new Action(() => AddMessage("State: " + e.Value)));
        }

        private void torrent_ReportStatsChanged(object sender, StatsEventArgs e)
        {
            DownloadedBytes(e.DownloadedBytes);
            chokedBy = e.ChokedBy;
            totalPeers = e.TotalPeers;
            queued = e.QueuedRequests;
        }

        void DownloadedBytes(long downloadedBytes)
        {
            speed =
                Global.Instance.FileSizeFormat(
                    (long)((double)(downloadedBytes - oldSize)/(DateTime.Now - past).TotalSeconds)) + "/s";
            past = DateTime.Now;
            oldSize = downloadedBytes;
            Downloaded = Global.Instance.FileSizeFormat(downloadedBytes);
            percentDone = String.Format("{0:0.0}%", (double)downloadedBytes*100/(double)filesSize);
            averageSpeed = Global.Instance.FileSizeFormat((long)((double)(downloadedBytes) / (DateTime.Now - begin).TotalSeconds)) + "/s";
            TotalElapsedTime();
        }

        private void TotalElapsedTime()
        {
            var elapsedTime = DateTime.Now - begin;
            if (elapsedTime.TotalSeconds < 60)
                totalTime = elapsedTime.ToString(@"ss") + " s";
            else if (elapsedTime.TotalMinutes < 60)
                totalTime = elapsedTime.ToString(@"mm\:ss");
            else totalTime = elapsedTime.ToString(@"h\:mm\:ss");
        }

        void torrent_PeersChanged(object sender, EventArgs<IEnumerable<PeerState>> e)
        {
            Peers = new ObservableCollection<PeerState>(e.Value);
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
            dispatcher.Invoke(new Action(() => AddMessage("Torrent said it's stopping.")));
        }

        private void AddMessage(string message)
        {
            Messages.Add(message);
        }

        private void HandleException(Exception e)
        {
            AddMessage(string.Format("Ooops. Something bad happened. {0}", e.Message));
        }

        void torrent_RaisedException(object sender, EventArgs<Exception> e)
        {
            dispatcher.Invoke(new Action(() => HandleException(e.Value)));
        }

        void torrent_GotPeers(object sender, EventArgs e)
        {
            if (torrent.Peers != null)
            {
                dispatcher.Invoke(new Action(() =>
                                            {
                                                Peers.Clear();
                                                torrent.Peers.ForEach(p => Peers.Add(p.Value));
                                            }));
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
        private int queued;
        private TorrentState state;
    }
}

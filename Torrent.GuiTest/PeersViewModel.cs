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

namespace Torrent.GuiTest
{
    /// <summary>
    /// A class providing human-readable representation of a .torrent file.
    /// </summary>
    class PeersViewModel:INotifyPropertyChanged
    {
        ObservableCollection<PeerEndpoint> peers;
        ObservableCollection<FileEntry> files;
        ObservableCollection<string> announces;
        string hash;
        Window mainWindow;
        Dispatcher dispatcher;
        string name;
        
        int pieceLength;
        string announceURL;
        private int pieceCount;

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

        /// <summary>
        /// Gets or sets and ObservableCollection of PeerEndpoints of the torrent.
        /// </summary>
        public ObservableCollection<PeerEndpoint> Peers
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
            Peers = new ObservableCollection<PeerEndpoint>();
            Files = new ObservableCollection<FileEntry>();
            Announces = new ObservableCollection<string>();
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

                torrent.GotPeers += torrent_GotPeers;
                torrent.RaisedException += torrent_RaisedException;
                torrent.Start();
                
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private void HandleException(Exception e)
        {
            MessageBox.Show(mainWindow, string.Format("Ooops. Something bad happened. {0}", e.Message));
        }

        void torrent_RaisedException(object sender, Exception e)
        {
            dispatcher.Invoke(new Action(() => HandleException(e)));
        }

        void torrent_GotPeers(object sender, EventArgs e)
        {
            if (torrent.PeerEndpoints != null)
                dispatcher.Invoke(() => torrent.PeerEndpoints.ForEach(p => Peers.Add(p)));
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

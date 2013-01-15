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

namespace Torrent.GuiTest
{
    class PeersViewModel:INotifyPropertyChanged
    {
        ObservableCollection<PeerEndpoint> peers;
        ObservableCollection<FileEntry> files;
        ObservableCollection<string> announces;
        string hash;
        Window mainWindow;
        string name;
        
        int pieceLength;
        string announceURL;
        private int pieceCount;

        public ObservableCollection<FileEntry> Files
        {
            get { return files; }
            set
            {
                files = value;
                OnPropertyChanged("Files");
            }
        }

        public ObservableCollection<string> Announces
        {
            get { return announces; }
            set
            {
                announces = value;
                OnPropertyChanged("Announces");
            }
        }

        public int PieceCount
        {
            get { return pieceCount; }
            set
            {
                pieceCount = value;
                OnPropertyChanged("PieceCount");
            }
        }

        public string AnnounceURL
        {
            get { return announceURL; }
            set
            {
                announceURL = value;
                OnPropertyChanged("AnnounceURL");
            }
        }

        public int PieceLength
        {
            get { return pieceLength; }
            set
            {
                pieceLength = value;
                OnPropertyChanged("PieceLength");
            }
        }
        
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        public ObservableCollection<PeerEndpoint> Peers
        {
            get { return peers; }
            set
            {
                peers = value;
                OnPropertyChanged("Peers");
            }
        }
        public string Hash
        {
            get { return hash; }
            set
            {
                hash = value;
                OnPropertyChanged("Hash");
            }
        }

        public PeersViewModel(Window window)
        {
            Peers = new ObservableCollection<PeerEndpoint>();
            Files = new ObservableCollection<FileEntry>();
            Announces = new ObservableCollection<string>();
            mainWindow = window;
        }

        public async void GetPeers(string path)
        {
            try
            {
                Peers.Clear();
               
                var torrent = new TorrentData(path);
                var hasher = SHA1.Create();
                PieceLength = torrent.PieceLength;
                PieceCount = torrent.Checksums.Count;
                Name=torrent.Name;
                PieceLength = torrent.PieceLength;
                AnnounceURL = torrent.AnnounceURL;
                torrent.Files.ForEach(f => Files.Add(f));
                if (torrent.AnnounceList != null)
                    torrent.AnnounceList.ForEach(a => Announces.Add(a));
                string bencoded = torrent.Info.ToBencodedString();
                byte[] bytes = bencoded.Select(c => (byte)c).ToArray();
                byte[] hash = hasher.ComputeHash(bytes);
                
                Hash = BitConverter.ToString(hash).Replace("-", string.Empty);

                var request = new TrackerRequest(hash,
                    Encoding.ASCII.GetBytes("-UT3230-761290182730"), 8910, 0, 0, (long)torrent.Files.Sum(f => f.Length),
                    false, false, numWant: 200, @event: EventType.Started);
                var client = new TrackerClient(torrent.AnnounceURL);
                var res = await client.GetResponseAsync(request);
                if (res.PeerEndpoints != null)
                    res.PeerEndpoints.ForEach(p => Peers.Add(p));
                if (res.FailureReason != null)
                    MessageBox.Show(mainWindow, string.Format("Failure reason: {0}", res.FailureReason));
            }
            catch (Exception e)
            {
                MessageBox.Show(mainWindow, string.Format("Ooops. Something bad happened. {0}", e.Message));
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

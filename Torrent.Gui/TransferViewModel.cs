using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Torrent.Client;

namespace Torrent.Gui
{
    class Transfer:INotifyPropertyChanged
    {
        private string name;
        private float progress;
        private Mode mode;
        private ICollection<string> files = new List<string>();
        private TorrentTransfer transfer;

        public Transfer(TorrentTransfer transfer)
        {
            this.transfer = transfer;
            this.transfer.StateChanged += transfer_StateChanged;
            this.transfer.ReportStats += transfer_ReportStats;
            this.Mode = Mode.Idle;
            this.Name = transfer.Data.Name;
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
        }

        public ICommand StartCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public string Name
        {
            get { return name; }
            set { name = value; RaisePropertyChanged("Name"); }
        }

        public Mode Mode
        {
            get { return mode; }
            set { mode = value; RaisePropertyChanged("Mode"); }
        }

        public float Progress
        {
            get { return progress; }
            set { progress = value; RaisePropertyChanged("Progress"); }
        }

        public ICollection<string> Files
        {
            get { return files; }
            set { files = value; RaisePropertyChanged("Files"); }
        }

        public void Start()
        {
            transfer.Start();
        }

        public void Stop()
        {
            transfer.Stop();
        }

        void transfer_ReportStats(object sender, Client.Events.StatsEventArgs e)
        {
            Progress = e.BytesCompleted / transfer.Data.TotalLength;
        }

        void transfer_StateChanged(object sender, Client.Events.EventArgs<TorrentState> e)
        {
            switch (e.Value)
            {
                case TorrentState.Hashing:
                    Mode = Mode.Hash;
                    break;
                case TorrentState.Downloading:
                    Mode = Mode.Download;
                    break;
                case TorrentState.Seeding:
                    Mode = Mode.Seed;
                    break;
                case TorrentState.NotRunning:
                    Mode = Mode.Stopped;
                    break;
            }
        }


        private void RaisePropertyChanged(string name)
        {
            if(PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Torrent.Client;

namespace Torrent.Gui
{
    class Transfer : ViewModelBase, INotifyPropertyChanged
    {
        private string name;
        private float progress;
        private Mode mode;
        private ICollection<string> files = new List<string>();
        private TorrentTransfer transfer;
        private float speed;
        private long lastDownloaded;
        private DateTime lastTimeReported;
        private  string error;
        private NumericProgress numericProgress;
private  bool receivedAny;

        public Transfer(TorrentTransfer transfer)
        {
            TorrentTransfer = transfer;
            TorrentTransfer.StateChanged += transfer_StateChanged;
            TorrentTransfer.ReportStats += transfer_ReportStats;
            TorrentTransfer.RaisedException += TorrentTransfer_RaisedException;
            Mode = Mode.Idle;
            Name = transfer.Data.Name;
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            numericProgress.BytesTotal = TorrentTransfer.Data.TotalLength;
        }


        public bool CanDelete { get { return CanStart; } }

        public bool CanStart
        {
            get
            {
                return Mode == Mode.Idle || Mode == Mode.Stopped
                    || Mode == Mode.Completed || Mode == Mode.Error;
            }
        }

        public bool CanStop
        {
            get
            {
                return Mode == Mode.Hash || Mode == Mode.Download || Mode == Mode.Seed;
            }
        }

        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand OpenFolderCommand { get; private set; }


        public InfoHash InfoHash
        {
            get { return TorrentTransfer.Data.InfoHash; }
        }

        public TorrentTransfer TorrentTransfer { get; private set; }

        public string Error { 
            get { return error; }
            set { error = value; RaisePropertyChanged("Error"); }
        }

        public NumericProgress NumericProgress
        {
            get { return numericProgress; }
            set { numericProgress = value; RaisePropertyChanged("NumericProgress"); }
        }

        public bool ReceivedAny
        {
            get { return receivedAny && CanStop; }
            set { receivedAny = value; RaisePropertyChanged("ReceivedAny"); }
        }

        public float Speed
        {
            get { return speed; }
            set { speed = value; RaisePropertyChanged("Speed"); }
        }

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
            TorrentTransfer.Start();
            Error = null;
        }

        public void Stop()
        {
            TorrentTransfer.Stop();
        }

        void TorrentTransfer_RaisedException(object sender, Client.Events.EventArgs<Exception> e)
        {
            Mode = Gui.Mode.Error;
            Error = e.Value.Message;
        }


        private void OpenFolder()
        {
            try
            {
                Process.Start(TorrentTransfer.DownloadFolder);
            }
            catch { }
        }

        void transfer_ReportStats(object sender, Client.Events.StatsEventArgs e)
        {
            if (e.BytesCompleted > 0) ReceivedAny = false;

            Progress = (float)e.BytesCompleted / (float)TorrentTransfer.Data.TotalLength;
            Speed = (e.BytesCompleted - lastDownloaded) / (float)(DateTime.Now - lastTimeReported).TotalSeconds;

            numericProgress.BytesComplete = e.BytesCompleted;
            RaisePropertyChanged("NumericProgress");
            lastTimeReported = DateTime.Now;
            lastDownloaded = e.BytesCompleted;
        }

        void transfer_StateChanged(object sender, Client.Events.EventArgs<TorrentState> e)
        {
            ReceivedAny = true;
            lastDownloaded = 0;
            lastTimeReported = DateTime.Now;
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
                    if (TorrentTransfer.Complete)
                        Mode = Mode.Completed;
                    else
                        if(Mode != Gui.Mode.Error)
                            Mode = Mode.Stopped;
                    break;
            }
            RaisePropertyChanged("ReceivedAny");
            RaisePropertyChanged("CanStart");
            RaisePropertyChanged("CanStop");
            RaisePropertyChanged("CanDelete");
        }

    }
}

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Torrent.Client;
namespace Torrent.Gui
{
    class MainViewModel:ViewModelBase
    {
        public ObservableCollection<Transfer> Transfers { get; private set; }

        public ICommand AddTransferCommand { get; set; }

        public IOService IOService { get; private set; }

        public MainViewModel(IOService ioService)
        {
            Transfers = new ObservableCollection<Transfer>();
            AddTransferCommand = new RelayCommand<object>(param => AddTransfer(param));
            IOService = ioService;
        }

        private void AddTransfer(object path)
        {
            string filePath = path == null ? IOService.OpenFile("Torrent files|*.torrent") : path as string;
            string saveFolder = IOService.OpenFolder();
            if (filePath != null && saveFolder != null)
                Transfers.Add(new Transfer(new TorrentTransfer(filePath, saveFolder)));
        }

    }
}

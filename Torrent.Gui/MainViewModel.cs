using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        public ICommand RemoveTransferCommand { get; set; }

        public IOService IOService { get; private set; }

        public MainViewModel(IOService ioService)
        {
            Transfers = new ObservableCollection<Transfer>();
            AddTransferCommand = new RelayCommand<string>(param => AddTransfer(param));
            RemoveTransferCommand = new RelayCommand<Transfer>(param => RemoveTransfer(param));
            IOService = ioService;
        }

        private void RemoveTransfer(Transfer transfer)
        {
            Transfers.Remove(transfer);
        }

        private void AddTransfer(string path)
        {
            if (path != null && Path.GetExtension(path) != ".torrent")
            {
                IOService.ShowErrorMessage("Not a valid torrent file", "Only files with a .torrent extension are recognized as valid.");
                return;
            }

            try
            {
                string filePath = path == null ? IOService.OpenFile("Torrent files|*.torrent") : path;
                string saveFolder = IOService.OpenFolder(Path.GetFileName(path));
                if (filePath != null && saveFolder != null)
                {
                    var transfer = new Transfer(new TorrentTransfer(filePath, saveFolder));
                    if (!Transfers.Any(t => t.InfoHash == transfer.InfoHash))
                        Transfers.Add(transfer);
                    else
                        IOService.ShowErrorMessage("Torrent transfer already exists",
                            string.Format("{0} is already added and can not be added again.", path));
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception when adding transfer " + e);
                IOService.ShowErrorMessage("Adding transfer failed", string.Format("Adding transfer failed due to: {0}", e.Message));
            }
        }

    }
}

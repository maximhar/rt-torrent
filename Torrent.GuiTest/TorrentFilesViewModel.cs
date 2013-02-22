using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Torrent.Client;
using Torrent.Client.Extensions;

namespace Torrent.GuiTest
{
    class TorrentFilesViewModel
    {
        private ObservableCollection<FileEntry> filesToAdd;
        public ObservableCollection<FileEntry> FilesToAdd
        {
            get { return filesToAdd; }
            set { filesToAdd = value; }
        }

        public TorrentFilesViewModel()
        {
            FilesToAdd = new ObservableCollection<FileEntry>();
        }

        public void AddFiles(string[] fileNames)
        {
            fileNames.ForEach(AddFile);
        }

        private void AddFile(string fn)
        {
            filesToAdd.Add(new FileEntry(fn, new FileInfo(fn).Length));
        }

        public void Remove(FileEntry fileEntry)
        {
            filesToAdd.Remove(fileEntry);
        }

        internal void AddFolder(string folderPath)
        {
            Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).ForEach(AddFile);
        }
    }
}

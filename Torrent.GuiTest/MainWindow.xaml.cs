using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Torrent.GuiTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PeersViewModel model;
        public MainWindow()
        {
            InitializeComponent();
            model = new PeersViewModel(this);
            this.DataContext = model;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var str = (string[])e.Data.GetData(DataFormats.FileDrop);
            OpenTorrent(str.FirstOrDefault());
        }

        private void OpenTorrent(string str)
        {
            TorrentPathTextBox.Text = str;
            model.GetPeers(TorrentPathTextBox.Text);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            model.Stop();
        }

        private void OpenTorrent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog(); 
            dlg.DefaultExt = ".torrent"; 
            dlg.Filter = "Torrent files (.torrent)|*.torrent"; // Filter files by extension 

            if (dlg.ShowDialog() == true)
                OpenTorrent(dlg.FileName);
        }

        private void CreateTorrent_Click(object sender, RoutedEventArgs e)
        {

        }
        

    }
}

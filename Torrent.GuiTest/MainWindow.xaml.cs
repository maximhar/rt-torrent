using Microsoft.Win32;
using System.Linq;
using System.Windows;
using Torrent.Client;

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
            var createTorrentWindow = new CreateTorrentWindow();
            createTorrentWindow.Owner = this;
            createTorrentWindow.Show();
        }

        private void Shutdown_Click(object sender, RoutedEventArgs e)
        {
            model.AutoShutdown = AutoShutdownCheck.IsChecked;
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            model.AutoQuit = AutoQuitCheck.IsChecked;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("RTorrent\nVersion: "+Global.Instance.Version+"\nCreated by: Maxim Harizanov & Dimitar Pankov\n2013", "About");
        }
    }
}

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

        private void GetPeersButton_Click(object sender, RoutedEventArgs e)
        {
            model.GetPeers(AnnounceUrlTextBox.Text);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var str = (string[])e.Data.GetData(DataFormats.FileDrop);
            AnnounceUrlTextBox.Text = str.FirstOrDefault();
            model.GetPeers(AnnounceUrlTextBox.Text);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            model.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Torrent.Client;

namespace Torrent.GuiTest
{
    /// <summary>
    /// Interaction logic for CreateTorrentWindow.xaml
    /// </summary>
    public partial class CreateTorrentWindow : Window
    {
        private readonly TorrentFilesViewModel model;

        public CreateTorrentWindow()
        {
            InitializeComponent();
            model = new TorrentFilesViewModel();
            this.DataContext = model;
            InitPieceSizesComboBox();
        }

        private void InitPieceSizesComboBox()
        {
            var pieceSizes = new List<PieceSize>();
            for (int i = 0, start = 16; i < 10; i++, start *= 2)
                pieceSizes.Add(new PieceSize(start.ToString() + " kB", start));
            PieceSizes.ItemsSource = pieceSizes;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            if (dlg.ShowDialog() == true)
                model.AddFiles(dlg.FileNames);
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            var bd = new System.Windows.Forms.FolderBrowserDialog();
            bd.Description = "Select the directory you want to add.";
            bd.ShowNewFolderButton = false;
            System.Windows.Forms.DialogResult result = bd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                model.AddFolder(bd.SelectedPath);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var si = FilesToAddListBox.SelectedItems.Cast<FileEntry>().ToList();
            foreach (var fileEntry in si)
                model.Remove(fileEntry);
        }

        private void CreateAndSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var announces = Announces.Text.Split(new char[] {' ', '\n'});

            SHA1 sha = new SHA1CryptoServiceProvider();

            //TorrentData.Create(TorrentName.Text, model.FilesToAdd.ToList(),,announces[0],announces.ToList(),(PieceSizes.SelectedItem as PieceSize).Value,);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    class PieceSize
    {
        public string Text { get; private set; }
        public int Value { get; private set; }

        public PieceSize(string text, int value)
        {
            this.Text = text;
            this.Value = value;
        }

        public override string ToString()
        {
            return Text;
        }

    }
}

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

namespace Torrent.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IOService
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(this);
        }

        public string OpenFile(string filter)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = filter;
            var result = dialog.ShowDialog(this);
            if (result == null) return null;
            return dialog.FileName;
        }

        public string SaveFile()
        {
            throw new NotImplementedException();
        }

        public string OpenFolder(string title)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = title;
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                return dialog.SelectedPath;
            return null;
        }

        private void Drop(object sender, DragEventArgs e)
        {
            var str = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach(var file in str)
                (this.DataContext as MainViewModel).AddTransferCommand.Execute(file);
        }


        public void ShowErrorMessage(string title, string content)
        {
            MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

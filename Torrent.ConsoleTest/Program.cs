using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client;
using Torrent.Client.Bencoding;
using System.IO;
using System.Security.Cryptography;
using MoreLinq;
namespace Torrent.ConsoleTest
{
    class Program
    {
        private const string PATH = "test.torrent";
        private TorrentTransfer torrent;
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }

        private void Run()
        {
            try
            {
                while (true)
                {
                    torrent = new TorrentTransfer(Console.ReadLine());
                    torrent.GotPeers += torrent_GotPeers;
                    torrent.Start();
                    for (; ; ) ;
                }
            }
            finally
            {
                Console.Read();
            }
        }

        void torrent_GotPeers(object sender, EventArgs e)
        {
            foreach (var p in torrent.PeerEndpoints)
            {
                Console.WriteLine(p);
            }
        }


    }
}

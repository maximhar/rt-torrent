using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torrent.Client;
using Torrent.Client.Bencoding;
using System.IO;

namespace Torrent.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }

        private void Run()
        {
            try
            {
                Console.WriteLine("Torrent path: ");
                string path = Console.ReadLine();
                TorrentData torrent = new TorrentData(path);
                Console.WriteLine("Torrent name: {0}", torrent.Name);
                Console.WriteLine("Announce URL: {0}", torrent.AnnounceURL);
                Console.WriteLine("Piece length: {0}", torrent.PieceLength);
                Console.WriteLine("Piece count: {0}", torrent.Checksums.Count);
                Console.WriteLine("Files: (count: {0})", torrent.Files.Count);
                foreach (var file in torrent.Files)
                {
                    Console.WriteLine("    {0,-50} {1}", file.Name, file.Length);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
            finally
            {
                Console.Read();
            }
        }


    }
}

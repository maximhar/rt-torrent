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

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
        }

        private async void Run()
        {
            try
            {
                while (true)
                {
                    TorrentData torrent = new TorrentData(Console.ReadLine());
                    Console.WriteLine("Torrent name: {0}", torrent.Name);
                    Console.WriteLine("Announce URL: {0}", torrent.AnnounceURL);
                    Console.WriteLine("Piece length: {0}", torrent.PieceLength);
                    Console.WriteLine("Piece count: {0}", torrent.Checksums.Count);
                    Console.WriteLine("Files: (count: {0})", torrent.Files.Count);



                    foreach (var file in torrent.Files)
                    {
                        Console.WriteLine("    {0,-50} {1}", file.Name, file.Length);
                    }
                    var hasher = SHA1.Create();

                    Console.WriteLine("Sending request");
                    string bencoded = torrent.Info.ToBencodedString();
                    byte[] bytes = bencoded.Select(c => (byte)c).ToArray();
                    File.WriteAllBytes("output.torrent", bytes);
                    byte[] hash = hasher.ComputeHash(bytes);
                    Console.WriteLine(BitConverter.ToString(hash));
                    
                    
                    var request = new TrackerRequest(hash,
                        Encoding.ASCII.GetBytes("-UT3230-761290182730"), 8910, 0, 0, (long)torrent.Files.Sum(f => f.Length),
                        false, false, numWant: 200, @event: EventType.Started);
                    var client = new TrackerClient(torrent.AnnounceURL);
                    var res = client.GetResponseAsync(request);
                    res.Wait();
                    Console.WriteLine(res.Result);
                }
            }
            finally
            {
                Console.Read();
            }
        }


    }
}

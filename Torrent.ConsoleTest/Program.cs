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
            BencodedStreamParser parser = new BencodedStreamParser(new StreamReader(File.Open("C:/test.torrent", FileMode.Open, FileAccess.Read), Encoding.ASCII));
            Console.WriteLine(parser.Parse());
            Console.Read();
        }


    }
}

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
            BencodedStreamParser parser = new BencodedStreamParser(new StringReader("li7ei12ei1023e4:hey!d3:loli5e3:omgi685eee"));
            Console.WriteLine(parser.Parse());
            Console.Read();
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public class TorrentFile
    {
        public string Name { get; private set; }
        public long Length { get; private set; }

        public TorrentFile(string name, long length)
        {
            this.Name = name;
            this.Length = length;
        }
    }
}

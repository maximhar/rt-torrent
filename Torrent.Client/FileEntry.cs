using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
namespace Torrent.Client
{
    /// <summary>
    /// Represents a file exchanged on the BitTorrent protocol.
    /// </summary>
    public class FileEntry
    {
        public string Name { get; private set; }
        public long Length { get; private set; }

        public FileEntry(string name, long length)
        {
            Contract.Requires(length >= 0);
            Contract.Requires(name != null);

            this.Name = name;
            this.Length = length;
        }
    }
}

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
        /// <summary>
        /// The name of the file enter. (in BitTorrent, includes full path)
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The length of the file in bytes.
        /// </summary>
        public long Length { get; private set; }
        /// <summary>
        /// Initializes a new instance of the Torrent.Client.FileEntry class.
        /// </summary>
        /// <param name="name">The name of the file. (full path)</param>
        /// <param name="length">The length of the file in bytes.</param>
        public FileEntry(string name, long length)
        {
            Contract.Requires(length >= 0);
            Contract.Requires(name != null);

            this.Name = name;
            this.Length = length;
        }
        /// <summary>
        /// Returns a string that represents the content of the FileEntry object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var size = FileSizeFormat(this.Length);
            return String.Format("{0} | {1}", this.Name, size);
        }

        private string FileSizeFormat(long size)
        {
            const int KB = 1024;
            const int MB = 1048576;
            const int GB = 1073741824;

            if (size < KB)
            {
                return string.Format("{0} bytes", size);
            }
            else if (size < MB)
            {
                return string.Format("{0:0.00} KB", ((float)size / KB));
            }
            else if (size < GB)
            {
                return string.Format("{0:0.00} MB", ((float)size / MB));
            }
            else
            {
                return string.Format("{0:0.00} GB", ((float)size / GB));
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Torrent.Client.Bencoding;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using MoreLinq;

namespace Torrent.Client
{
    /// <summary>
    /// Represents the BitTorrent metadata contained in a .torrent file.
    /// </summary>
    public class TorrentData
    {
        private const int CHECKSUM_SIZE = 20;

        public string AnnounceURL { get; private set; }
        public int PieceLength { get; private set; }
        public string Name { get; private set; }
        public ReadOnlyCollection<FileEntry> Files { get; private set; }
        public ReadOnlyCollection<byte[]> Checksums { get; private set; }

        private string path;
        /// <summary>
        /// Loads the metadata from a .torrent file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public TorrentData(string path)
        {
            Contract.Requires<TorrentException>(path != null);
            this.path = path;
            LoadMetadata();
        }

        private void LoadMetadata()
        {
            var metadata = GetMetadata();

            var announce = metadata["announce"] as BencodedString;
            var info = metadata["info"] as BencodedDictionary;

            CheckMetadata(announce, info);

            var pieceLength = info["piece length"] as BencodedInteger;

            var checksumList = GetRawChecksums(info, pieceLength);
            var name = GetNameFromInfo(info);
            var decodedFiles = DecodeFiles(info, name);
            
            this.AnnounceURL = announce;
            this.Checksums = checksumList.AsReadOnly();
            this.Files = decodedFiles.AsReadOnly();
            this.PieceLength = pieceLength;
            this.Name = name;
        }

        private void CheckMetadata(BencodedString announce, BencodedDictionary info)
        {
            if (announce == null || info == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'announce'/'info' not of expected type.", path));

        }

        private BencodedString GetNameFromInfo(BencodedDictionary info)
        {
            var name = info["name"] as BencodedString;
            if (name == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'name' not of expected type.", path));

            return name;
        }

        private List<FileEntry> DecodeFiles(BencodedDictionary info, BencodedString name)
        {
            var decodedFiles = new List<FileEntry>();
            if (info.ContainsKey("files")) //multiple-file torrent
            {
                var files = info["files"] as BencodedList;
                CheckInfoFiles(files);
                foreach (BencodedDictionary file in files)
                {
                    var torrentFile = CreateTorrentFile(file);
                    decodedFiles.Add(torrentFile);
                }
            }
            else //single-file torrent, we get the file name from the torrent name
            {
                var fileLength = info["length"] as BencodedInteger;
                CheckFileLength(fileLength); 
                decodedFiles.Add(new FileEntry(name, fileLength));
            }
            return decodedFiles;
        }

        private FileEntry CreateTorrentFile(BencodedDictionary file)
        {
            var filePathList = (file["path"] as BencodedList).Select(s => (string)(s as BencodedString)).ToArray();
            var fileLength = file["length"] as BencodedInteger;
            CheckFileProperties(filePathList, fileLength);
            string filePath = Path.Combine(filePathList);
            var torrentFile = new FileEntry(filePath, fileLength);
            return torrentFile;
        }

        private void CheckFileLength(BencodedInteger fileLength)
        {
            if (fileLength == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'length' not of expected type.", path));

        }

        private void CheckFileProperties(string[] filePathList, BencodedInteger fileLength)
        {
            if (filePathList == null || fileLength == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'path'/'length' not of expected type.", path));

        }

        private void CheckInfoFiles(BencodedList files)
        {
            if (files == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'files' not of expected type.", path));

        }

        private List<byte[]> GetRawChecksums(BencodedDictionary info, BencodedInteger pieceLength)
        {
            byte[] rawChecksums = Encoding.ASCII.GetBytes(info["pieces"] as BencodedString);
            if (pieceLength == null || rawChecksums == null || rawChecksums.Length % CHECKSUM_SIZE != 0) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'piece length'/'pieces' not of expected type, or invalid length of 'pieces'.", path));
            var slicedChecksums = rawChecksums.Batch(CHECKSUM_SIZE).Select(e=>e.ToArray());
            return slicedChecksums.ToList();
        }

        private BencodedDictionary GetMetadata()
        {
            var torrentFile = File.OpenRead(path);
            var decoder = new BencodedStreamParser(torrentFile);
            var metadata = decoder.Parse() as BencodedDictionary;
            CheckMetadata(metadata);
            return metadata;
        }

        private void CheckMetadata(BencodedDictionary metadata)
        {
            if (metadata == null) throw new TorrentException(string.Format("Invalid metadata in file {0}.", path));
            if (!metadata.ContainsKey("announce")) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'announce' not found.", path));
            if (!metadata.ContainsKey("info")) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'info' not found.", path));

        }
    }
}

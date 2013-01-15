using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Torrent.Client.Bencoding;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using MoreLinq;
using System.Security.Cryptography;

namespace Torrent.Client
{
    /// <summary>
    /// Represents the BitTorrent metadata contained in a .torrent file.
    /// </summary>
    public class TorrentData
    {
        private const int CHECKSUM_SIZE = 20;
        /// <summary>
        /// The URL of the announce server.
        /// </summary>
        public string AnnounceURL { get; private set; }
        /// <summary>
        /// The list of announce URLs, if available.
        /// </summary>
        public ReadOnlyCollection<string> AnnounceList { get; private set; }
        /// <summary>
        /// Length of the individual BitTorrent piece.
        /// </summary>
        public int PieceLength { get; private set; }
        /// <summary>
        /// Name of the torrent.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// A read-only list of the files in the torrent.
        /// </summary>
        public ReadOnlyCollection<FileEntry> Files { get; private set; }
        /// <summary>
        /// A read-only list of the raw SHA1 checksums of the pieces in the torrent.
        /// </summary>
        public ReadOnlyCollection<byte[]> Checksums { get; private set; }
        
        /// <summary>
        /// The SHA1 hash of the info dictionary of the torrent.
        /// </summary>
        public byte[] InfoHash { get; private set; }

        private byte[] data;
        /// <summary>
        /// Initializes a new instance of the Torrent.Client.TorrentData class with data from a byte array.
        /// </summary>
        /// <param name="data">The binary representation of the metadata.</param>
        public TorrentData(byte[] data)
        {
            Contract.Requires(data != null);

            this.data = data;

            try
            {
                LoadMetadata();
            }
            catch (Exception e)
            {
                throw new TorrentException("Unable to read torrent metadata.", e);
            }
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.TorrentData class with data from a specified .torrent file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public TorrentData(string path)
            : this(File.ReadAllBytes(path))
        {
            Contract.Requires(path != null);
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
            if (metadata.ContainsKey("announce-list"))
            {
                var announceList = (BencodedList)metadata["announce-list"];
                this.AnnounceList = DecodeAnnounceList(announceList).AsReadOnly();
            }
            this.AnnounceURL = announce;
            this.Checksums = checksumList.AsReadOnly();
            this.Files = decodedFiles.AsReadOnly();
            this.PieceLength = pieceLength;
            this.Name = name;
            this.InfoHash = ComputeInfoHash(info);
        }

        private byte[] ComputeInfoHash(BencodedDictionary info)
        {
            var hasher = SHA1.Create();
            string bencoded = info.ToBencodedString();
            byte[] bytes = bencoded.Select(c => (byte)c).ToArray();
            byte[] hash = hasher.ComputeHash(bytes);
            return hash;
        }

        private List<string> DecodeAnnounceList(BencodedList announceList)
        {
            var list = new List<string>();
            foreach (BencodedList urllist in announceList)
            {
                list.Add((BencodedString)urllist.First());
            }
            return list;
        }

        private void CheckMetadata(BencodedString announce, BencodedDictionary info)
        {
            if (announce == null || info == null) 
                throw new TorrentException(string.Format("Invalid metadata, 'announce'/'info' not of expected type."));

        }

        private BencodedString GetNameFromInfo(BencodedDictionary info)
        {
            var name = info["name"] as BencodedString;
            if (name == null) 
                throw new TorrentException(string.Format("Invalid metadata in file {0}, 'name' not of expected type."));
            return name;
        }

        private List<FileEntry> DecodeFiles(BencodedDictionary info, BencodedString name)
        {
            var decodedFiles = new List<FileEntry>();
            if (info.ContainsKey("files")) 
            {
                var files = info["files"] as BencodedList;
                CheckInfoFiles(files);
                foreach (BencodedDictionary file in files)
                {
                    var torrentFile = CreateTorrentFile(file);
                    decodedFiles.Add(torrentFile);
                }
            }
            else 
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
            if (fileLength == null) 
                throw new TorrentException(string.Format("Invalid metadata, 'length' not of expected type."));

        }

        private void CheckFileProperties(string[] filePathList, BencodedInteger fileLength)
        {
            if (filePathList == null || fileLength == null) 
                throw new TorrentException(string.Format("Invalid metadata, 'path'/'length' not of expected type."));

        }

        private void CheckInfoFiles(BencodedList files)
        {
            if (files == null) 
                throw new TorrentException(string.Format("Invalid metadata, 'files' not of expected type."));

        }

        private List<byte[]> GetRawChecksums(BencodedDictionary info, BencodedInteger pieceLength)
        {
            byte[] rawChecksums = Encoding.ASCII.GetBytes(info["pieces"] as BencodedString);
            if (pieceLength == null || rawChecksums == null || rawChecksums.Length % CHECKSUM_SIZE != 0)
                throw new TorrentException(string.Format("Invalid metadata, 'piece length'/'pieces' not of expected type, or invalid length of 'pieces'."));
            var slicedChecksums = rawChecksums.Batch(CHECKSUM_SIZE).Select(e=>e.ToArray());
            return slicedChecksums.ToList();
        }

        private BencodedDictionary GetMetadata()
        {
            var metadata = BencodingParser.Decode(this.data) as BencodedDictionary;
            CheckMetadata(metadata);
            return metadata;
        }

        private void CheckMetadata(BencodedDictionary metadata)
        {
            if (metadata == null) 
                throw new TorrentException(string.Format("Invalid metadata."));
            if (!metadata.ContainsKey("announce")) 
                throw new TorrentException(string.Format("Invalid metadata, 'announce' not found."));
            if (!metadata.ContainsKey("info")) 
                throw new TorrentException(string.Format("Invalid metadata, 'info' not found."));

        }
    }
}

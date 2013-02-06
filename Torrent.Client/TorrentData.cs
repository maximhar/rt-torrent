using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Torrent.Client.Bencoding;
using Torrent.Client.Extensions;

namespace Torrent.Client
{
    /// <summary>
    /// Represents the BitTorrent metadata contained in a .torrent file.
    /// </summary>
    public class TorrentData
    {
        private const int CHECKSUM_SIZE = 20;
        private readonly byte[] data;

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

        /// <summary>
        /// The URL of the announce server.
        /// </summary>
        public string AnnounceURL { get; private set; }

        /// <summary>
        /// The list of announce URLs, if available.
        /// </summary>
        public ReadOnlyCollection<string> AnnounceList { get; private set; }

        public IEnumerable<string> Announces { get; private set; }

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
        public InfoHash InfoHash { get; private set; }

        /// <summary>
        /// Creates a .torrent file with the given data.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="inputFiles">A List<string> containing all the files to be added to the .torrent file.</param>
        /// <param name="filesDir">The root directory of all the files in the .torrent file.</param>
        /// <param name="announce">The announce URL of the tracker, in which the .torrent file will be uploaded to.</param>
        /// <param name="announceList">A list of announce URLs.</param>
        /// <param name="pieceLength">The number of bytes in each piece.</param>
        /// <param name="pieces">A list of strings consisting of the concatenation of all 20-byte SHA1 hash values.</param>
        /// <param name="savePath">A path to save the .torrent file to.</param>
        /// <returns>A string representig the content of the .torrent file.</returns>
        public static string Create(string name, List<FileEntry> inputFiles, string filesDir, string announce,
                                    List<string> announceList, int pieceLength, List<byte[]> pieces, string savePath)
        {
            Contract.Requires(name != null);
            Contract.Requires(inputFiles != null);
            Contract.Requires(inputFiles.Count >= 1);
            Contract.Requires(announce != null);
            Contract.Requires(pieces != null);

            string clientVersion = "0200";
            var res = new BencodedDictionary();
            res.Add("announce", new BencodedString(announce));
            var alist = new BencodedList();
            announceList.ForEach(a => alist.Add(new BencodedList {new BencodedString(a)}));
            res.Add("announce-list", alist);
            res.Add("created by", new BencodedString("rtTorrent/" + clientVersion));
            res.Add("creation date", new BencodedInteger(GetUnixTime()));
            res.Add("encoding", new BencodedString("UTF-8"));

            var info = new BencodedDictionary();

            info.Add("piece length", new BencodedInteger(pieceLength));

            var piecesString = new StringBuilder();
            pieces.ForEach(piece => piece.ForEach(b => piecesString.Append((char) b)));

            info.Add("pieces", new BencodedString(piecesString.ToString()));

            //"1" - the client MUST publish its presence to get other peers ONLY via the trackers explicitly described in the metainfo file
            //"0" - the client may obtain peer from other means, e.g. PEX peer exchange, dht. Here, "private" may be read as "no external peer source".
            //info.Add("private", new BencodedInteger(0));

            if (inputFiles.Count == 1)
            {
                info.Add("name", new BencodedString(name));
                info.Add("length", new BencodedInteger(inputFiles[0].Length));
                //MD5 hash of the file should be added here. The BitTorrent protocol specifies it as NOT needed.
            }
            else
            {
                info.Add("name", new BencodedString(filesDir));
                var files = new BencodedList();

                foreach (FileEntry inputFile in inputFiles)
                {
                    var aFile = new BencodedDictionary();
                    aFile.Add("length", new BencodedInteger(inputFile.Length));

                    var filePath = new BencodedList();
                    inputFile.Name.Split(Path.DirectorySeparatorChar).ForEach(
                        dir => filePath.Add(new BencodedString(dir)));
                    aFile.Add("path", filePath);
                    files.Add(aFile);
                }
                info.Add("files", files);
            }
            res.Add("info", info);
            return res.ToString();
        }

        private static long GetUnixTime()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now - epoch).TotalSeconds);
        }

        private void LoadMetadata()
        {
            BencodedDictionary metadata = GetMetadata();

            var announce = metadata["announce"] as BencodedString;

            var info = metadata["info"] as BencodedDictionary;

            CheckMetadata(announce, info);

            var pieceLength = info["piece length"] as BencodedInteger;

            List<byte[]> checksumList = GetRawChecksums(info, pieceLength);
            BencodedString name = GetNameFromInfo(info);
            List<FileEntry> decodedFiles = DecodeFiles(info, name);
            if (metadata.ContainsKey("announce-list"))
            {
                var announceList = (BencodedList) metadata["announce-list"];
                AnnounceList = DecodeAnnounceList(announceList).AsReadOnly();
            }
            AnnounceURL = announce;
            Checksums = checksumList.AsReadOnly();
            Files = decodedFiles.AsReadOnly();
            PieceLength = pieceLength;
            Name = name;
            InfoHash = ComputeInfoHash(info);
            Announces = CreateAnnouces(AnnounceURL, AnnounceList);
        }

        private IEnumerable<string> CreateAnnouces(string AnnounceURL, ReadOnlyCollection<string> AnnounceList)
        {
            var list = new List<string>();

            if (AnnounceList != null)
                list.AddRange(AnnounceList);

            list.Add(AnnounceURL);
            return list.AsReadOnly();
        }

        private byte[] ComputeInfoHash(BencodedDictionary info)
        {
            SHA1 hasher = SHA1.Create();
            string bencoded = info.ToBencodedString();
            byte[] bytes = bencoded.Select(c => (byte) c).ToArray();
            byte[] hash = hasher.ComputeHash(bytes);
            return hash;
        }

        private List<string> DecodeAnnounceList(BencodedList announceList)
        {
            var list = new List<string>();
            foreach (BencodedList urllist in announceList)
            {
                list.Add((BencodedString) urllist.First());
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
                throw new TorrentException(string.Format("Invalid metadata in file, 'name' not of expected type."));
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
                    FileEntry torrentFile = CreateTorrentFile(file);
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
            string[] filePathList = (file["path"] as BencodedList).Select(s => (string) (s as BencodedString)).ToArray();
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
            if (pieceLength == null || rawChecksums == null || rawChecksums.Length%CHECKSUM_SIZE != 0)
                throw new TorrentException(
                    string.Format(
                        "Invalid metadata, 'piece length'/'pieces' not of expected type, or invalid length of 'pieces'."));
            IEnumerable<byte[]> slicedChecksums = rawChecksums.Batch(CHECKSUM_SIZE).Select(e => e.ToArray());
            return slicedChecksums.ToList();
        }

        private BencodedDictionary GetMetadata()
        {
            var metadata = BencodingParser.Decode(data) as BencodedDictionary;
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
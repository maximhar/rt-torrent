using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Torrent.Client.Bencoding;
using System.Collections.ObjectModel;

namespace Torrent.Client
{
    public class TorrentData
    {
        private const int CHECKSUM_SIZE = 20;

        public string AnnounceURL { get; private set; }
        public int PieceLength { get; private set; }
        public string Name { get; private set; }
        public ReadOnlyCollection<TorrentFile> Files { get; private set; }
        public ReadOnlyCollection<byte[]> Checksums { get; private set; }

        private string path;
        
        public TorrentData(string path)
        {
            this.path = path;
            LoadMetadata();
        }

        private void LoadMetadata()
        {
            var torrentFile = File.OpenRead(path);
            var decoder = new BencodedStreamParser(torrentFile);
            var metadata = decoder.Parse() as BencodedDictionary;
            if (metadata == null) throw new TorrentException(string.Format("Invalid metadata in file {0}.", path));
            if (!metadata.ContainsKey("announce")) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'announce' not found.", path));
            if (!metadata.ContainsKey("info")) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'info' not found.", path));

            var announce = metadata["announce"] as BencodedString;
            var info = metadata["info"] as BencodedDictionary;
            if (announce == null || info == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'announce'/'info' not of expected type.", path));
            var pieceLength = info["piece length"] as BencodedInteger;
            byte[] rawChecksums = Encoding.ASCII.GetBytes(info["pieces"] as BencodedString);
            if (pieceLength == null || rawChecksums == null || rawChecksums.Length % CHECKSUM_SIZE != 0) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'piece length'/'pieces' not of expected type, or invalid length of 'pieces'.", path));
            var checksumList = new List<byte[]>(rawChecksums.Length/CHECKSUM_SIZE);
            int piececount = rawChecksums.Length / CHECKSUM_SIZE;
            for (var pi = 0; pi < rawChecksums.Length-CHECKSUM_SIZE; pi += CHECKSUM_SIZE)
            {
                checksumList.Add(rawChecksums.Skip(pi).Take(CHECKSUM_SIZE).ToArray());
            }
            var name = info["name"] as BencodedString;
            if (name == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'name' not of expected type.", path));
            var decodedFiles = new List<TorrentFile>();
            if (info.ContainsKey("files")) //multiple-file torrent
            {
                var files = info["files"] as BencodedList;  
                if (files == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'files' not of expected type.", path));
                foreach (BencodedDictionary file in files)
                {
                    var filePathList = (file["path"] as BencodedList).Select(s=>(string)(s as BencodedString)).ToArray();
                    var fileLength = file["length"] as BencodedInteger;
                    if (filePathList == null || fileLength == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'path'/'length' not of expected type.", path));
                    string filePath = Path.Combine(filePathList);
                    decodedFiles.Add(new TorrentFile(filePath, fileLength));
                }
            }
            else //single-file torrent, we get the file name from the torrent name
            {
                var fileLength = info["length"] as BencodedInteger;
                if (fileLength == null) throw new TorrentException(string.Format("Invalid metadata in file {0}, 'length' not of expected type.", path));
                decodedFiles.Add(new TorrentFile(name, fileLength));
            }
            this.AnnounceURL = announce;
            this.Checksums = checksumList.AsReadOnly();
            this.Files = decodedFiles.AsReadOnly();
            this.PieceLength = pieceLength;
            this.Name = name;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Torrent.Client
{
    public delegate void PieceReadDelegate(bool success, Piece piece, object state);

    public delegate void PieceWrittenDelegate(bool success, object state);

    public class PieceManager : IDisposable
    {
        public readonly int PieceSize = 1024*16;

        private readonly int blockSize;
        private readonly ConcurrentDictionary<string, FileStream> openStreams;
        private readonly int piecesPerBlock;
        private readonly Cache<PieceReadState> readCache;
        private readonly TorrentData torrentData;
        private readonly Cache<PieceWriteState> writeCache;
        private bool disposed;

        public PieceManager(TorrentData data)
        {
            piecesPerBlock = (int)Math.Ceiling((double)data.PieceLength/PieceSize);
            readCache = new Cache<PieceReadState>();
            writeCache = new Cache<PieceWriteState>();
            openStreams = new ConcurrentDictionary<string, FileStream>();
            torrentData = data;
            blockSize = data.PieceLength;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void AddPiece(Piece piece, PieceWrittenDelegate callback, object state)
        {
            IEnumerable<PieceFileInfo> parts = GetParts(piece.Info.Index, piece.Info.Offset, piece.Data.Length);
            PieceWriteState data = writeCache.Get().Init(callback, piece.Data.Length, piece, state);
            foreach(PieceFileInfo part in parts)
            {
                DiskIO.QueueWrite(part.FileStream, piece.Data, part.FileOffset, part.DataOffset, part.Length,
                                  EndAddPiece, data);
            }
        }

        public void GetPiece(int block, int offset, int length, PieceReadDelegate callback, object state)
        {
            IEnumerable<PieceFileInfo> files = GetParts(block, offset, length);
            var buffer = new byte[length];
            var piece = new Piece(buffer, block, offset, length);
            int partOffset = 0;
            PieceReadState data = readCache.Get().Init(piece, callback, length, state);
            foreach(PieceFileInfo part in files)
            {
                DiskIO.QueueRead(part.FileStream, buffer, 0, part.FileOffset, part.Length, EndGetPiece, data);
                partOffset += (int)part.Length;
            }
        }

        private void EndAddPiece(bool success, int written, object state)
        {
            var data = (PieceWriteState)state;

            if(success)
            {
                data.Remaining -= written;
                if(data.Remaining == 0)
                {
                    data.Callback(true, data.State);
                }
            }
            else data.Callback(false, data.State);

            writeCache.Put(data);
        }

        private void EndGetPiece(bool success, int read, byte[] pieceData, object state)
        {
            var data = (PieceReadState)state; //that should work :D so its test time? no, lets do it for writing too
            if(success)
            {
                data.Remaining -= read;
                if(data.Remaining == 0)
                {
                    data.Callback(true, data.Piece, data.State);
                }
            }
            else data.Callback(false, null, data.State);

            readCache.Put(data);
        }

        private IEnumerable<PieceFileInfo> GetParts(int block, int offset, int length)
        {
            var files = new List<PieceFileInfo>();
            long requestedOffset = Piece.GetAbsoluteAddress(block, offset, blockSize);
            long currentOffset = 0;
            long remaining = length;
            foreach(FileEntry file in torrentData.Files)
            {
                if(remaining <= 0) break;
                if(currentOffset+file.Length >= requestedOffset)
                {
                    long relativePosition = requestedOffset - currentOffset;
                    long partLength = Math.Min(file.Length - relativePosition, remaining);
                    FileStream stream = OpenStreamOrGetFromDictionary(file);

                    files.Add(new PieceFileInfo
                                  {
                                      FileStream = stream,
                                      FileOffset = relativePosition,
                                      Length = partLength,
                                      DataOffset = length - (int)remaining
                                  });
                    Debug.Assert(relativePosition>=0);
                    Debug.Assert(partLength>0);
                    remaining -= partLength;
                    requestedOffset += partLength;
                }
                currentOffset += file.Length;
            }
            return files.ToArray();
        }

        private FileStream OpenStreamOrGetFromDictionary(FileEntry file)
        {
            FileStream stream;
            if(openStreams.TryGetValue(file.Name, out stream)) return stream;
            var dir = Path.GetDirectoryName(file.Name);
            if(dir!=string.Empty && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            stream = File.Open(file.Name, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            openStreams.TryAdd(file.Name, stream);
            return stream;
        }

        private void Dispose(bool disposing)
        {
            if(!disposed)
            {
                if(disposing)
                {
                    if(openStreams != null)
                    {
                        foreach(var stream in openStreams)
                        {
                            if(stream.Value != null)
                            {
                                stream.Value.Dispose();
                            }
                        }
                    }
                }
                disposed = true;
            }
        }

        #region Nested type: PieceFileInfo

        public struct PieceFileInfo
        {
            public FileStream FileStream { get; set; }
            public long FileOffset { get; set; }
            public int DataOffset { get; set; }
            public long Length { get; set; }
        }

        #endregion

        #region Nested type: PieceReadState

        public class PieceReadState : ICacheable
        {
            public Piece Piece { get; private set; }
            public PieceReadDelegate Callback { get; private set; }
            public object State { get; private set; }
            public int Remaining { get; internal set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, null);
            }

            #endregion

            public PieceReadState Init(Piece piece, PieceReadDelegate callback, int remaining, object state)
            {
                Piece = piece;
                Callback = callback;
                State = state;
                Remaining = remaining;
                return this;
            }
        }

        #endregion

        #region Nested type: PieceWriteState

        public class PieceWriteState : ICacheable
        {
            public PieceWrittenDelegate Callback { get; private set; }
            public object State { get; private set; }
            public int Remaining { get; internal set; }
            public Piece Piece { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, 0, null, null);
            }

            #endregion

            public PieceWriteState Init(PieceWrittenDelegate callback, int remaining, Piece piece, object state)
            {
                Callback = callback;
                State = state;
                Remaining = remaining;
                Piece = piece;
                return this;
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Torrent.Client
{
    public delegate void PieceReadDelegate(bool success, Piece piece, object state);

    public delegate void PieceWrittenDelegate(bool success, object state);

    public class PieceManager : IDisposable
    {
        private readonly int blockSize;
        private readonly ConcurrentDictionary<string, FileStream> openStreams;
        private readonly Cache<PieceReadState> readCache;
        private readonly TorrentData torrentData;
        private readonly Cache<PieceWriteState> writeCache;
        private bool disposed;

        public PieceManager(TorrentData data)
        {
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
            Tuple<FileStream, long> file = GetFile((int)piece.Block, (int) piece.Offset);
            long relativeOffset = file.Item2;
            FileStream stream = file.Item1;
            PieceWriteState data = writeCache.Get().Init(callback, state);
            DiskIO.QueueWrite(stream, piece.Data, relativeOffset, piece.Data.Length, EndAddPiece, data);
        }

        private void EndAddPiece(bool success, object state)
        {
            var data = (PieceWriteState) state;

            if (success) data.Callback(true, data.State);
            else data.Callback(false, data.State);

            writeCache.Put(data);
        }

        public void GetPiece(int block, int offset, int length, PieceReadDelegate callback, object state)
        {
            Tuple<FileStream, long> file = GetFile(block, offset);
            long relativePosition = file.Item2;
            FileStream stream = file.Item1;
            var buffer = new byte[length];
            var piece = new Piece(buffer, block, offset);
            PieceReadState data = readCache.Get().Init(piece, callback, state);
            DiskIO.QueueRead(stream, buffer, 0, relativePosition, length, EndGetPiece, data);
        }

        private void EndGetPiece(bool success, byte[] pieceData, object state)
        {
            var data = (PieceReadState) state;

            if (success) data.Callback(true, data.Piece, data.State);
            else data.Callback(false, null, data.State);

            readCache.Put(data);
        }

        private Tuple<FileStream, long> GetFile(int block, int offset)
        {
            long requestedOffset = GenerateAbsolutePieceAddress(block, offset);
            long currentOffset = 0;
            foreach (FileEntry file in torrentData.Files)
            {
                if (requestedOffset >= currentOffset)
                {
                    long relativePosition = requestedOffset - currentOffset;
                    FileStream stream = OpenStreamOrGetFromDictionary(file);
                    return new Tuple<FileStream, long>(stream, relativePosition);
                }
                currentOffset += file.Length;
            }
            return null;
        }

        private FileStream OpenStreamOrGetFromDictionary(FileEntry file)
        {
            FileStream stream;
            if (openStreams.TryGetValue(file.Name, out stream)) return stream;
            return File.Open(file.Name, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        private long GenerateAbsolutePieceAddress(int block, int offset)
        {
            return block*blockSize + offset;
        }

        private Tuple<long, long> GenerateRelativePieceAddress(long address)
        {
            long offset;
            long block = Math.DivRem(address, blockSize, out offset);
            return new Tuple<long, long>(block, offset);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (openStreams != null)
                    {
                        foreach (var stream in openStreams)
                        {
                            if (stream.Value != null)
                            {
                                stream.Value.Dispose();
                            }
                        }
                    }
                }
                disposed = true;
            }
        }

        #region Nested type: PieceReadState

        public class PieceReadState : ICacheable
        {
            public Piece Piece { get; private set; }
            public PieceReadDelegate Callback { get; private set; }
            public object State { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, null);
            }

            #endregion

            public PieceReadState Init(Piece piece, PieceReadDelegate callback, object state)
            {
                Piece = piece;
                Callback = callback;
                State = state;
                return this;
            }
        }

        #endregion

        #region Nested type: PieceWriteState

        public class PieceWriteState : ICacheable
        {
            public PieceWrittenDelegate Callback { get; private set; }
            public object State { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null);
            }

            #endregion

            public PieceWriteState Init(PieceWrittenDelegate callback, object state)
            {
                Callback = callback;
                State = state;
                return this;
            }
        }

        #endregion
    }
}
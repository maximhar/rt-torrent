using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Torrent.Client
{
    public delegate void BlockReadDelegate(bool success, Block block, object state);

    public delegate void BlockWrittenDelegate(bool success, object state);

    public class BlockManager : IDisposable
    {
        public readonly int BlockSize = 1024*16;

        private readonly int pieceSize;
        private readonly ConcurrentDictionary<string, FileStream> openStreams;
        private readonly int blocksPerPiece;
        private readonly Cache<BlockReadState> readCache;
        private readonly TorrentData torrentData;
        private readonly Cache<BlockWriteState> writeCache;
        private readonly string mainDir;
        private bool disposed;
        private int queuedWrites = 0;


        public BlockManager(TorrentData data, string mainDir)
        {
            blocksPerPiece = (int)Math.Ceiling((double)data.PieceLength/BlockSize);
            readCache = new Cache<BlockReadState>();
            writeCache = new Cache<BlockWriteState>();
            openStreams = new ConcurrentDictionary<string, FileStream>();
            torrentData = data;
            pieceSize = data.PieceLength;
            this.mainDir = mainDir;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public void AddBlock(Block block, BlockWrittenDelegate callback, object state)
        {
            //Wait(5000);
            IEnumerable<BlockPartInfo> parts = GetParts(block.Info.Index, block.Info.Offset, block.Info.Length);
            var totalLen = parts.Sum(p => p.Length);
            BlockWriteState data = writeCache.Get().Init(callback, (int)totalLen, block, state);
            Trace.Assert(parts.Any());
            Interlocked.Increment(ref blocksToWrite);
            foreach(BlockPartInfo part in parts)
            {
                DiskIO.QueueWrite(part.FileStream, block.Data, part.FileOffset, part.DataOffset, part.Length,
                                  EndAddBlock, data);
                Interlocked.Increment(ref partsToWrite);
            }
        }

        private int writtenBlocks;
        private int blocksToWrite;
        private int writtenParts;
        private int partsToWrite;

        public void GetBlock(int pieceIndex, int offset, int length, BlockReadDelegate callback, object state)
        {
            IEnumerable<BlockPartInfo> parts = GetParts(pieceIndex, offset, length);
            var buffer = new byte[length];
            var block = new Block(buffer, pieceIndex, offset, length);
            BlockReadState data = readCache.Get().Init(block, callback, length, state);
            foreach(BlockPartInfo part in parts)
            {
                DiskIO.QueueRead(part.FileStream, buffer, 0, part.FileOffset, part.Length, EndGetBlock, data);
            }
        }

        private void EndAddBlock(bool success, int written, object state)
        {
            
            var data = (BlockWriteState)state;
            lock(state)
            if(success)
            {
                Interlocked.Increment(ref writtenParts);
                data.Remaining -= written;
                if(data.Remaining <= 0)
                {
                    data.Callback(true, data.State);
                    Interlocked.Increment(ref writtenBlocks);
                }
            }
            else data.Callback(false, data.State);
            writeCache.Put(data);
        }

        private void EndGetBlock(bool success, int read, byte[] pieceData, object state)
        {
            var data = (BlockReadState)state;
            if(success)
            {
                data.Remaining -= read;
                if(data.Remaining == 0)
                {
                    data.Callback(true, data.Block, data.State);
                }
            }
            else data.Callback(false, null, data.State);

            readCache.Put(data);
        }
        
        private IEnumerable<BlockPartInfo> GetParts(int pieceIndex, int offset, int length)
        {
            var pieces = new List<BlockPartInfo>();
            long requestedOffset = Block.GetAbsoluteAddress(pieceIndex, offset, pieceSize);
            long currentOffset = 0;
            long remaining = length;
            foreach(FileEntry file in torrentData.Files)
            {
                if(remaining <= 0) break;
                if(currentOffset+file.Length >= requestedOffset)
                {
                    long relativePosition = requestedOffset - currentOffset;
                    long partLength = Math.Min(file.Length - relativePosition, remaining);
                    FileStream stream = GetStream(file);

                    pieces.Add(new BlockPartInfo
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
            return pieces.ToArray();
        }

        private FileStream GetStream(FileEntry file)
        {
            string finalPath = Path.Combine(mainDir, file.Name);
            FileStream stream;
            if(openStreams.TryGetValue(finalPath, out stream)) return stream;
            var dir = Path.GetDirectoryName(finalPath);
            if(dir!=string.Empty && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            stream = File.Open(finalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            openStreams.TryAdd(finalPath, stream);
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
                        foreach(var stream in openStreams.Values)
                        {
                            if(stream != null)
                            {
                                stream.Flush();
                                stream.Dispose();
                            }
                        }
                    }
                }
                disposed = true;
            }
        }

        #region Nested type: BlockPartInfo

        public struct BlockPartInfo
        {
            public FileStream FileStream { get; set; }
            public long FileOffset { get; set; }
            public int DataOffset { get; set; }
            public long Length { get; set; }
        }

        #endregion

        #region Nested type: BlockReadState

        public class BlockReadState : ICacheable
        {
            public Block Block { get; private set; }
            public BlockReadDelegate Callback { get; private set; }
            public object State { get; private set; }
            public int Remaining { get; internal set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, null);
            }

            #endregion

            public BlockReadState Init(Block block, BlockReadDelegate callback, int remaining, object state)
            {
                Block = block;
                Callback = callback;
                State = state;
                Remaining = remaining;
                return this;
            }
        }

        #endregion

        #region Nested type: BlockWriteState

        public class BlockWriteState : ICacheable
        {
            public BlockWrittenDelegate Callback { get; private set; }
            public object State { get; private set; }
            public int Remaining { get; internal set; }
            public Block Block { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, 0, null, null);
            }

            #endregion

            public BlockWriteState Init(BlockWrittenDelegate callback, int remaining, Block block, object state)
            {
                Callback = callback;
                State = state;
                Remaining = remaining;
                Block = block;
                return this;
            }
        }

        #endregion

        private void Wait(int count)
        {
            while(queuedWrites > count)
            {
                Thread.Sleep(10);
            }
        }
    }
}
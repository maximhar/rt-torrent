using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;

namespace Torrent.Client
{
    public delegate void DiskIOReadCallback(bool success, int read, byte[] data, object state);

    public delegate void DiskIOWriteCallback(bool success, int written, object state);

    internal static class DiskIO
    {
        private static readonly ConcurrentQueue<DiskIOReadState> readQueue;
        private static readonly ConcurrentQueue<DiskIOWriteState> writeQueue;

        private static readonly Cache<DiskIOReadState> readCache;
        private static readonly Cache<DiskIOWriteState> writeCache;

        private static readonly AutoResetEvent ioHandle;

        static DiskIO()
        {
            readQueue = new ConcurrentQueue<DiskIOReadState>();
            writeQueue = new ConcurrentQueue<DiskIOWriteState>();
            readCache = new Cache<DiskIOReadState>();
            writeCache = new Cache<DiskIOWriteState>();
            ioHandle = new AutoResetEvent(false);
            StartDiskThread();
        }


        public static void QueueRead(Stream stream, byte[] buffer, int bufferOffset, long streamOffset, long length,
                                     DiskIOReadCallback callback, object state)
        {
            DiskIOReadState readData = readCache.Get().Init(stream, buffer, bufferOffset, streamOffset, length, callback,
                                                            state);
            readQueue.Enqueue(readData);
            if (readQueue.Count > 1000) ioHandle.Set();
        }

        public static void QueueWrite(Stream stream, byte[] data, long fileOffset, int dataOffset, long length, DiskIOWriteCallback callback,
                                      object state)
        {
            DiskIOWriteState writeData = writeCache.Get().Init(stream, data, fileOffset, dataOffset, length, callback, state);
            writeQueue.Enqueue(writeData);
            if(writeQueue.Count>1000) ioHandle.Set();
        }

        private static void StartDiskThread()
        {
            var diskThread = new Thread(DiskLoop);
            diskThread.IsBackground = true;
            diskThread.Start();
        }

        private static void DiskLoop()
        {
            while (true)
            {
                try
                {
                    ioHandle.WaitOne(200);
                    bool write = true, read = true;
                    var readList = new List<DiskIOReadState>();
                    var writeList = new List<DiskIOWriteState>();
                    while (write)
                    {
                        DiskIOWriteState result;
                        write = writeQueue.TryDequeue(out result);
                        if(write) writeList.Add(result);
                    }
                    while (read)
                    {
                        DiskIOReadState result;
                        read = readQueue.TryDequeue(out result);
                        if (read) readList.Add(result);
                    }
                    var orderedReads = readList.OrderBy(r => r.Stream.Length).ThenBy(r => r.StreamOffset).ToList();
                    foreach(var readState in orderedReads)
                    {
                        Read(readState);
                    }

                    var orderedWrites = writeList.OrderBy(r => r.Stream.Length).ThenBy(r => r.FileOffset).ToList();
                    foreach (var writeState in orderedWrites)
                    {
                        Write(writeState);
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine("DiskIO", e);
                }
            }
        }

        private static void Write(DiskIOWriteState state)
        {
            try
            {
                state.Stream.Seek(state.FileOffset, SeekOrigin.Begin);
                state.Stream.Write(state.Data, state.DataOffset, (int)state.Length);
                state.Callback(true, (int)state.Length, state.State);
                writeCache.Put(state);
            }
            catch (Exception)
            {
                state.Callback(false, 0, state.State);
                writeCache.Put(state);
            }
        }

        private static void Read(DiskIOReadState state)
        {
            try
            {
                state.Stream.Seek(state.StreamOffset, SeekOrigin.Begin);
                int read = state.Stream.Read(state.Buffer, 0, (int)state.Length);
                if (read != state.Length) state.Callback(false, read, null, state.State);
                else state.Callback(true, read, state.Buffer, state.State);
                readCache.Put(state);
            }
            catch (Exception)
            {
                state.Callback(false, 0, null, state.State);
                readCache.Put(state);
            }
        }

        #region Nested type: DiskIOReadState

        private class DiskIOReadState : ICacheable
        {
            public Stream Stream { get; private set; }
            public byte[] Buffer { get; private set; }
            public int BufferOffset { get; private set; }
            public long StreamOffset { get; private set; }
            public long Length { get; private set; }
            public DiskIOReadCallback Callback { get; private set; }
            public object State { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, 0, 0, null, null);
            }

            #endregion

            public DiskIOReadState Init(Stream stream, byte[] buffer, int bufferOffset, long streamOffset, long length,
                                        DiskIOReadCallback callback, object state)
            {
                Stream = stream;
                BufferOffset = bufferOffset;
                StreamOffset = streamOffset;
                Length = length;
                Callback = callback;
                State = state;
                Buffer = buffer;
                return this;
            }
        }

        #endregion

        #region Nested type: DiskIOWriteState

        private class DiskIOWriteState : ICacheable
        {
            public Stream Stream { get; private set; }
            public byte[] Data { get; private set; }
            public long FileOffset { get; private set; }
            public int DataOffset { get; private set; }
            public long Length { get; private set; }
            public DiskIOWriteCallback Callback { get; private set; }
            public object State { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, 0, 0, null, null);
            }

            #endregion

            public DiskIOWriteState Init(Stream stream, byte[] data, long fileOffset, int dataOffset, long length,
                                         DiskIOWriteCallback callback, object state)
            {
                Stream = stream;
                FileOffset = fileOffset;
                DataOffset = dataOffset;
                Length = length;
                Data = data;
                Callback = callback;
                State = state;
                return this;
            }
        }

        #endregion

        #region Nested type: ReadOperation

        private delegate void ReadOperation(DiskIOReadState state);

        #endregion

        #region Nested type: WriteOperation

        private delegate void WriteOperation(DiskIOWriteState state);

        #endregion
    }
}
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Torrent.Client
{
    public delegate void DiskIOReadCallback(bool success, byte[] data, object state);

    public delegate void DiskIOWriteCallback(bool success, object state);

    internal static class DiskIO
    {
        private static readonly ConcurrentQueue<Action> readQueue;
        private static readonly ConcurrentQueue<Action> writeQueue;

        private static readonly Cache<DiskIOReadState> readCache;
        private static readonly Cache<DiskIOWriteState> writeCache;

        private static readonly AutoResetEvent ioHandle;

        static DiskIO()
        {
            readQueue = new ConcurrentQueue<Action>();
            writeQueue = new ConcurrentQueue<Action>();
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
            readQueue.Enqueue(() => Read(readData));
            ioHandle.Set();
        }

        public static void QueueWrite(Stream stream, byte[] data, long offset, long length, DiskIOWriteCallback callback,
                                      object state)
        {
            DiskIOWriteState writeData = writeCache.Get().Init(stream, data, offset, length, callback, state);
            writeQueue.Enqueue(() => Write(writeData));
            ioHandle.Set();
        }

        private static void StartDiskThread()
        {
            var diskThread = new Thread(DiskLoop);
            diskThread.Start();
        }

        private static void DiskLoop()
        {
            while (true)
            {
                ioHandle.WaitOne();
                bool write = true, read = true;
                while (write || read)
                {
                    Action result;
                    write = writeQueue.TryDequeue(out result);
                    if (write) result();
                    read = readQueue.TryDequeue(out result);
                    if (read) result();
                }
            }
        }

        private static void Write(DiskIOWriteState state)
        {
            try
            {
                state.Stream.Seek(state.Offset, SeekOrigin.Begin);
                state.Stream.Write(state.Data, 0, state.Data.Length);
                state.Callback(true, state.State);
                writeCache.Put(state);
            }
            catch (Exception)
            {
                state.Callback(false, state.State);
                writeCache.Put(state);
            }
        }

        private static void Read(DiskIOReadState state)
        {
            try
            {
                state.Stream.Seek(state.StreamOffset, SeekOrigin.Begin);
                int read = state.Stream.Read(state.Buffer, 0, (int)state.Length);
                if (read != state.Length) state.Callback(false, null, state.State);
                else state.Callback(true, state.Buffer, state.State);
                readCache.Put(state);
            }
            catch (Exception)
            {
                state.Callback(false, null, state.State);
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
            public long Offset { get; private set; }
            public long Length { get; private set; }
            public DiskIOWriteCallback Callback { get; private set; }
            public object State { get; private set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, 0, null, null);
            }

            #endregion

            public DiskIOWriteState Init(Stream stream, byte[] data, long offset, long length,
                                         DiskIOWriteCallback callback, object state)
            {
                Stream = stream;
                Offset = offset;
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
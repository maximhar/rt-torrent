using System.Threading;

namespace Torrent.Client
{
    public class TransferMonitor
    {
        private long bytesReceived;
        private long bytesSent;
        private long bytesRead;
        private long bytesWritten;

        public InfoHash TorrentHash { get; private set; }

        public long BytesReceived
        {
            get { return bytesReceived; }
        }

        public long BytesSent
        {
            get { return bytesSent; }
        }

        public long BytesWritten
        {
            get { return bytesWritten; }
        }

        public long BytesRead
        {
            get { return bytesRead; }
        }

        public TransferMonitor(InfoHash hash)
        {
            TorrentHash = hash;
        }

        public void Received(int count)
        {
            Interlocked.Add(ref bytesReceived, count);
        }

        public void Sent(int count)
        {
            Interlocked.Add(ref bytesSent, count);
        }

        public void Written(int count)
        {
            Interlocked.Add(ref bytesWritten, count);
        }

        public void Read(int count)
        {
            Interlocked.Add(ref bytesRead, count);
        }
    }
}
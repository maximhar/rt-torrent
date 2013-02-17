using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Torrent.Client
{
    /// <summary>
    /// Holds globally available information.
    /// </summary>
    public sealed class Global
    {
        private const int ID_LENGTH = 20;
        private const string ID_HEAD = "-RT1111-";
        private const ushort LISTEN_PORT = 8912;
        private static volatile Global instance = new Global();
        private static readonly object syncRoot = new object();
        private readonly Random random;

        private Global()
        {
            ListeningPort = LISTEN_PORT;
            BlockSize = 1024*16;
            int seed = DateTime.Now.Millisecond + DateTime.Now.Minute + DateTime.Now.Day + ID_HEAD.Length;
            random = new Random(seed);
            PeerId = new string(GeneratePeerId().Select(b => (char) b).ToArray());

            BindSocket();
        }

        public string Version { get { return ID_HEAD.Trim('-').Trim('R').Trim('T'); } }

        public readonly int BlockSize;

        /// <summary>
        /// The current peer ID.
        /// </summary>
        public string PeerId { get; private set; }

        /// <summary>
        /// The port the client listens on;
        /// </summary>
        public ushort ListeningPort { get; private set; }

        public Socket Listener { get; private set; }

        /// <summary>
        /// Holds the single instance of the Torrent.Client.LocalInfo class.
        /// </summary>
        public static Global Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        instance = new Global();
                    }
                }
                return instance;
            }
        }

        public int NextRandom(int max)
        {
            return NextRandom(0, max);
        }

        public int NextRandom(int min, int max)
        {
            lock (random)
            {
                return random.Next(min, max);
            }
        }

        public string FileSizeFormat(long size)
        {
            const int KB = 1024;
            const int MB = 1048576;
            const int GB = 1073741824;

            if (size < KB)
            {
                return string.Format("{0} bytes", size);
            }
            else if (size < MB)
            {
                return string.Format("{0:0.00} KB", ((float)size / KB));
            }
            else if (size < GB)
            {
                return string.Format("{0:0.00} MB", ((float)size / MB));
            }
            else
            {
                return string.Format("{0:0.00} GB", ((float)size / GB));
            }
        }

        private void BindSocket()
        {
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.Bind(new IPEndPoint(IPAddress.Any, LISTEN_PORT));
            Listener.Listen(10);
        }

        private byte[] GeneratePeerId()
        {
            var id = new List<byte>(ID_LENGTH);
            lock (random)
            {
                id.AddRange(Encoding.UTF8.GetBytes(ID_HEAD));
                id.AddRange(Enumerable.Repeat(0, ID_LENGTH - ID_HEAD.Length).Select(i => (byte) NextRandom('0', 'z')));
            }
            return id.ToArray();
        }
    }
}
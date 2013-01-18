using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    /// <summary>
    /// Holds globally available information.
    /// </summary>
    public sealed class LocalInfo
    {
        private const int ID_LENGTH = 20;
        private const string ID_HEAD = "-RT1000-";
        private const ushort LISTEN_PORT = 8912;
        private static volatile LocalInfo instance = new LocalInfo();
        private static object syncRoot = new object();

        /// <summary>
        /// The current peer ID.
        /// </summary>
        public byte[] PeerId { get; private set; }
        /// <summary>
        /// The port the client listens on;
        /// </summary>
        public ushort ListeningPort { get; private set; }

        /// <summary>
        /// Holds the single instance of the Torrent.Client.LocalInfo class.
        /// </summary>
        public static LocalInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        instance = new LocalInfo();
                    }
                }
                return instance;
            }
        }

        private LocalInfo()
        {
            PeerId = GeneratePeerId();
            ListeningPort = LISTEN_PORT;
        }

        private byte[] GeneratePeerId()
        {
            var id = new List<byte>(ID_LENGTH);
            var seed = DateTime.Now.Millisecond + DateTime.Now.Minute + DateTime.Now.Day + ID_HEAD.Length;
            var random = new Random(seed);
            id.AddRange(Encoding.UTF8.GetBytes(ID_HEAD));
            id.AddRange(Enumerable.Repeat(0, ID_LENGTH - ID_HEAD.Length).Select(i => (byte)random.Next(128)));
            return id.ToArray();
        }
    }
}

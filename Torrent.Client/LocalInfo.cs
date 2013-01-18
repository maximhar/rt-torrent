using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client
{
    public sealed class LocalInfo
    {
        private const int ID_LENGTH = 20;
        private const string ID_HEAD = "-RT1000-";

        private static volatile LocalInfo instance = new LocalInfo();
        private static object syncRoot = new object();

        public byte[] PeerId { get; private set; }

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

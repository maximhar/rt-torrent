using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Torrent.Client
{
    public class Cache<T> where T : class, ICacheable, new()
    {
        private ConcurrentQueue<T> Queue;

        public Cache()
        {
            Queue = new ConcurrentQueue<T>();
        }

        public void Put(T item)
        {
            Queue.Enqueue(item);
        }

        public ICacheable Get()
        {
            if (Queue.IsEmpty)
                return new T();
            T item;
            Queue.TryDequeue(out item);
            return item;
        }
    }
}

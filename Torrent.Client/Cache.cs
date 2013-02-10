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

        public T Get()
        {
            T item;
            if (!Queue.TryDequeue(out item))
                item = new T();
            return (T)item.Init();
        }
    }
}

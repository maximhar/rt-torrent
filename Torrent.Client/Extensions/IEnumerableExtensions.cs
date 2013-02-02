using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client.Extensions
{
    public static class EnumerableExtensions
    {
        public static TSource Random<TSource>(this IEnumerable<TSource> source)
        {
            var count = source.Count();
            var random = Global.Instance.NextRandom(count);
            return source.ElementAt(random);
        }
    }
}

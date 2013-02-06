using System.Collections;

namespace Torrent.Client.Extensions
{
    static public class BitArrayExtensions {

        public static bool AllSet(this BitArray source)
        {
            for(int i = 0; i < source.Count; i++)
            {
                if (!source[i]) return false;
            }
            return true;
        }

        public static bool AllUnset(this BitArray source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i]) return false;
            }
            return true;
        }
    }
}
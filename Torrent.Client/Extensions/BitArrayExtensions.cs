using System;
using System.Collections;

namespace Torrent.Client.Extensions
{
    static public class BitArrayExtensions {

        public static void CopyTo(this BitArray source, BitArray destination, int srcOffset, int destOffset, int count)
        {
            if(source == null) throw new ArgumentNullException("source");
            if(destination == null) throw new ArgumentNullException("destination");

            if(source.Count < srcOffset+count)
                throw new ArgumentException();
            if(destination.Count < destOffset+count)
                throw new ArgumentException();

            for(int i=0;i<count;i++)
            {
                destination[destOffset + i] = source[srcOffset + i];
            }
        }

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    public class BencodedInteger:IBencodedElement
    {
        private int innerInteger;

        public BencodedInteger(int value)
        {
            innerInteger = value;
        }

        public override string ToString()
        {
            return innerInteger.ToString();
        }

        public static implicit operator int(BencodedInteger value)
        {
            return value.innerInteger;
        }

        public static implicit operator BencodedInteger(int value)
        {
            return new BencodedInteger(value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    public class BencodedString : IBencodedElement
    {
        private string innerString;

        public BencodedString(string value)
        {
            innerString = value;
        }

        public override string ToString()
        {
            return innerString;
        }

        public static implicit operator string(BencodedString value)
        {
            return value.innerString;
        }
        public static implicit operator BencodedString(string value)
        {
            return new BencodedString(value);
        }
    }
}

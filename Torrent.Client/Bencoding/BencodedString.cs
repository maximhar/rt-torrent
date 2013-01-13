
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    public class BencodedString : IBencodedElement, IEnumerable<char>
    {
        private string innerString;
        public int Length { get { return innerString.Length; } }

        public BencodedString(string value)
        {
            innerString = value;
        }

        public override string ToString()
        {
            return innerString;
        }
        public string ToBencodedString()
        {
            return String.Format("{0}:{1}", innerString.Length, innerString);
        }
        public static implicit operator string(BencodedString value)
        {
            return value.innerString;
        }
        public static implicit operator BencodedString(string value)
        {
            return new BencodedString(value);
        }


        public IEnumerator<char> GetEnumerator()
        {
            return innerString.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return innerString.GetEnumerator();
        }
    }
}


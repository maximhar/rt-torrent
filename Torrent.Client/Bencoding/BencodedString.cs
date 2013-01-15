
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Provides a class for representing Bencoded strings.
    /// </summary>
    public class BencodedString : IBencodedElement, IEnumerable<char>
    {
        private string innerString;
        /// <summary>
        /// Returns the length of the Bencoded string.
        /// </summary>
        public int Length { get { return innerString.Length; } }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.Bencoding.BencodedString class via a string.
        /// </summary>
        /// <param name="value">A string containing the Bencoded data.</param>
        public BencodedString(string value)
        {
            innerString = value;
        }

        /// <summary>
        /// Returns the Bencoded string as a normal string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return innerString;
        }
        /// <summary>
        /// Returns the Bencoded string in Bencoded format.
        /// </summary>
        /// <returns></returns>
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


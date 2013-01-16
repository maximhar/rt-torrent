
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MoreLinq;

namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Provides a class for representing Bencoded lists.
    /// </summary>
    public class BencodedList : IBencodedElement, IEnumerable<IBencodedElement>
    {
        private List<IBencodedElement> innerList;

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.Bencoding.BencodedList class that is empty.
        /// </summary>
        public BencodedList()
        {
            innerList = new List<IBencodedElement>();
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.Bencoding.BencodedList class from a IEnumerable collection of elements.
        /// </summary>
        /// <param name="collection">The IEnumerable collection to add to the Bencoded list.</param>
        public BencodedList(IEnumerable<string> collection)
        {
            collection.ForEach(e => innerList.Add(new BencodedString(e)));
        }

        /// <summary>
        /// Adds an object to the end of the Bencoded list.
        /// </summary>
        /// <param name="value">The Bencoded element to add to the end of the Bencoded list.</param>
        public void Add(IBencodedElement value)
        {
            innerList.Add(value);
        }

        /// <summary>
        /// Removes the first occurence of the specified object from the Bencoded list.
        /// </summary>
        /// <param name="value">The Bencoded element to remove from the Bencoded list.</param>
        /// <returns></returns>
        public bool Remove(IBencodedElement value)
        {
            return innerList.Remove(value);
        }

        public IEnumerator<IBencodedElement> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the content of the Bencoded list.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buff = new StringBuilder();
            buff.Append("list: { ");
            foreach (var el in innerList)
                buff.Append(el + ", ");
            buff.Remove(buff.Length - 2, 2);
            buff.Append(" } ");
            return buff.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        /// <summary>
        /// Returns a Bencoded string that represents the content of the Bencoded list.
        /// </summary>
        /// <returns></returns>
        public string ToBencodedString()
        {
            StringBuilder str = new StringBuilder("l");
            foreach (var item in innerList)
            {
                str.Append(item.ToBencodedString());
            }
            str.Append("e");
            return str.ToString();
        }
    }
}


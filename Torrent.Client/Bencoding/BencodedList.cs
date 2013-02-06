using System.Collections;
using System.Collections.Generic;
using System.Text;
using Torrent.Client.Extensions;

namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Provides a class for representing Bencoded lists.
    /// </summary>
    public class BencodedList : IBencodedElement, IEnumerable<IBencodedElement>
    {
        private readonly List<IBencodedElement> innerList;

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

        #region IBencodedElement Members

        /// <summary>
        /// Returns a Bencoded string that represents the content of the Bencoded list.
        /// </summary>
        /// <returns></returns>
        public string ToBencodedString()
        {
            var str = new StringBuilder("l");
            foreach (IBencodedElement item in innerList)
            {
                str.Append(item.ToBencodedString());
            }
            str.Append("e");
            return str.ToString();
        }

        #endregion

        #region IEnumerable<IBencodedElement> Members

        public IEnumerator<IBencodedElement> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        #endregion

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

        /// <summary>
        /// Returns a string that represents the content of the Bencoded list.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append("list: { ");
            foreach (IBencodedElement el in innerList)
                buff.Append(el + ", ");
            buff.Remove(buff.Length - 2, 2);
            buff.Append(" } ");
            return buff.ToString();
        }
    }
}
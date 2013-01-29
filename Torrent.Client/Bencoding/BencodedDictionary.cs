using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Provides a class for representing Bencoded dictionaries.
    /// </summary>
    public class BencodedDictionary : IBencodedElement, IEnumerable<KeyValuePair<string, IBencodedElement>>
    {
        private readonly Dictionary<string, IBencodedElement> innerDictionary;

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.Bencoding.BencodedDictionary class that is empty.
        /// </summary>
        public BencodedDictionary()
        {
            innerDictionary = new Dictionary<string, IBencodedElement>();
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns></returns>
        public IBencodedElement this[string key]
        {
            get { return innerDictionary[key]; }
            set { innerDictionary[key] = value; }
        }

        #region IBencodedElement Members

        /// <summary>
        /// Returns a Bencoded string that represents the content of the Bencoded dictionary.
        /// </summary>
        /// <returns></returns>
        public string ToBencodedString()
        {
            var str = new StringBuilder("d");
            foreach (var item in innerDictionary)
            {
                str.Append(((BencodedString) item.Key).ToBencodedString()).Append(item.Value.ToBencodedString());
            }
            str.Append("e");
            return str.ToString();
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,IBencodedElement>> Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerDictionary.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, IBencodedElement>> GetEnumerator()
        {
            return innerDictionary.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Adds the specified key and value to the Bencoded dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add. MUST be a string.</param>
        /// <param name="value">The value of the element to add. MUST be of type IBencodedElement</param>
        public void Add(string key, IBencodedElement value)
        {
            innerDictionary.Add(key, value);
        }

        /// <summary>
        /// Removes the value associated with the specified key from the Bencoded dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            return innerDictionary.Remove(key);
        }

        /// <summary>
        /// Determines whether the Bencoded dictionary contains the specified element by key.
        /// </summary>
        /// <param name="key">The key to locate in the Bencoded dictionary.</param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return innerDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns a string that represents the content of the Bencoded dictionary.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append("dictionary: { ");
            foreach (var el in innerDictionary)
                buff.Append(el.Key + "->" + el.Value + ", ");
            buff.Remove(buff.Length - 2, 2);
            buff.Append(" } ");
            return buff.ToString();
        }
    }
}
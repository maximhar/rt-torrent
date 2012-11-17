using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Torrent.Client.Bencoding
{
    public class BencodedDictionary : IBencodedElement, IEnumerable
    {
        private Dictionary<string, IBencodedElement> innerDictionary;

        public BencodedDictionary()
        {
            innerDictionary = new Dictionary<string, IBencodedElement>();
        }

        public void Add(string key, IBencodedElement value)
        {
            innerDictionary.Add(key, value);
        }

        public bool Remove(string key)
        {
            return innerDictionary.Remove(key);
        }

        public bool ContainsKey(string key)
        {
            return innerDictionary.ContainsKey(key);
        }

        public IEnumerator GetEnumerator()
        {
            return innerDictionary.GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder buff = new StringBuilder();
            buff.Append("dictionary: { ");
            foreach (var el in innerDictionary)
                buff.Append(el.Key + "->" + el.Value + ", ");
            buff.Remove(buff.Length - 2, 2);
            buff.Append(" } ");
            return buff.ToString();
        }
    }
}

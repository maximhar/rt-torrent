<<<<<<< HEAD
=======
<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Torrent.Client.Bencoding
{
    public class BencodedList : IBencodedElement, IEnumerable<IBencodedElement>
    {
        private List<IBencodedElement> innerList;

        public BencodedList()
        {
            innerList = new List<IBencodedElement>();
        }

        public void Add(IBencodedElement value)
        {
            innerList.Add(value);
        }

        public bool Remove(IBencodedElement value)
        {
            return innerList.Remove(value);
        }

        public IEnumerator<IBencodedElement> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }
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
    }
}
=======
>>>>>>> temp
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Torrent.Client.Bencoding
{
    public class BencodedList : IBencodedElement, IEnumerable
    {
        private List<IBencodedElement> innerList;

        public BencodedList()
        {
            innerList = new List<IBencodedElement>();
        }

        public void Add(IBencodedElement value)
        {
            innerList.Add(value);
        }

        public bool Remove(IBencodedElement value)
        {
            return innerList.Remove(value);
        }

        public IEnumerator GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

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
<<<<<<< HEAD
=======
>>>>>>> Added ToBencodedString() methods to the 4 types.
>>>>>>> temp

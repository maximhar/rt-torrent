<<<<<<< HEAD
=======
<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    public class BencodedInteger:IBencodedElement
    {
        private long innerInteger;

        public BencodedInteger(long value)
        {
            innerInteger = value;
        }

        public override string ToString()
        {
            return innerInteger.ToString();
        }

        public static implicit operator long(BencodedInteger value)
        {
            return value.innerInteger;
        }
        public static implicit operator int(BencodedInteger value)
        {
            return (int)value.innerInteger;
        }
        public static implicit operator BencodedInteger(long value)
        {
            return new BencodedInteger(value);
        }
        public static implicit operator BencodedInteger(int value)
        {
            return new BencodedInteger(value);
        }
    }
}
=======
>>>>>>> temp
﻿using System;
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

        public string ToBencodedString()
        {
            return String.Format("i{0}e", innerInteger);
        }
    }
}
<<<<<<< HEAD
=======
>>>>>>> Added ToBencodedString() methods to the 4 types.
>>>>>>> temp

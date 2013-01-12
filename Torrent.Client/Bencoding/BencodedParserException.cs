using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    class BencodedParserException : Exception
    {
        public BencodedParserException() : base() { }
        public BencodedParserException(string message) : base(message) { }
        public BencodedParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}

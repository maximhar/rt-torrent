using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    class BencodingParserException : Exception
    {
        public BencodingParserException() : base() { }
        public BencodingParserException(string message) : base(message) { }
        public BencodingParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}

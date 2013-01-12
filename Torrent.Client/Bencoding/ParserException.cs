using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Torrent.Client.Bencoding
{
    class ParserException : Exception
    {
        public ParserException() : base() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}

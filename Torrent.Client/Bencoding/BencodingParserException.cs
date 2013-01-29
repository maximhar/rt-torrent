using System;

namespace Torrent.Client.Bencoding
{
    /// <summary>
    /// Represents an exception raised by the bencoding parser.
    /// </summary>
    internal class BencodingParserException : Exception
    {
        public BencodingParserException()
        {
        }

        public BencodingParserException(string message) : base(message)
        {
        }

        public BencodingParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Torrent.Client.Bencoding
{
    enum BencodedNodeType
    {
        String, 
        Integer,
        List,
        Dictionary
    }

    public class BencodedStreamParser
    {
        private TextReader reader;

        public BencodedStreamParser(TextReader reader)
        {
            this.reader = reader;
        }

        public IBencodedElement Parse()
        {
            return ParseElement();
        }

        private IBencodedElement ParseElement()
        {
            switch(CurrentNodeType())
            {
                case BencodedNodeType.Integer:
                    return ParseInteger();
                case BencodedNodeType.String:
                    return ParseString();
                case BencodedNodeType.List:
                    return ParseList();
                case BencodedNodeType.Dictionary:
                    return ParseDictionary();
                default:
                    throw new ParserException("Unrecognized node type.");
            }
        }

        private BencodedDictionary ParseDictionary()
        {
            char endChar = 'e';
            char beginChar = 'd';
            BencodedDictionary list = new BencodedDictionary();
            if (reader.Peek() != beginChar) throw new ParserException("Expected dictionary.");

            reader.Read();
            while ((char)reader.Peek() != endChar)
            {
                string key = ParseElement() as BencodedString;
                if (key == null) throw new ParserException("Key is expected to be a string.");
                list.Add(key, ParseElement());
                
            }
            reader.Read();
            return list;
        }

        private BencodedList ParseList()
        {
            char endChar = 'e';
            char beginChar = 'l';
            BencodedList list = new BencodedList();
            if (reader.Peek() != beginChar) throw new ParserException("Expected list.");

            reader.Read();
            while ((char)reader.Peek() != endChar)
            {
                list.Add(ParseElement());
            }
            reader.Read();
            return list;
        }

        private BencodedString ParseString()
        {
            char lenEndChar = ':';
            if (!char.IsDigit((char)reader.Peek()))throw new ParserException("Expected to read string length.");
            int length = ReadIntegerValue(lenEndChar);
            char[] result = new char[length];
            int len;
            if((len = reader.Read(result, 0, length)) != length) 
                throw new ParserException(string.Format("Did not read the expected amount of {0} characters, {1} instead.", length, len));
            return new string(result);
        }

        private BencodedInteger ParseInteger()
        {
            char endChar = 'e';
            char beginChar = 'i';
            if (reader.Peek() != beginChar) throw new ParserException("Expected integer.");
            reader.Read();
            int result = ReadIntegerValue(endChar);
            return result;
        }

        private int ReadIntegerValue(char endChar)
        {
            char c;
            int result = 0;
            int negative = 1;
            if ((char)reader.Peek() == '-')
            {
                reader.Read();
                negative = -1;
            }
            while ((c = (char)reader.Read()) != endChar)
            {
                if (!char.IsDigit(c)) throw new ParserException(string.Format("Expected a digit, got '{0}'.", c));
                result *= 10;
                result += ((int)char.GetNumericValue(c));
            }
            return result * negative;
        }

        private BencodedNodeType CurrentNodeType()
        {
            char c;
            switch (c = (char)reader.Peek())
            {
                case 'l':
                    return BencodedNodeType.List;
                case 'd':
                    return BencodedNodeType.Dictionary;
                case 'i':
                    return BencodedNodeType.Integer;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': 
                    return BencodedNodeType.String;
                default:
                    throw new ParserException(string.Format("Node type not recognized: '{0}'.", c));
            }
        }
    }
}

namespace Torrent.Client.Messages
{
    internal class RequestMessage : PeerMessage
    {
        public static readonly int Id = 6;

        public RequestMessage()
        {
            Index = -1;
            Begin = -1;
            Length = -1;
        }

        public RequestMessage(int index, int begin, int length)
        {
            Index = index;
            Begin = begin;
            Length = length;
        }

        public int Index { get; private set; }
        public int Begin { get; private set; }
        public int Length { get; private set; }

        public override int MessageLength
        {
            get { return 17; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            ReadInt(buffer, ref offset);
            ReadByte(buffer, ref offset);
            Index = ReadInt(buffer, ref offset);
            Begin = ReadInt(buffer, ref offset);
            Length = ReadInt(buffer, ref offset);
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 13);
            offset += Write(buffer, offset, (byte) 6);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Begin);
            offset += Write(buffer, offset, Length);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Request message: Index: {0}, Begin: {1}, Length: {2}", Index, Begin, Length);
        }

        public override bool Equals(object obj)
        {
            var msg = obj as RequestMessage;

            if (msg == null)
                return false;
            return Index == msg.Index && Begin == msg.Begin && Length == msg.Length;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^
                   Length.GetHashCode();
        }
    }
}
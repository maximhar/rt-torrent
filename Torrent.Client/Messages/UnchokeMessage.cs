namespace Torrent.Client.Messages
{
    internal class UnchokeMessage : PeerMessage
    {
        public static readonly int Id = 1;

        public override int MessageLength
        {
            get { return 5; }
        }

        public override void FromBytes(byte[] buffer, int offset, int count)
        {
        }

        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, 1);
            offset += Write(buffer, offset, (byte) 1);
            return offset - start;
        }

        public override string ToString()
        {
            return string.Format("Unchoke message");
        }

        public override bool Equals(object obj)
        {
            return obj is UnchokeMessage;
        }

        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode();
        }
    }
}
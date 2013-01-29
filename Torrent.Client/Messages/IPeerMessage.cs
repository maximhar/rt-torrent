namespace Torrent.Client.Messages
{
    internal interface IPeerMessage
    {
        int MessageLength { get; }
        byte[] ToBytes();
        int ToBytes(byte[] buffer, int offset);
        void FromBytes(byte[] buffer, int offset, int count);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    /// <summary>
    /// Provides an abstract base for the message classes, as well as constructor methods.
    /// </summary>
    public abstract class PeerMessage:IPeerMessage
    {
        public abstract int MessageLength { get; }
        public abstract void FromBytes(byte[] buffer, int offset, int count);
        public abstract int ToBytes(byte[] buffer, int offset);
        private static Dictionary<int, Func<PeerMessage>> messages;
        static PeerMessage()
        {
            messages = new Dictionary<int, Func<PeerMessage>>();
            messages.Add(ChokeMessage.Id, () => new ChokeMessage());
            messages.Add(UnchokeMessage.Id, () => new UnchokeMessage());
            messages.Add(InterestedMessage.Id, () => new InterestedMessage());
            messages.Add(NotInterestedMessage.Id, () => new NotInterestedMessage());
            messages.Add(HaveMessage.Id, () => new HaveMessage());
            messages.Add(BitfieldMessage.Id, () => new BitfieldMessage());
            messages.Add(RequestMessage.Id, () => new RequestMessage());
            messages.Add(PieceMessage.Id, () => new PieceMessage());
            messages.Add(CancelMessage.Id, () => new CancelMessage());
            messages.Add(PortMessage.Id, () => new PortMessage());
        }
        public byte[] ToBytes()
        {
            byte[] buffer = new byte[MessageLength];
            ToBytes(buffer, 0);
            return buffer;
        }
        public static PeerMessage CreateFromBytes(byte[] buffer, int offset, int count)
        {
            PeerMessage message;

            int length = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buffer, offset));
            byte firstByte = buffer[offset];

            if (firstByte == 0 && count == 1)
                return new KeepAliveMessage();
            
            if (firstByte == 19 && count == 68)
            {
                message = new HandshakeMessage();
                message.FromBytes(buffer, offset, count);
                return message;
            }

            var id = buffer[offset + 4];
            if (!messages.ContainsKey(id))
                throw new TorrentException("Unknown message.");

            message = messages[id]();
            message.FromBytes(buffer, offset, count);
            return message;
        }

        public bool CompareByteArray(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        #region Read/Write utility methods
        static protected byte ReadByte(byte[] buffer, int offset)
        {
            return buffer[offset];
        }

        static protected byte ReadByte(byte[] buffer, ref int offset)
        {
            byte b = buffer[offset];
            offset++;
            return b;
        }

        static protected byte[] ReadBytes(byte[] buffer, int offset, int count)
        {
            return ReadBytes(buffer, ref offset, count);
        }

        static protected byte[] ReadBytes(byte[] buffer, ref int offset, int count)
        {
            byte[] result = new byte[count];
            Buffer.BlockCopy(buffer, offset, result, 0, count);
            offset += count;
            return result;
        }

        static protected short ReadShort(byte[] buffer, int offset)
        {
            return ReadShort(buffer, ref offset);
        }

        static protected short ReadShort(byte[] buffer, ref int offset)
        {
            short ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, offset));
            offset += 2;
            return ret;
        }

        static protected string ReadString(byte[] buffer, int offset, int count)
        {
            return ReadString(buffer, ref offset, count);
        }

        static protected string ReadString(byte[] buffer, ref int offset, int count)
        {
            string s = System.Text.Encoding.ASCII.GetString(buffer, offset, count);
            offset += count;
            return s;
        }

        static protected int ReadInt(byte[] buffer, int offset)
        {
            return ReadInt(buffer, ref offset);
        }

        static protected int ReadInt(byte[] buffer, ref int offset)
        {
            int ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
            offset += 4;
            return ret;
        }

        static protected long ReadLong(byte[] buffer, int offset)
        {
            return ReadLong(buffer, ref offset);
        }

        static protected long ReadLong(byte[] buffer, ref int offset)
        {
            long ret = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, offset));
            offset += 8;
            return ret;
        }

        static protected int Write(byte[] buffer, int offset, byte value)
        {
            buffer[offset] = value;
            return 1;
        }

        static protected int Write(byte[] dest, int destOffset, byte[] src, int srcOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dest, destOffset, count);
            return count;
        }

        static protected int Write(byte[] buffer, int offset, ushort value)
        {
            return Write(buffer, offset, (short)value);
        }

        static protected int Write(byte[] buffer, int offset, short value)
        {
            offset += Write(buffer, offset, (byte)(value >> 8));
            offset += Write(buffer, offset, (byte)value);
            return 2;
        }

        static protected int Write(byte[] buffer, int offset, int value)
        {
            offset += Write(buffer, offset, (byte)(value >> 24));
            offset += Write(buffer, offset, (byte)(value >> 16));
            offset += Write(buffer, offset, (byte)(value >> 8));
            offset += Write(buffer, offset, (byte)(value));
            return 4;
        }

        static protected int Write(byte[] buffer, int offset, uint value)
        {
            return Write(buffer, offset, (int)value);
        }

        static protected int Write(byte[] buffer, int offset, long value)
        {
            offset += Write(buffer, offset, (int)(value >> 32));
            offset += Write(buffer, offset, (int)value);
            return 8;
        }

        static protected int Write(byte[] buffer, int offset, ulong value)
        {
            return Write(buffer, offset, (long)value);
        }

        static protected int Write(byte[] buffer, int offset, byte[] value)
        {
            return Write(buffer, offset, value, 0, value.Length);
        }

        static protected int WriteAscii(byte[] buffer, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
                Write(buffer, offset + i, (byte)text[i]);
            return text.Length;
        }
        #endregion
    }
}

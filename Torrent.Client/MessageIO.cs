using System;
using System.Net;
using System.Net.Sockets;
using Torrent.Client.Messages;

namespace Torrent.Client
{
    public delegate void MessageSentCallback(bool success, int sent, object state);

    public delegate void MessageReceivedCallback(bool success, PeerMessage message, object state);

    internal static class MessageIO
    {
        private static readonly NetworkCallback EndSendCallback = EndSend;
        private static readonly NetworkCallback EndReceiveCallback = EndReceive;
        private static readonly NetworkCallback EndReceiveLengthCallback = EndReceiveLength;
        private static readonly NetworkCallback EndReceiveHandshakeCallback = EndReceiveHandshake;


        private static readonly Cache<SendMessageState> sendCache = new Cache<SendMessageState>();
        private static readonly Cache<ReceiveMessageState> receiveCache = new Cache<ReceiveMessageState>();


        public static void SendMessage(Socket socket, PeerMessage message, object state, MessageSentCallback callback)
        {
            byte[] buffer = message.ToBytes();
            SendMessageState data = sendCache.Get().Init(socket, buffer, 0, buffer.Length, state, callback);
            SendMessageBase(data);
        }

        public static void ReceiveMessage(Socket socket, object state, MessageReceivedCallback callback)
        {
            ReceiveMessageState data = receiveCache.Get().Init(socket, state, callback);
            ReceiveMessageBase(data);
        }

        public static void ReceiveHandshake(Socket socket, object state, MessageReceivedCallback callback)
        {
            ReceiveMessageState data = receiveCache.Get().Init(socket, state, callback);
            HandshakeMessageBase(data);
        }

        private static void HandshakeMessageBase(ReceiveMessageState data)
        {
            var buffer = new byte[HandshakeMessage.Length];
            data.Buffer = buffer;
            NetworkIO.Receive(data.Socket, data.Buffer, 0, buffer.Length, data, EndReceiveHandshakeCallback);
        }

        private static void ReceiveMessageBase(ReceiveMessageState data)
        {
            var buffer = new byte[4];
            data.Buffer = buffer;
            NetworkIO.Receive(data.Socket, data.Buffer, 0, 4, data, EndReceiveLengthCallback);
        }

        private static void SendMessageBase(SendMessageState data)
        {
            NetworkIO.Send(data.Socket, data.Buffer, data.Offset, data.Count, data, EndSendCallback);
        }

        private static void EndReceiveHandshake(bool success, int read, object state)
        {
            var data = (ReceiveMessageState) state;
            try
            {
                if (!success)
                {
                    data.Callback(false, null, data.State);
                    return;
                }
                PeerMessage message = PeerMessage.CreateFromBytes(data.Buffer, 0, read);
                data.Callback(true, message, data.State);
            }
            finally
            {
                receiveCache.Put(data);
            }
        }

        private static void EndSend(bool success, int sent, object state)
        {
            var data = (SendMessageState) state;
            data.Callback(success, sent, data.State);
            sendCache.Put(data);
        }

        private static void EndReceiveLength(bool success, int read, object state)
        {
            var data = (ReceiveMessageState) state;
            if (success)
            {
                int messageLength = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(data.Buffer, 0));
                if (messageLength == 0)
                {
                    data.Callback(true, new KeepAliveMessage(), data.State);
                    receiveCache.Put(data);
                    return;
                }
                var newBuffer = new byte[read + messageLength];
                BufferCopy(newBuffer, 0, data.Buffer, 0, read);
                data.Buffer = newBuffer;
                NetworkIO.Receive(data.Socket, data.Buffer, read, messageLength, data, EndReceiveCallback);
            }
            else
            {
                data.Callback(false, null, data.State);
                receiveCache.Put(data);
            }
        }


        private static void EndReceive(bool success, int read, object state)
        {
            var data = (ReceiveMessageState) state;
            if (!success)
            {
                data.Callback(false, null, data.State);
                receiveCache.Put(data);
                return;
            }
            PeerMessage message = PeerMessage.CreateFromBytes(data.Buffer, 0, read + 4);
            data.Callback(true, message, data.State);
            receiveCache.Put(data);
        }

        private static void BufferCopy(byte[] destination, int destOffset, byte[] source, int srcOffset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[destOffset + i] = source[srcOffset + i];
            }
        }

        #region Nested type: ReceiveMessageState

        private class ReceiveMessageState : ICacheable
        {
            public Socket Socket { get; internal set; }
            public byte[] Buffer { get; internal set; }
            public object State { get; internal set; }
            public MessageReceivedCallback Callback { get; internal set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, null);
            }

            #endregion

            public ReceiveMessageState Init(Socket socket, object state, MessageReceivedCallback callback)
            {
                Socket = socket;
                State = state;
                Callback = callback;
                return this;
            }
        }

        #endregion

        #region Nested type: SendMessageState

        private class SendMessageState : ICacheable
        {
            public Socket Socket { get; internal set; }
            public byte[] Buffer { get; internal set; }
            public int Offset { get; internal set; }
            public int Count { get; internal set; }
            public object State { get; internal set; }
            public MessageSentCallback Callback { get; internal set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, 0, 0, null, null);
            }

            #endregion

            public SendMessageState Init(Socket socket, byte[] buffer,
                                         int offset, int count, object state, MessageSentCallback callback)
            {
                Socket = socket;
                Buffer = buffer;
                Offset = offset;
                Count = count;
                State = state;
                Callback = callback;
                return this;
            }
        }

        #endregion
    }
}
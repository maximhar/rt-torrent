using System;
using System.Net;
using System.Net.Sockets;

namespace Torrent.Client
{
    public delegate void NetworkCallback(bool success, int transmitted, object state);

    internal static class NetworkIO
    {
        private static readonly AsyncCallback EndReceiveCallback = EndReceive;
        private static readonly AsyncCallback EndSendCallback = EndSend;
        private static readonly AsyncCallback EndConnectCallback = EndConnect;

        private static readonly Cache<NetworkState> cache = new Cache<NetworkState>();

        public static void Receive(Socket socket, byte[] buffer, int offset, int count, object state,
                                   NetworkCallback callback)
        {
            NetworkState data = cache.Get().Init(socket, buffer, offset, count, callback, state);
            ReceiveBase(data);
        }

        public static void Send(Socket socket, byte[] buffer, int offset, int count, object state,
                                NetworkCallback callback)
        {
            NetworkState data = cache.Get().Init(socket, buffer, offset, count, callback, state);
            SendBase(data);
        }

        public static void Connect(Socket socket, IPEndPoint endpoint, object state, NetworkCallback callback)
        {
            NetworkState data = cache.Get().Init(socket, callback, state);
            try
            {
                socket.BeginConnect(endpoint, EndConnectCallback, data);
            }
            catch
            {
                callback(false, 0, state);
                cache.Put(data);
            }
        }

        private static void SendBase(NetworkState data)
        {
            try
            {
                data.Socket.BeginSend(data.Buffer, data.Offset, data.Count, SocketFlags.None, EndSendCallback, data);
            }
            catch
            {
                data.Callback(false, 0, data.State);
                cache.Put(data);
            }
        }

        private static void ReceiveBase(NetworkState data)
        {
            try
            {
                data.Socket.BeginReceive(data.Buffer, data.Offset, data.Remaining, SocketFlags.None, EndReceiveCallback,
                                         data);
            }
            catch
            {
                data.Callback(false, 0, data.State);
                cache.Put(data);
            }
        }

        private static void EndReceive(IAsyncResult ar)
        {
            var data = (NetworkState) ar.AsyncState;
            try
            {
                int count = data.Socket.EndReceive(ar);
                if (count == 0)
                {
                    data.Callback(false, 0, data.State);
                    cache.Put(data);
                }
                else
                {
                    data.Offset += count;
                    data.Remaining -= count;
                    if (data.Remaining == 0)
                    {
                        data.Callback(true, data.Count, data.State);
                        cache.Put(data);
                    }
                    else
                    {
                        ReceiveBase(data);
                    }
                }
            }
            catch
            {
                data.Callback(false, 0, data.State);
                cache.Put(data);
            }
        }

        private static void EndSend(IAsyncResult ar)
        {
            var data = (NetworkState) ar.AsyncState;
            try
            {
                int count = data.Socket.EndSend(ar);
                if (count == 0)
                {
                    data.Callback(false, 0, data.State);
                    cache.Put(data);
                }
                else
                {
                    data.Offset += count;
                    data.Remaining -= count;
                    if (data.Remaining == 0)
                    {
                        data.Callback(true, data.Count, data.State);
                        cache.Put(data);
                    }
                    else
                    {
                        SendBase(data);
                    }
                }
            }
            catch
            {
                data.Callback(false, 0, data.State);
                cache.Put(data);
            }
        }

        private static void EndConnect(IAsyncResult ar)
        {
            var data = (NetworkState) ar.AsyncState;
            try
            {
                data.Socket.EndConnect(ar);
                data.Callback(true, 0, data.State);
            }
            catch (Exception e)
            {
                data.Callback(false, 0, data.State);
            }
            finally
            {
                cache.Put(data);
            }
        }

        #region Nested type: NetworkState

        private class NetworkState : ICacheable
        {
            public Socket Socket { get; internal set; }
            public object State { get; internal set; }
            public byte[] Buffer { get; internal set; }
            public int Offset { get; internal set; }
            public int Remaining { get; internal set; }
            public int Count { get; internal set; }
            public NetworkCallback Callback { get; internal set; }

            #region ICacheable Members

            public ICacheable Init()
            {
                return Init(null, null, null);
            }

            #endregion

            public NetworkState Init(Socket socket, byte[] buffer, int offset, int count, NetworkCallback callback,
                                     object state)
            {
                Socket = socket;
                State = state;
                Offset = offset;
                Remaining = count;
                Count = count;
                Buffer = buffer;
                Callback = callback;
                return this;
            }

            public NetworkState Init(Socket socket, NetworkCallback callback, object state)
            {
                Socket = socket;
                State = state;
                Offset = 0;
                Remaining = 0;
                Count = 0;
                Buffer = null;
                Callback = callback;
                return this;
            }
        }

        #endregion
    }
}
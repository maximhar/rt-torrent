using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    public delegate void NetworkCallback(bool success, int read, object state);
    static class Network
    {
        static AsyncCallback EndReceiveCallback = EndReceive;
        static AsyncCallback EndSendCallback = EndSend;
        static AsyncCallback EndConnectCallback = EndConnect;

        
        static public void Receive(Socket socket, byte[] buffer, int offset, int count, object state, NetworkCallback callback)
        {
            var data = new NetworkState(socket, buffer, offset, count, callback, state);
            ReceiveBase(data);
        }

        static public void Send(Socket socket, byte[] buffer, int offset, int count, object state, NetworkCallback callback)
        {
            var data = new NetworkState(socket, buffer, offset, count, callback, state);
            SendBase(data);
        }

        static public void Connect(Socket socket, IPEndPoint endpoint, object state, NetworkCallback callback)
        {
            try
            {
                var data = new NetworkState(socket, callback, state);
                var task = socket.BeginConnect(endpoint, EndConnectCallback, state);
            }
            catch
            {
                callback(false, 0, state);
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
            }
        }

        private static void ReceiveBase(NetworkState data)
        {
            try
            {
                data.Socket.BeginReceive(data.Buffer, data.Offset, data.Count, SocketFlags.None, EndReceiveCallback, data);
            }
            catch
            {
                data.Callback(false, 0, data.State);
            }
        }

        private static void EndReceive(IAsyncResult ar)
        {
            var data = (NetworkState)ar.AsyncState;
            try
            {
                int count = data.Socket.EndReceive(ar);
                if (count == 0)
                {
                    data.Callback(false, 0, data.State);
                }
                else
                {
                    data.Offset += count;
                    data.Remaining -= count;
                    if (data.Remaining == 0)
                    {
                        data.Callback(true, data.Count, data.State);
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
            }
        }

        private static void EndSend(IAsyncResult ar)
        {
            var data = (NetworkState)ar.AsyncState;
            try
            {
                int count = data.Socket.EndSend(ar);
                if (count == 0)
                {
                    data.Callback(false, 0, data.State);
                }
                else
                {
                    data.Offset += count;
                    data.Remaining -= count;
                    if (data.Remaining == 0)
                    {
                        data.Callback(true, data.Count, data.State);
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
            }
        }

        private static void EndConnect(IAsyncResult ar)
        {
            var data = (NetworkState)ar.AsyncState;
            try
            {
                data.Socket.EndConnect(ar);
                data.Callback(true, 0, data.State);
            }
            catch
            {
                data.Callback(false, 0, data.State);
            }
        }

        class NetworkState:ICacheable
        {
            public Socket Socket { get; internal set; }
            public object State { get; internal set; }
            public byte[] Buffer { get; internal set; }
            public int Offset { get; internal set; }
            public int Remaining { get; internal set; }
            public int Count { get; internal set; }
            public NetworkCallback Callback { get; internal set; }

            public NetworkState(Socket socket, byte[] buffer, int offset, int count, NetworkCallback callback, object state)
            {
                this.Socket = socket;
                this.State = state;
                this.Offset = offset;
                this.Remaining = count;
                this.Count = count;
                this.Buffer = buffer;
                this.Callback = callback;
            }

            public NetworkState(Socket socket, NetworkCallback callback, object state)
            {
                this.Socket = socket;
                this.State = state;
                this.Offset = 0;
                this.Remaining = 0;
                this.Count = 0;
                this.Buffer = null;
                this.Callback = callback;
            }

            public ICacheable Init()
            {
                this.Socket = null;
                this.State = null;
                this.Offset = 0;
                this.Remaining = 0;
                this.Count = 0;
                this.Buffer = null;
                this.Callback = null;
                return this;
            }

            public NetworkState Init(Socket socket, byte[] buffer, int offset, int count, NetworkCallback callback, object state)
            {
                this.Socket = socket;
                this.State = state;
                this.Offset = offset;
                this.Remaining = count;
                this.Count = count;
                this.Buffer = buffer;
                this.Callback = callback;
                return this;
            }

            public ICacheable Init(Socket socket, NetworkCallback callback, object state)
            {
                this.Socket = socket;
                this.State = state;
                this.Offset = 0;
                this.Remaining = 0;
                this.Count = 0;
                this.Buffer = null;
                this.Callback = callback;
                return this;
            }
        }
    }
}

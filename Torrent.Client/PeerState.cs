using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Torrent.Client.Extensions;
namespace Torrent.Client
{
    public class PeerState
    {
        public PeerState(Socket socket, IPEndPoint endpoint)
        {
            Socket = socket;
            EndPoint = endpoint;
            AmChoked = true;
            IsChoked = true;
        }

        public bool AmChoked { get; set; }
        public bool AmInterested { get; set; }
        public bool IsChoked { get; set; }
        public bool IsInterested { get; set; }
        public Socket Socket { get; set; }
        public bool ReceivedHandshake { get; set; }
        public bool SentHandshake { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public string ID { get; set; }
        public BitArray Bitfield { get; set; }
        public int PiecesReceived { get; set; }
        public bool Top { get; set; }
        public DateTime LastReceived { get; set; }
        public int PendingBlocks { get; set; }
        public bool Seeder
        {
            get { return Bitfield.AllSet(); }
        }
        public bool NoBlocks
        {
            get { return Bitfield.AllUnset(); }
        }


        public static PeerState FromSocket(Socket socket, int bitfieldLength)
        {
            return new PeerState(socket, (IPEndPoint)socket.RemoteEndPoint)
                       {
                           Bitfield = new BitArray(bitfieldLength),
                           AmChoked = true,
                           AmInterested = false,
                           IsChoked = true,
                           IsInterested = false
                       };
        }

        public override string ToString()
        {
            return string.Format("{0}, R: {1}, T: {2}, C: {3}", EndPoint, PiecesReceived, Top, AmChoked);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torrent.Client
{
    /// <summary>
    /// Provides a container class for the CancelMessage data for peer communication.
    /// </summary>
    class CancelMessage:PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 8;
        /// <summary>
        /// The zero-based piece index.
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// The zero-based byte offset within the piece.
        /// </summary>
        public int Begin { get; private set; }
        /// <summary>
        /// The requested length.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.CancelMessage class.
        /// </summary>
        public CancelMessage() 
        {
            this.Index = -1;
            this.Begin = -1;
            this.Length = -1;
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.CancelMessage class.
        /// </summary>
        /// <param name="index">The zero-based piece index.</param>
        /// <param name="begin">The zero-based byte offset within the piece.</param>
        /// <param name="length">The requested length.</param>
        public CancelMessage(int index, int begin, int length)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(begin >= 0);
            Contract.Requires(length >= 0);

            this.Index = index;
            this.Begin = begin;
            this.Length = length;
        }

        /// <summary>
        /// Sets the CancelMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            if (count != MessageLength)
                throw new ArgumentException("Invalid message length.");
            this.Index = ReadInt(buffer, ref offset);
            this.Begin = ReadInt(buffer, ref offset);
            this.Length = ReadInt(buffer, ref offset);
        }

        /// <summary>
        /// The length of the CancelMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 17; }
        }

        /// <summary>
        /// Writes the CancelMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            int start = offset;
            offset += Write(buffer, offset, (int)5);
            offset += Write(buffer, offset, (byte)8);
            offset += Write(buffer, offset, Index);
            offset += Write(buffer, offset, Begin);
            offset += Write(buffer, offset, Length);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the CancelMessage object.
        /// </summary>
        /// <returns>The string containing the CancelMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Cancel message: Index: {0}, Begin: {1}, Length: {2}", Index, Begin, Length);
        }

        /// <summary>
        /// Determines wheteher this CancelMessage instance and a specified object, which also must be a CancelMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The CancelMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            CancelMessage msg = obj as CancelMessage;

            if (msg == null)
                return false;
            return this.Index == msg.Index && this.Begin == msg.Begin && this.Length == msg.Length;
        }

        /// <summary>
        /// Returns the hash code for this CancelMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the CancelMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Index.GetHashCode() ^ Begin.GetHashCode() ^ Length.GetHashCode();
        }
    }
}

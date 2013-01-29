using System;
using System.Collections;
using System.Diagnostics.Contracts;

namespace Torrent.Client.Messages
{
    /// <summary>
    /// Provides a container class for the BitfieldMessage data for peer communication.
    /// </summary>
    internal class BitfieldMessage : PeerMessage
    {
        /// <summary>
        /// The ID of the message
        /// </summary>
        public static readonly int Id = 5;

        /// <summary>
        /// Initializes a new empty instance of the Torrent.Client.BitfieldMessage class.
        /// </summary>
        public BitfieldMessage()
        {
            Bitfield = null;
        }

        /// <summary>
        /// Initializes a new instance of the Torrent.Client.BitfieldMessage class.
        /// </summary>
        /// <param name="bitfield">A bitfield representing the pieces that have been successfully downloaded.</param>
        public BitfieldMessage(BitArray bitfield)
        {
            Contract.Requires(bitfield != null);

            Bitfield = bitfield;
        }

        /// <summary>
        /// A bitfield representing the pieces that have been successfully downloaded.
        /// <para>A cleared bit indicated a missing piece, and set bits indicate a valid and available piece.</para>
        /// </summary>
        public BitArray Bitfield { get; private set; }

        /// <summary>
        /// The length of the BitfieldMessage.
        /// </summary>
        public override int MessageLength
        {
            get { return 4 + 1 + Bitfield.Length/8; }
        }

        /// <summary>
        /// Sets the BitfieldMessage properties via a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing the message data.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <param name="count">The length to be read in bytes.</param>
        public override void FromBytes(byte[] buffer, int offset, int count)
        {
            ReadInt(buffer, ref offset);
            ReadByte(buffer, ref offset);
            byte[] bytes = ReadBytes(buffer, ref offset, count - offset);
            Bitfield = new BitArray(bytes);
        }

        /// <summary>
        /// Writes the BitfieldMessage data to a byte array.
        /// </summary>
        /// <param name="buffer">The byte array that the message data will be written to.</param>
        /// <param name="offset">The position in the array at which the message begins.</param>
        /// <returns>An integer representing the the amount of bytes written in the array.</returns>
        public override int ToBytes(byte[] buffer, int offset)
        {
            var byteArray = new byte[(int) Math.Ceiling((double) Bitfield.Length/8)];
            Bitfield.CopyTo(byteArray, 0);
            int start = offset;
            offset += Write(buffer, offset, 5);
            offset += Write(buffer, offset, (byte) 5);
            offset += Write(buffer, offset, byteArray);
            return offset - start;
        }

        /// <summary>
        /// Returns a string that represents the content of the BitfieldMessage object.
        /// </summary>
        /// <returns>The string containing the BitfieldMessage data representation.</returns>
        public override string ToString()
        {
            return string.Format("Bitfield message: Bitfield.Length: {0}", Bitfield.Length);
        }

        /// <summary>
        /// Determines wheteher this BitfieldMessage instance and a specified object, which also must be a BitfieldMessage object, have the same data values.
        /// </summary>
        /// <param name="obj">The BitfieldMessage to compare to this instance.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var msg = obj as BitfieldMessage;

            if (msg == null)
                return false;
            return CompareBitArray(Bitfield, msg.Bitfield);
        }

        /// <summary>
        /// Returns the hash code for this BitfieldMessage instance.
        /// </summary>
        /// <returns>An integer representing the hash code of this instace of the BitfieldMessage class.</returns>
        public override int GetHashCode()
        {
            return MessageLength.GetHashCode() ^ Id.GetHashCode() ^ Bitfield.GetHashCode();
        }
    }
}
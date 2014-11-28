using System;

namespace ZXing.Common
{
    /// <summary>
    ///     <p>
    ///         This provides an easy abstraction to read bits at a time from a sequence of bytes, where the
    ///         number of bits read is not often a multiple of 8.
    ///     </p>
    ///     <p>
    ///         This class is thread-safe but not reentrant. Unless the caller modifies the bytes array
    ///         it passed in, in which case all bets are off.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class BitSource
    {
        private readonly byte[] bytes;
        private int byteOffset;
        private int bitOffset;

        /// <param name="bytes">
        ///     bytes from which this will read bits. Bits will be read from the first byte first.
        ///     Bits are read within a byte from most-significant to least-significant bit.
        /// </param>
        public BitSource(byte[] bytes) { this.bytes = bytes; }

        /// <summary>
        ///     index of next bit in current byte which would be read by the next call to {@link #readBits(int)}.
        /// </summary>
        public int BitOffset { get { return bitOffset; } }

        /// <summary>
        ///     index of next byte in input byte array which would be read by the next call to {@link #readBits(int)}.
        /// </summary>
        public int ByteOffset { get { return byteOffset; } }

        /// <param name="numBits">
        ///     number of bits to read
        /// </param>
        /// <returns>
        ///     int representing the bits read. The bits will appear as the least-significant
        ///     bits of the int
        /// </returns>
        /// <exception cref="ArgumentException">if numBits isn't in [1,32] or more than is available</exception>
        public int readBits(int numBits)
        {
            if (numBits < 1 ||
                numBits > 32 ||
                numBits > available())
                throw new ArgumentException(numBits.ToString(), "numBits");

            var result = 0;

            // First, read remainder from current byte
            if (bitOffset > 0)
            {
                var bitsLeft = 8 - bitOffset;
                var toRead = numBits < bitsLeft ? numBits : bitsLeft;
                var bitsToNotRead = bitsLeft - toRead;
                var mask = (0xFF >> (8 - toRead)) << bitsToNotRead;
                result = (bytes[byteOffset] & mask) >> bitsToNotRead;
                numBits -= toRead;
                bitOffset += toRead;
                if (bitOffset == 8)
                {
                    bitOffset = 0;
                    byteOffset++;
                }
            }

            // Next read whole bytes
            if (numBits > 0)
            {
                while (numBits >= 8)
                {
                    result = (result << 8) | (bytes[byteOffset] & 0xFF);
                    byteOffset++;
                    numBits -= 8;
                }

                // Finally read a partial byte
                if (numBits > 0)
                {
                    var bitsToNotRead = 8 - numBits;
                    var mask = (0xFF >> bitsToNotRead) << bitsToNotRead;
                    result = (result << numBits) | ((bytes[byteOffset] & mask) >> bitsToNotRead);
                    bitOffset += numBits;
                }
            }

            return result;
        }

        /// <returns>
        ///     number of bits that can be read successfully
        /// </returns>
        public int available() { return 8 * (bytes.Length - byteOffset) - bitOffset; }
    }
}

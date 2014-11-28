using System.Text;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     <p>Implements decoding of the EAN-8 format.</p>
    ///     <author>Sean Owen</author>
    /// </summary>
    public sealed class EAN8Reader : UPCEANReader
    {
        private readonly int[] decodeMiddleCounters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EAN8Reader" /> class.
        /// </summary>
        public EAN8Reader() { decodeMiddleCounters = new int[4]; }

        /// <summary>
        ///     Decodes the middle.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="startRange">The start range.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        protected internal override int decodeMiddle(BitArray row,
                                                     int[] startRange,
                                                     StringBuilder result)
        {
            var counters = decodeMiddleCounters;
            counters[0] = 0;
            counters[1] = 0;
            counters[2] = 0;
            counters[3] = 0;
            var end = row.Size;
            var rowOffset = startRange[1];

            for (var x = 0; x < 4 && rowOffset < end; x++)
            {
                int bestMatch;
                if (!decodeDigit(row, counters, rowOffset, L_PATTERNS, out bestMatch))
                    return -1;
                result.Append((char)('0' + bestMatch));
                foreach (var counter in counters)
                    rowOffset += counter;
            }

            var middleRange = findGuardPattern(row, rowOffset, true, MIDDLE_PATTERN);
            if (middleRange == null)
                return -1;
            rowOffset = middleRange[1];

            for (var x = 0; x < 4 && rowOffset < end; x++)
            {
                int bestMatch;
                if (!decodeDigit(row, counters, rowOffset, L_PATTERNS, out bestMatch))
                    return -1;
                result.Append((char)('0' + bestMatch));
                foreach (var counter in counters)
                    rowOffset += counter;
            }

            return rowOffset;
        }

        /// <summary>
        ///     Get the format of this decoder.
        ///     <returns>The 1D format.</returns>
        /// </summary>
        internal override BarcodeFormat BarcodeFormat { get { return BarcodeFormat.EAN_8; } }
    }
}

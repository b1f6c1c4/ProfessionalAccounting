namespace ZXing.PDF417.Internal
{
    /// <summary>
    /// </summary>
    /// <author>Guenther Grau</author>
    /// <author>creatale GmbH (christoph.schulz@creatale.de)</author>
    public static class PDF417CodewordDecoder
    {
        /// <summary>
        ///     The ratios table
        /// </summary>
        private static readonly float[][] RATIOS_TABLE;
                                          // = new float[PDF417Common.SYMBOL_TABLE.Length][PDF417Common.BARS_IN_MODULE];

        /// <summary>
        ///     Initializes the <see cref="ZXing.PDF417.Internal.PDF417CodewordDecoder" /> class & Pre-computes the symbol ratio
        ///     table.
        /// </summary>
        static PDF417CodewordDecoder()
        {
            // Jagged arrays in Java assign the memory automatically, but C# has no equivalent. (Jon Skeet says so!)
            // http://stackoverflow.com/a/5313879/266252
            RATIOS_TABLE = new float[PDF417Common.SYMBOL_TABLE.Length][];
            for (var s = 0; s < RATIOS_TABLE.Length; s++)
                RATIOS_TABLE[s] = new float[PDF417Common.BARS_IN_MODULE];

            // Pre-computes the symbol ratio table.
            for (var i = 0; i < PDF417Common.SYMBOL_TABLE.Length; i++)
            {
                var currentSymbol = PDF417Common.SYMBOL_TABLE[i];
                var currentBit = currentSymbol & 0x1;
                for (var j = 0; j < PDF417Common.BARS_IN_MODULE; j++)
                {
                    var size = 0.0f;
                    while ((currentSymbol & 0x1) == currentBit)
                    {
                        size += 1.0f;
                        currentSymbol >>= 1;
                    }
                    currentBit = currentSymbol & 0x1;
                    RATIOS_TABLE[i][PDF417Common.BARS_IN_MODULE - j - 1] = size / PDF417Common.MODULES_IN_CODEWORD;
                }
            }
        }

        /// <summary>
        ///     Gets the decoded value.
        /// </summary>
        /// <returns>The decoded value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        public static int getDecodedValue(int[] moduleBitCount)
        {
            var decodedValue = getDecodedCodewordValue(sampleBitCounts(moduleBitCount));
            if (decodedValue != PDF417Common.INVALID_CODEWORD)
                return decodedValue;
            return getClosestDecodedValue(moduleBitCount);
        }

        /// <summary>
        ///     Samples the bit counts.
        /// </summary>
        /// <returns>The bit counts.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int[] sampleBitCounts(int[] moduleBitCount)
        {
            float bitCountSum = PDF417Common.getBitCountSum(moduleBitCount);
            var result = new int[PDF417Common.BARS_IN_MODULE];
            var bitCountIndex = 0;
            var sumPreviousBits = 0;
            for (var i = 0; i < PDF417Common.MODULES_IN_CODEWORD; i++)
            {
                var sampleIndex =
                    bitCountSum / (2 * PDF417Common.MODULES_IN_CODEWORD) +
                    (i * bitCountSum) / PDF417Common.MODULES_IN_CODEWORD;
                if (sumPreviousBits + moduleBitCount[bitCountIndex] <= sampleIndex)
                {
                    sumPreviousBits += moduleBitCount[bitCountIndex];
                    bitCountIndex++;
                }
                result[bitCountIndex]++;
            }
            return result;
        }

        /// <summary>
        ///     Gets the decoded codeword value.
        /// </summary>
        /// <returns>The decoded codeword value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int getDecodedCodewordValue(int[] moduleBitCount)
        {
            var decodedValue = getBitValue(moduleBitCount);
            return PDF417Common.getCodeword(decodedValue) == PDF417Common.INVALID_CODEWORD
                       ? PDF417Common.INVALID_CODEWORD
                       : decodedValue;
        }

        /// <summary>
        ///     Gets the bit value.
        /// </summary>
        /// <returns>The bit value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int getBitValue(int[] moduleBitCount)
        {
            ulong result = 0;
            for (ulong i = 0; i < (ulong)moduleBitCount.Length; i++)
                for (var bit = 0; bit < moduleBitCount[i]; bit++)
                    result = (result << 1) | (i % 2ul == 0ul ? 1ul : 0ul);
                        // C# was warning about using the bit-wise 'OR' here with a mix of int/longs.
            return (int)result;
        }

        /// <summary>
        ///     Gets the closest decoded value.
        /// </summary>
        /// <returns>The closest decoded value.</returns>
        /// <param name="moduleBitCount">Module bit count.</param>
        private static int getClosestDecodedValue(int[] moduleBitCount)
        {
            var bitCountSum = PDF417Common.getBitCountSum(moduleBitCount);
            var bitCountRatios = new float[PDF417Common.BARS_IN_MODULE];
            for (var i = 0; i < bitCountRatios.Length; i++)
                bitCountRatios[i] = moduleBitCount[i] / (float)bitCountSum;
            var bestMatchError = float.MaxValue;
            var bestMatch = PDF417Common.INVALID_CODEWORD;
            for (var j = 0; j < RATIOS_TABLE.Length; j++)
            {
                var error = 0.0f;
                var ratioTableRow = RATIOS_TABLE[j];
                for (var k = 0; k < PDF417Common.BARS_IN_MODULE; k++)
                {
                    var diff = ratioTableRow[k] - bitCountRatios[k];
                    error += diff * diff;
                    if (error >= bestMatchError)
                        break;
                }
                if (error < bestMatchError)
                {
                    bestMatchError = error;
                    bestMatch = PDF417Common.SYMBOL_TABLE[j];
                }
            }
            return bestMatch;
        }
    }
}

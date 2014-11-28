using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     @see UPCEANExtension5Support
    /// </summary>
    internal sealed class UPCEANExtension2Support
    {
        private readonly int[] decodeMiddleCounters = new int[4];
        private readonly StringBuilder decodeRowStringBuffer = new StringBuilder();

        internal Result decodeRow(int rowNumber, BitArray row, int[] extensionStartRange)
        {
            var result = decodeRowStringBuffer;
            result.Length = 0;
            var end = decodeMiddle(row, extensionStartRange, result);
            if (end < 0)
                return null;

            var resultString = result.ToString();
            var extensionData = parseExtensionString(resultString);

            var extensionResult =
                new Result(
                    resultString,
                    null,
                    new[]
                        {
                            new ResultPoint((extensionStartRange[0] + extensionStartRange[1]) / 2.0f, rowNumber),
                            new ResultPoint(end, rowNumber)
                        },
                    BarcodeFormat.UPC_EAN_EXTENSION);
            if (extensionData != null)
                extensionResult.putAllMetadata(extensionData);
            return extensionResult;
        }

        private int decodeMiddle(BitArray row, int[] startRange, StringBuilder resultString)
        {
            var counters = decodeMiddleCounters;
            counters[0] = 0;
            counters[1] = 0;
            counters[2] = 0;
            counters[3] = 0;
            var end = row.Size;
            var rowOffset = startRange[1];

            var checkParity = 0;

            for (var x = 0; x < 2 && rowOffset < end; x++)
            {
                int bestMatch;
                if (!UPCEANReader.decodeDigit(row, counters, rowOffset, UPCEANReader.L_AND_G_PATTERNS, out bestMatch))
                    return -1;
                resultString.Append((char)('0' + bestMatch % 10));
                foreach (var counter in counters)
                    rowOffset += counter;
                if (bestMatch >= 10)
                    checkParity |= 1 << (1 - x);
                if (x != 1)
                {
                    // Read off separator if not last
                    rowOffset = row.getNextSet(rowOffset);
                    rowOffset = row.getNextUnset(rowOffset);
                }
            }

            if (resultString.Length != 2)
                return -1;

            if (int.Parse(resultString.ToString()) % 4 != checkParity)
                return -1;

            return rowOffset;
        }

        /// <summary>
        ///     Parses the extension string.
        /// </summary>
        /// <param name="raw">raw content of extension</param>
        /// <returns>formatted interpretation of raw content as a {@link Map} mapping
        private static IDictionary<ResultMetadataType, Object> parseExtensionString(String raw)
        {
            if (raw.Length != 2)
                return null;
            IDictionary<ResultMetadataType, Object> result = new Dictionary<ResultMetadataType, Object>();
            result[ResultMetadataType.ISSUE_NUMBER] = Convert.ToInt32(raw);
            return result;
        }
    }
}

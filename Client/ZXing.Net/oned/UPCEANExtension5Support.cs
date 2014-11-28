using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD
{
    /**
    * @see UPCEANExtension2Support
    */

    internal sealed class UPCEANExtension5Support
    {
        private static readonly int[] CHECK_DIGIT_ENCODINGS =
            {
                0x18, 0x14, 0x12, 0x11, 0x0C, 0x06, 0x03, 0x0A, 0x09, 0x05
            };

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

            var lgPatternFound = 0;

            for (var x = 0; x < 5 && rowOffset < end; x++)
            {
                int bestMatch;
                if (!UPCEANReader.decodeDigit(row, counters, rowOffset, UPCEANReader.L_AND_G_PATTERNS, out bestMatch))
                    return -1;
                resultString.Append((char)('0' + bestMatch % 10));
                foreach (var counter in counters)
                    rowOffset += counter;
                if (bestMatch >= 10)
                    lgPatternFound |= 1 << (4 - x);
                if (x != 4)
                {
                    // Read off separator if not last
                    rowOffset = row.getNextSet(rowOffset);
                    rowOffset = row.getNextUnset(rowOffset);
                }
            }

            if (resultString.Length != 5)
                return -1;

            int checkDigit;
            if (!determineCheckDigit(lgPatternFound, out checkDigit))
                return -1;

            if (extensionChecksum(resultString.ToString()) != checkDigit)
                return -1;

            return rowOffset;
        }

        private static int extensionChecksum(String s)
        {
            var length = s.Length;
            var sum = 0;
            for (var i = length - 2; i >= 0; i -= 2)
                sum += s[i] - '0';
            sum *= 3;
            for (var i = length - 1; i >= 0; i -= 2)
                sum += s[i] - '0';
            sum *= 3;
            return sum % 10;
        }

        private static bool determineCheckDigit(int lgPatternFound, out int checkDigit)
        {
            for (checkDigit = 0; checkDigit < 10; checkDigit++)
                if (lgPatternFound == CHECK_DIGIT_ENCODINGS[checkDigit])
                    return true;
            return false;
        }

        /// <summary>
        ///     Parses the extension string.
        /// </summary>
        /// <param name="raw">raw content of extension</param>
        /// <returns>
        ///     formatted interpretation of raw content as a {@link Map} mapping
        ///     one {@link ResultMetadataType} to appropriate value, or {@code null} if not known
        /// </returns>
        private static IDictionary<ResultMetadataType, Object> parseExtensionString(String raw)
        {
            if (raw.Length != 5)
                return null;
            Object value = parseExtension5String(raw);
            if (value == null)
                return null;
            IDictionary<ResultMetadataType, Object> result = new Dictionary<ResultMetadataType, Object>();
            result[ResultMetadataType.SUGGESTED_PRICE] = value;
            return result;
        }

        private static String parseExtension5String(String raw)
        {
            String currency;
            switch (raw[0])
            {
                case '0':
                    currency = "£";
                    break;
                case '5':
                    currency = "$";
                    break;
                case '9':
                    // Reference: http://www.jollytech.com
                    if ("90000".Equals(raw))
                        // No suggested retail price
                        return null;
                    if ("99991".Equals(raw))
                        // Complementary
                        return "0.00";
                    if ("99990".Equals(raw))
                        return "Used";
                    // Otherwise... unknown currency?
                    currency = "";
                    break;
                default:
                    currency = "";
                    break;
            }
            var rawAmount = int.Parse(raw.Substring(1));
            var unitsString = (rawAmount / 100).ToString();
            var hundredths = rawAmount % 100;
            var hundredthsString = hundredths < 10 ? "0" + hundredths : hundredths.ToString();
            return currency + unitsString + '.' + hundredthsString;
        }
    }
}

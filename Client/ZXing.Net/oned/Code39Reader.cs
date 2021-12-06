using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     <p>Decodes Code 39 barcodes. This does not support "Full ASCII Code 39" yet.</p>
    ///     <author>Sean Owen</author>
    ///     @see Code93Reader
    /// </summary>
    public sealed class Code39Reader : OneDReader
    {
        internal static String ALPHABET_STRING = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. *$/+%";
        private static readonly char[] ALPHABET = ALPHABET_STRING.ToCharArray();

        /// <summary>
        ///     These represent the encodings of characters, as patterns of wide and narrow bars.
        ///     The 9 least-significant bits of each int correspond to the pattern of wide and narrow,
        ///     with 1s representing "wide" and 0s representing narrow.
        /// </summary>
        internal static int[] CHARACTER_ENCODINGS =
            {
                0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064, // 0-9
                0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C, // A-J
                0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016, // K-T
                0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x094, // U-*
                0x0A8, 0x0A2, 0x08A, 0x02A // $-%
            };

        private static readonly int ASTERISK_ENCODING = CHARACTER_ENCODINGS[39];

        private readonly bool usingCheckDigit;
        private readonly bool extendedMode;
        private readonly StringBuilder decodeRowResult;
        private readonly int[] counters;

        /// <summary>
        ///     Creates a reader that assumes all encoded data is data, and does not treat the final
        ///     character as a check digit. It will not decoded "extended Code 39" sequences.
        /// </summary>
        public Code39Reader()
            : this(false) { }

        /// <summary>
        ///     Creates a reader that can be configured to check the last character as a check digit.
        ///     It will not decoded "extended Code 39" sequences.
        /// </summary>
        /// <param name="usingCheckDigit">
        ///     if true, treat the last data character as a check digit, not
        ///     data, and verify that the checksum passes.
        /// </param>
        public Code39Reader(bool usingCheckDigit)
            : this(usingCheckDigit, false) { }

        /// <summary>
        ///     Creates a reader that can be configured to check the last character as a check digit,
        ///     or optionally attempt to decode "extended Code 39" sequences that are used to encode
        ///     the full ASCII character set.
        /// </summary>
        /// <param name="usingCheckDigit">
        ///     if true, treat the last data character as a check digit, not
        ///     data, and verify that the checksum passes.
        /// </param>
        /// <param name="extendedMode">if true, will attempt to decode extended Code 39 sequences in the text.</param>
        public Code39Reader(bool usingCheckDigit, bool extendedMode)
        {
            this.usingCheckDigit = usingCheckDigit;
            this.extendedMode = extendedMode;
            decodeRowResult = new StringBuilder(20);
            counters = new int[9];
        }

        /// <summary>
        ///     <p>
        ///         Attempts to decode a one-dimensional barcode format given a single row of
        ///         an image.
        ///     </p>
        /// </summary>
        /// <param name="rowNumber">row number from top of the row</param>
        /// <param name="row">the black/white pixel data of the row</param>
        /// <param name="hints">decode hints</param>
        /// <returns><see cref="Result" />containing encoded string and start/end of barcode</returns>
        public override Result decodeRow(int rowNumber, BitArray row, IDictionary<DecodeHintType, object> hints)
        {
            for (var index = 0; index < counters.Length; index++)
                counters[index] = 0;
            decodeRowResult.Length = 0;

            var start = findAsteriskPattern(row, counters);
            if (start == null)
                return null;

            // Read off white space    
            var nextStart = row.getNextSet(start[1]);
            var end = row.Size;

            char decodedChar;
            int lastStart;
            do
            {
                if (!recordPattern(row, nextStart, counters))
                    return null;

                var pattern = toNarrowWidePattern(counters);
                if (pattern < 0)
                    return null;
                if (!patternToChar(pattern, out decodedChar))
                    return null;
                decodeRowResult.Append(decodedChar);
                lastStart = nextStart;
                foreach (var counter in counters)
                    nextStart += counter;
                // Read off white space
                nextStart = row.getNextSet(nextStart);
            } while (decodedChar != '*');
            decodeRowResult.Length = decodeRowResult.Length - 1; // remove asterisk

            // Look for whitespace after pattern:
            var lastPatternSize = 0;
            foreach (var counter in counters)
                lastPatternSize += counter;
            var whiteSpaceAfterEnd = nextStart - lastStart - lastPatternSize;
            // If 50% of last pattern size, following last pattern, is not whitespace, fail
            // (but if it's whitespace to the very end of the image, that's OK)
            if (nextStart != end &&
                (whiteSpaceAfterEnd << 1) < lastPatternSize)
                return null;

            // overriding constructor value is possible
            var useCode39CheckDigit = usingCheckDigit;
            if (hints != null &&
                hints.ContainsKey(DecodeHintType.ASSUME_CODE_39_CHECK_DIGIT))
                useCode39CheckDigit = (bool)hints[DecodeHintType.ASSUME_CODE_39_CHECK_DIGIT];

            if (useCode39CheckDigit)
            {
                var max = decodeRowResult.Length - 1;
                var total = 0;
                for (var i = 0; i < max; i++)
                    total += ALPHABET_STRING.IndexOf(decodeRowResult[i]);
                if (decodeRowResult[max] != ALPHABET[total % 43])
                    return null;
                decodeRowResult.Length = max;
            }

            if (decodeRowResult.Length == 0)
                // false positive
                return null;

            // overriding constructor value is possible
            var useCode39ExtendedMode = extendedMode;
            if (hints != null &&
                hints.ContainsKey(DecodeHintType.USE_CODE_39_EXTENDED_MODE))
                useCode39ExtendedMode = (bool)hints[DecodeHintType.USE_CODE_39_EXTENDED_MODE];

            String resultString;
            if (useCode39ExtendedMode)
            {
                resultString = decodeExtended(decodeRowResult.ToString());
                if (resultString == null)
                    if (hints != null &&
                        hints.ContainsKey(DecodeHintType.RELAXED_CODE_39_EXTENDED_MODE) &&
                        Convert.ToBoolean(hints[DecodeHintType.RELAXED_CODE_39_EXTENDED_MODE]))
                        resultString = decodeRowResult.ToString();
                    else
                        return null;
            }
            else
                resultString = decodeRowResult.ToString();

            var left = (start[1] + start[0]) / 2.0f;
            var right = lastStart + lastPatternSize / 2.0f;

            var resultPointCallback = hints == null || !hints.ContainsKey(DecodeHintType.NEED_RESULT_POINT_CALLBACK)
                                          ? null
                                          : (ResultPointCallback)hints[DecodeHintType.NEED_RESULT_POINT_CALLBACK];
            if (resultPointCallback != null)
            {
                resultPointCallback(new ResultPoint(left, rowNumber));
                resultPointCallback(new ResultPoint(right, rowNumber));
            }

            return new Result(
                resultString,
                null,
                new[]
                    {
                        new ResultPoint(left, rowNumber),
                        new ResultPoint(right, rowNumber)
                    },
                BarcodeFormat.CODE_39);
        }

        private static int[] findAsteriskPattern(BitArray row, int[] counters)
        {
            var width = row.Size;
            var rowOffset = row.getNextSet(0);

            var counterPosition = 0;
            var patternStart = rowOffset;
            var isWhite = false;
            var patternLength = counters.Length;

            for (var i = rowOffset; i < width; i++)
                if (row[i] ^ isWhite)
                    counters[counterPosition]++;
                else
                {
                    if (counterPosition == patternLength - 1)
                    {
                        if (toNarrowWidePattern(counters) == ASTERISK_ENCODING)
                            // Look for whitespace before start pattern, >= 50% of width of start pattern
                            if (row.isRange(Math.Max(0, patternStart - ((i - patternStart) >> 1)), patternStart, false))
                                return new[] {patternStart, i};
                        patternStart += counters[0] + counters[1];
                        Array.Copy(counters, 2, counters, 0, patternLength - 2);
                        counters[patternLength - 2] = 0;
                        counters[patternLength - 1] = 0;
                        counterPosition--;
                    }
                    else
                        counterPosition++;
                    counters[counterPosition] = 1;
                    isWhite = !isWhite;
                }
            return null;
        }

        // For efficiency, returns -1 on failure. Not throwing here saved as many as 700 exceptions
        // per image when using some of our blackbox images.
        private static int toNarrowWidePattern(int[] counters)
        {
            var numCounters = counters.Length;
            var maxNarrowCounter = 0;
            int wideCounters;
            do
            {
                var minCounter = Int32.MaxValue;
                foreach (var counter in counters)
                    if (counter < minCounter &&
                        counter > maxNarrowCounter)
                        minCounter = counter;
                maxNarrowCounter = minCounter;
                wideCounters = 0;
                var totalWideCountersWidth = 0;
                var pattern = 0;
                for (var i = 0; i < numCounters; i++)
                {
                    var counter = counters[i];
                    if (counter > maxNarrowCounter)
                    {
                        pattern |= 1 << (numCounters - 1 - i);
                        wideCounters++;
                        totalWideCountersWidth += counter;
                    }
                }
                if (wideCounters == 3)
                {
                    // Found 3 wide counters, but are they close enough in width?
                    // We can perform a cheap, conservative check to see if any individual
                    // counter is more than 1.5 times the average:
                    for (var i = 0; i < numCounters && wideCounters > 0; i++)
                    {
                        var counter = counters[i];
                        if (counter > maxNarrowCounter)
                        {
                            wideCounters--;
                            // totalWideCountersWidth = 3 * average, so this checks if counter >= 3/2 * average
                            if ((counter << 1) >= totalWideCountersWidth)
                                return -1;
                        }
                    }
                    return pattern;
                }
            } while (wideCounters > 3);
            return -1;
        }

        private static bool patternToChar(int pattern, out char c)
        {
            for (var i = 0; i < CHARACTER_ENCODINGS.Length; i++)
                if (CHARACTER_ENCODINGS[i] == pattern)
                {
                    c = ALPHABET[i];
                    return true;
                }
            c = '*';
            return false;
        }

        private static String decodeExtended(String encoded)
        {
            var length = encoded.Length;
            var decoded = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                var c = encoded[i];
                if (c == '+' ||
                    c == '$' ||
                    c == '%' ||
                    c == '/')
                {
                    if (i + 1 >= encoded.Length)
                        return null;

                    var next = encoded[i + 1];
                    var decodedChar = '\0';
                    switch (c)
                    {
                        case '+':
                            // +A to +Z map to a to z
                            if (next >= 'A' &&
                                next <= 'Z')
                                decodedChar = (char)(next + 32);
                            else
                                return null;
                            break;
                        case '$':
                            // $A to $Z map to control codes SH to SB
                            if (next >= 'A' &&
                                next <= 'Z')
                                decodedChar = (char)(next - 64);
                            else
                                return null;
                            break;
                        case '%':
                            // %A to %E map to control codes ESC to US
                            if (next >= 'A' &&
                                next <= 'E')
                                decodedChar = (char)(next - 38);
                            else if (next >= 'F' &&
                                     next <= 'W')
                                decodedChar = (char)(next - 11);
                            else
                                return null;
                            break;
                        case '/':
                            // /A to /O map to ! to , and /Z maps to :
                            if (next >= 'A' &&
                                next <= 'O')
                                decodedChar = (char)(next - 32);
                            else if (next == 'Z')
                                decodedChar = ':';
                            else
                                return null;
                            break;
                    }
                    decoded.Append(decodedChar);
                    // bump up i again since we read two characters
                    i++;
                }
                else
                    decoded.Append(c);
            }
            return decoded.ToString();
        }
    }
}

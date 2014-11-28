using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     <p>Decodes Codabar barcodes.</p>
    ///     <author>Bas Vijfwinkel</author>
    /// </summary>
    public sealed class CodaBarReader : OneDReader
    {
        // These values are critical for determining how permissive the decoding
        // will be. All stripe sizes must be within the window these define, as
        // compared to the average stripe size.
        private static readonly int MAX_ACCEPTABLE = (int)(PATTERN_MATCH_RESULT_SCALE_FACTOR * 2.0f);
        private static readonly int PADDING = (int)(PATTERN_MATCH_RESULT_SCALE_FACTOR * 1.5f);

        private const String ALPHABET_STRING = "0123456789-$:/.+ABCD";
        internal static readonly char[] ALPHABET = ALPHABET_STRING.ToCharArray();

        /**
       * These represent the encodings of characters, as patterns of wide and narrow bars. The 7 least-significant bits of
       * each int correspond to the pattern of wide and narrow, with 1s representing "wide" and 0s representing narrow.
       */

        internal static int[] CHARACTER_ENCODINGS =
            {
                0x003, 0x006, 0x009, 0x060, 0x012, 0x042, 0x021, 0x024, 0x030, 0x048, // 0-9
                0x00c, 0x018, 0x045, 0x051, 0x054, 0x015, 0x01A, 0x029, 0x00B, 0x00E // -$:/.+ABCD
            };

        // minimal number of characters that should be present (inclusing start and stop characters)
        // under normal circumstances this should be set to 3, but can be set higher
        // as a last-ditch attempt to reduce false positives.
        private const int MIN_CHARACTER_LENGTH = 3;

        // official start and end patterns
        private static readonly char[] STARTEND_ENCODING = {'A', 'B', 'C', 'D'};
        // some codabar generator allow the codabar string to be closed by every
        // character. This will cause lots of false positives!

        // some industries use a checksum standard but this is not part of the original codabar standard
        // for more information see : http://www.mecsw.com/specs/codabar.html

        // Keep some instance variables to avoid reallocations
        private readonly StringBuilder decodeRowResult;
        private int[] counters;
        private int counterLength;

        public CodaBarReader()
        {
            decodeRowResult = new StringBuilder(20);
            counters = new int[80];
            counterLength = 0;
        }

        public override Result decodeRow(int rowNumber, BitArray row, IDictionary<DecodeHintType, object> hints)
        {
            for (var index = 0; index < counters.Length; index++)
                counters[index] = 0;

            if (!setCounters(row))
                return null;

            var startOffset = findStartPattern();
            if (startOffset < 0)
                return null;

            var nextStart = startOffset;

            decodeRowResult.Length = 0;
            do
            {
                var charOffset = toNarrowWidePattern(nextStart);
                if (charOffset == -1)
                    return null;
                // Hack: We store the position in the alphabet table into a
                // StringBuilder, so that we can access the decoded patterns in
                // validatePattern. We'll translate to the actual characters later.
                decodeRowResult.Append((char)charOffset);
                nextStart += 8;
                // Stop as soon as we see the end character.
                if (decodeRowResult.Length > 1 &&
                    arrayContains(STARTEND_ENCODING, ALPHABET[charOffset]))
                    break;
            } while (nextStart < counterLength); // no fixed end pattern so keep on reading while data is available

            // Look for whitespace after pattern:
            var trailingWhitespace = counters[nextStart - 1];
            var lastPatternSize = 0;
            for (var i = -8; i < -1; i++)
                lastPatternSize += counters[nextStart + i];

            // We need to see whitespace equal to 50% of the last pattern size,
            // otherwise this is probably a false positive. The exception is if we are
            // at the end of the row. (I.e. the barcode barely fits.)
            if (nextStart < counterLength &&
                trailingWhitespace < lastPatternSize / 2)
                return null;

            if (!validatePattern(startOffset))
                return null;

            // Translate character table offsets to actual characters.
            for (var i = 0; i < decodeRowResult.Length; i++)
                decodeRowResult[i] = ALPHABET[decodeRowResult[i]];
            // Ensure a valid start and end character
            var startchar = decodeRowResult[0];
            if (!arrayContains(STARTEND_ENCODING, startchar))
                return null;
            var endchar = decodeRowResult[decodeRowResult.Length - 1];
            if (!arrayContains(STARTEND_ENCODING, endchar))
                return null;

            // remove stop/start characters character and check if a long enough string is contained
            if (decodeRowResult.Length <= MIN_CHARACTER_LENGTH)
                // Almost surely a false positive ( start + stop + at least 1 character)
                return null;

            if (!SupportClass.GetValue(hints, DecodeHintType.RETURN_CODABAR_START_END, false))
            {
                decodeRowResult.Remove(decodeRowResult.Length - 1, 1);
                decodeRowResult.Remove(0, 1);
            }

            var runningCount = 0;
            for (var i = 0; i < startOffset; i++)
                runningCount += counters[i];
            float left = runningCount;
            for (var i = startOffset; i < nextStart - 1; i++)
                runningCount += counters[i];
            float right = runningCount;

            var resultPointCallback = SupportClass.GetValue(
                                                            hints,
                                                            DecodeHintType.NEED_RESULT_POINT_CALLBACK,
                                                            (ResultPointCallback)null);
            if (resultPointCallback != null)
            {
                resultPointCallback(new ResultPoint(left, rowNumber));
                resultPointCallback(new ResultPoint(right, rowNumber));
            }

            return new Result(
                decodeRowResult.ToString(),
                null,
                new[]
                    {
                        new ResultPoint(left, rowNumber),
                        new ResultPoint(right, rowNumber)
                    },
                BarcodeFormat.CODABAR);
        }

        private bool validatePattern(int start)
        {
            // First, sum up the total size of our four categories of stripe sizes;
            int[] sizes = {0, 0, 0, 0};
            int[] counts = {0, 0, 0, 0};
            var end = decodeRowResult.Length - 1;

            // We break out of this loop in the middle, in order to handle
            // inter-character spaces properly.
            var pos = start;
            for (var i = 0;; i++)
            {
                var pattern = CHARACTER_ENCODINGS[decodeRowResult[i]];
                for (var j = 6; j >= 0; j--)
                {
                    // Even j = bars, while odd j = spaces. Categories 2 and 3 are for
                    // long stripes, while 0 and 1 are for short stripes.
                    var category = (j & 1) + (pattern & 1) * 2;
                    sizes[category] += counters[pos + j];
                    counts[category]++;
                    pattern >>= 1;
                }
                if (i >= end)
                    break;
                // We ignore the inter-character space - it could be of any size.
                pos += 8;
            }

            // Calculate our allowable size thresholds using fixed-point math.
            var maxes = new int[4];
            var mins = new int[4];
            // Define the threshold of acceptability to be the midpoint between the
            // average small stripe and the average large stripe. No stripe lengths
            // should be on the "wrong" side of that line.
            for (var i = 0; i < 2; i++)
            {
                mins[i] = 0; // Accept arbitrarily small "short" stripes.
                mins[i + 2] = ((sizes[i] << INTEGER_MATH_SHIFT) / counts[i] +
                               (sizes[i + 2] << INTEGER_MATH_SHIFT) / counts[i + 2]) >> 1;
                maxes[i] = mins[i + 2];
                maxes[i + 2] = (sizes[i + 2] * MAX_ACCEPTABLE + PADDING) / counts[i + 2];
            }

            // Now verify that all of the stripes are within the thresholds.
            pos = start;
            for (var i = 0;; i++)
            {
                var pattern = CHARACTER_ENCODINGS[decodeRowResult[i]];
                for (var j = 6; j >= 0; j--)
                {
                    // Even j = bars, while odd j = spaces. Categories 2 and 3 are for
                    // long stripes, while 0 and 1 are for short stripes.
                    var category = (j & 1) + (pattern & 1) * 2;
                    var size = counters[pos + j] << INTEGER_MATH_SHIFT;
                    if (size < mins[category] ||
                        size > maxes[category])
                        return false;
                    pattern >>= 1;
                }
                if (i >= end)
                    break;
                pos += 8;
            }

            return true;
        }

        /// <summary>
        ///     Records the size of all runs of white and black pixels, starting with white.
        ///     This is just like recordPattern, except it records all the counters, and
        ///     uses our builtin "counters" member for storage.
        /// </summary>
        /// <param name="row">row to count from</param>
        private bool setCounters(BitArray row)
        {
            counterLength = 0;
            // Start from the first white bit.
            var i = row.getNextUnset(0);
            var end = row.Size;
            if (i >= end)
                return false;
            var isWhite = true;
            var count = 0;
            while (i < end)
            {
                if (row[i] ^ isWhite)
                    // that is, exactly one is true
                    count++;
                else
                {
                    counterAppend(count);
                    count = 1;
                    isWhite = !isWhite;
                }
                i++;
            }
            counterAppend(count);
            return true;
        }

        private void counterAppend(int e)
        {
            counters[counterLength] = e;
            counterLength++;
            if (counterLength >= counters.Length)
            {
                var temp = new int[counterLength * 2];
                Array.Copy(counters, 0, temp, 0, counterLength);
                counters = temp;
            }
        }

        private int findStartPattern()
        {
            for (var i = 1; i < counterLength; i += 2)
            {
                var charOffset = toNarrowWidePattern(i);
                if (charOffset != -1 &&
                    arrayContains(STARTEND_ENCODING, ALPHABET[charOffset]))
                {
                    // Look for whitespace before start pattern, >= 50% of width of start pattern
                    // We make an exception if the whitespace is the first element.
                    var patternSize = 0;
                    for (var j = i; j < i + 7; j++)
                        patternSize += counters[j];
                    if (i == 1 ||
                        counters[i - 1] >= patternSize / 2)
                        return i;
                }
            }
            return -1;
        }

        internal static bool arrayContains(char[] array, char key)
        {
            if (array != null)
                foreach (var c in array)
                    if (c == key)
                        return true;
            return false;
        }

        // Assumes that counters[position] is a bar.
        private int toNarrowWidePattern(int position)
        {
            var end = position + 7;
            if (end >= counterLength)
                return -1;
            var theCounters = counters;

            var maxBar = 0;
            var minBar = Int32.MaxValue;
            for (var j = position; j < end; j += 2)
            {
                var currentCounter = theCounters[j];
                if (currentCounter < minBar)
                    minBar = currentCounter;
                if (currentCounter > maxBar)
                    maxBar = currentCounter;
            }
            var thresholdBar = (minBar + maxBar) / 2;

            var maxSpace = 0;
            var minSpace = Int32.MaxValue;
            for (var j = position + 1; j < end; j += 2)
            {
                var currentCounter = theCounters[j];
                if (currentCounter < minSpace)
                    minSpace = currentCounter;
                if (currentCounter > maxSpace)
                    maxSpace = currentCounter;
            }
            var thresholdSpace = (minSpace + maxSpace) / 2;

            var bitmask = 1 << 7;
            var pattern = 0;
            for (var i = 0; i < 7; i++)
            {
                var threshold = (i & 1) == 0 ? thresholdBar : thresholdSpace;
                bitmask >>= 1;
                if (theCounters[position + i] > threshold)
                    pattern |= bitmask;
            }

            for (var i = 0; i < CHARACTER_ENCODINGS.Length; i++)
                if (CHARACTER_ENCODINGS[i] == pattern)
                    return i;
            return -1;
        }
    }
}

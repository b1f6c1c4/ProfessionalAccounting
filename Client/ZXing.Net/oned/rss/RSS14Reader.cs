using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS
{
    /// <summary>
    ///     Decodes RSS-14, including truncated and stacked variants. See ISO/IEC 24724:2006.
    /// </summary>
    public sealed class RSS14Reader : AbstractRSSReader
    {
        private static readonly int[] OUTSIDE_EVEN_TOTAL_SUBSET = {1, 10, 34, 70, 126};
        private static readonly int[] INSIDE_ODD_TOTAL_SUBSET = {4, 20, 48, 81};
        private static readonly int[] OUTSIDE_GSUM = {0, 161, 961, 2015, 2715};
        private static readonly int[] INSIDE_GSUM = {0, 336, 1036, 1516};
        private static readonly int[] OUTSIDE_ODD_WIDEST = {8, 6, 4, 3, 1};
        private static readonly int[] INSIDE_ODD_WIDEST = {2, 4, 6, 8};

        private static readonly int[][] FINDER_PATTERNS =
            {
                new[] {3, 8, 2, 1},
                new[] {3, 5, 5, 1},
                new[] {3, 3, 7, 1},
                new[] {3, 1, 9, 1},
                new[] {2, 7, 4, 1},
                new[] {2, 5, 6, 1},
                new[] {2, 3, 8, 1},
                new[] {1, 5, 7, 1},
                new[] {1, 3, 9, 1}
            };

        private readonly List<Pair> possibleLeftPairs;
        private readonly List<Pair> possibleRightPairs;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RSS14Reader" /> class.
        /// </summary>
        public RSS14Reader()
        {
            possibleLeftPairs = new List<Pair>();
            possibleRightPairs = new List<Pair>();
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
        /// <returns>
        ///     <see cref="Result" />containing encoded string and start/end of barcode or null, if an error occurs or barcode
        ///     cannot be found
        /// </returns>
        public override Result decodeRow(int rowNumber,
                                         BitArray row,
                                         IDictionary<DecodeHintType, object> hints)
        {
            var leftPair = decodePair(row, false, rowNumber, hints);
            addOrTally(possibleLeftPairs, leftPair);
            row.reverse();
            var rightPair = decodePair(row, true, rowNumber, hints);
            addOrTally(possibleRightPairs, rightPair);
            row.reverse();
            var lefSize = possibleLeftPairs.Count;
            for (var i = 0; i < lefSize; i++)
            {
                var left = possibleLeftPairs[i];
                if (left.Count > 1)
                {
                    var rightSize = possibleRightPairs.Count;
                    for (var j = 0; j < rightSize; j++)
                    {
                        var right = possibleRightPairs[j];
                        if (right.Count > 1)
                            if (checkChecksum(left, right))
                                return constructResult(left, right);
                    }
                }
            }
            return null;
        }

        private static void addOrTally(IList<Pair> possiblePairs, Pair pair)
        {
            if (pair == null)
                return;
            var found = false;
            foreach (var other in possiblePairs)
                if (other.Value == pair.Value)
                {
                    other.incrementCount();
                    found = true;
                    break;
                }
            if (!found)
                possiblePairs.Add(pair);
        }

        /// <summary>
        ///     Resets this instance.
        /// </summary>
        public override void reset()
        {
            possibleLeftPairs.Clear();
            possibleRightPairs.Clear();
        }

        private static Result constructResult(Pair leftPair, Pair rightPair)
        {
            var symbolValue = 4537077L * leftPair.Value + rightPair.Value;
            var text = symbolValue.ToString();

            var buffer = new StringBuilder(14);
            for (var i = 13 - text.Length; i > 0; i--)
                buffer.Append('0');
            buffer.Append(text);

            var checkDigit = 0;
            for (var i = 0; i < 13; i++)
            {
                var digit = buffer[i] - '0';
                checkDigit += (i & 0x01) == 0 ? 3 * digit : digit;
            }
            checkDigit = 10 - (checkDigit % 10);
            if (checkDigit == 10)
                checkDigit = 0;
            buffer.Append(checkDigit);

            var leftPoints = leftPair.FinderPattern.ResultPoints;
            var rightPoints = rightPair.FinderPattern.ResultPoints;
            return new Result(
                buffer.ToString(),
                null,
                new[] {leftPoints[0], leftPoints[1], rightPoints[0], rightPoints[1]},
                BarcodeFormat.RSS_14);
        }

        private static bool checkChecksum(Pair leftPair, Pair rightPair)
        {
            //int leftFPValue = leftPair.FinderPattern.Value;
            //int rightFPValue = rightPair.FinderPattern.Value;
            //if ((leftFPValue == 0 && rightFPValue == 8) ||
            //    (leftFPValue == 8 && rightFPValue == 0))
            //{
            //}
            var checkValue = (leftPair.ChecksumPortion + 16 * rightPair.ChecksumPortion) % 79;
            var targetCheckValue =
                9 * leftPair.FinderPattern.Value + rightPair.FinderPattern.Value;
            if (targetCheckValue > 72)
                targetCheckValue--;
            if (targetCheckValue > 8)
                targetCheckValue--;
            return checkValue == targetCheckValue;
        }

        private Pair decodePair(BitArray row, bool right, int rowNumber, IDictionary<DecodeHintType, object> hints)
        {
            var startEnd = findFinderPattern(row, 0, right);
            if (startEnd == null)
                return null;
            var pattern = parseFoundFinderPattern(row, rowNumber, right, startEnd);
            if (pattern == null)
                return null;

            var resultPointCallback = hints == null || !hints.ContainsKey(DecodeHintType.NEED_RESULT_POINT_CALLBACK)
                                          ? null
                                          : (ResultPointCallback)hints[DecodeHintType.NEED_RESULT_POINT_CALLBACK];

            if (resultPointCallback != null)
            {
                var center = (startEnd[0] + startEnd[1]) / 2.0f;
                if (right)
                    // row is actually reversed
                    center = row.Size - 1 - center;
                resultPointCallback(new ResultPoint(center, rowNumber));
            }

            var outside = decodeDataCharacter(row, pattern, true);
            if (outside == null)
                return null;
            var inside = decodeDataCharacter(row, pattern, false);
            if (inside == null)
                return null;
            return new Pair(
                1597 * outside.Value + inside.Value,
                outside.ChecksumPortion + 4 * inside.ChecksumPortion,
                pattern);
        }

        private DataCharacter decodeDataCharacter(BitArray row, FinderPattern pattern, bool outsideChar)
        {
            var counters = getDataCharacterCounters();
            counters[0] = 0;
            counters[1] = 0;
            counters[2] = 0;
            counters[3] = 0;
            counters[4] = 0;
            counters[5] = 0;
            counters[6] = 0;
            counters[7] = 0;

            if (outsideChar)
                recordPatternInReverse(row, pattern.StartEnd[0], counters);
            else
            {
                recordPattern(row, pattern.StartEnd[1] + 1, counters);
                // reverse it
                for (int i = 0, j = counters.Length - 1; i < j; i++, j--)
                {
                    var temp = counters[i];
                    counters[i] = counters[j];
                    counters[j] = temp;
                }
            }

            var numModules = outsideChar ? 16 : 15;
            var elementWidth = count(counters) / (float)numModules;

            var oddCounts = getOddCounts();
            var evenCounts = getEvenCounts();
            var oddRoundingErrors = getOddRoundingErrors();
            var evenRoundingErrors = getEvenRoundingErrors();

            for (var i = 0; i < counters.Length; i++)
            {
                var value = counters[i] / elementWidth;
                var rounded = (int)(value + 0.5f); // Round
                if (rounded < 1)
                    rounded = 1;
                else if (rounded > 8)
                    rounded = 8;
                var offset = i >> 1;
                if ((i & 0x01) == 0)
                {
                    oddCounts[offset] = rounded;
                    oddRoundingErrors[offset] = value - rounded;
                }
                else
                {
                    evenCounts[offset] = rounded;
                    evenRoundingErrors[offset] = value - rounded;
                }
            }

            if (!adjustOddEvenCounts(outsideChar, numModules))
                return null;

            var oddSum = 0;
            var oddChecksumPortion = 0;
            for (var i = oddCounts.Length - 1; i >= 0; i--)
            {
                oddChecksumPortion *= 9;
                oddChecksumPortion += oddCounts[i];
                oddSum += oddCounts[i];
            }
            var evenChecksumPortion = 0;
            var evenSum = 0;
            for (var i = evenCounts.Length - 1; i >= 0; i--)
            {
                evenChecksumPortion *= 9;
                evenChecksumPortion += evenCounts[i];
                evenSum += evenCounts[i];
            }
            var checksumPortion = oddChecksumPortion + 3 * evenChecksumPortion;

            if (outsideChar)
            {
                if ((oddSum & 0x01) != 0 ||
                    oddSum > 12 ||
                    oddSum < 4)
                    return null;
                var group = (12 - oddSum) / 2;
                var oddWidest = OUTSIDE_ODD_WIDEST[group];
                var evenWidest = 9 - oddWidest;
                var vOdd = RSSUtils.getRSSvalue(oddCounts, oddWidest, false);
                var vEven = RSSUtils.getRSSvalue(evenCounts, evenWidest, true);
                var tEven = OUTSIDE_EVEN_TOTAL_SUBSET[group];
                var gSum = OUTSIDE_GSUM[group];
                return new DataCharacter(vOdd * tEven + vEven + gSum, checksumPortion);
            }
            else
            {
                if ((evenSum & 0x01) != 0 ||
                    evenSum > 10 ||
                    evenSum < 4)
                    return null;
                var group = (10 - evenSum) / 2;
                var oddWidest = INSIDE_ODD_WIDEST[group];
                var evenWidest = 9 - oddWidest;
                var vOdd = RSSUtils.getRSSvalue(oddCounts, oddWidest, true);
                var vEven = RSSUtils.getRSSvalue(evenCounts, evenWidest, false);
                var tOdd = INSIDE_ODD_TOTAL_SUBSET[group];
                var gSum = INSIDE_GSUM[group];
                return new DataCharacter(vEven * tOdd + vOdd + gSum, checksumPortion);
            }
        }

        private int[] findFinderPattern(BitArray row, int rowOffset, bool rightFinderPattern)
        {
            var counters = getDecodeFinderCounters();
            counters[0] = 0;
            counters[1] = 0;
            counters[2] = 0;
            counters[3] = 0;

            var width = row.Size;
            var isWhite = false;
            while (rowOffset < width)
            {
                isWhite = !row[rowOffset];
                if (rightFinderPattern == isWhite)
                    // Will encounter white first when searching for right finder pattern
                    break;
                rowOffset++;
            }

            var counterPosition = 0;
            var patternStart = rowOffset;
            for (var x = rowOffset; x < width; x++)
                if (row[x] ^ isWhite)
                    counters[counterPosition]++;
                else
                {
                    if (counterPosition == 3)
                    {
                        if (isFinderPattern(counters))
                            return new[] {patternStart, x};
                        patternStart += counters[0] + counters[1];
                        counters[0] = counters[2];
                        counters[1] = counters[3];
                        counters[2] = 0;
                        counters[3] = 0;
                        counterPosition--;
                    }
                    else
                        counterPosition++;
                    counters[counterPosition] = 1;
                    isWhite = !isWhite;
                }
            return null;
        }

        private FinderPattern parseFoundFinderPattern(BitArray row, int rowNumber, bool right, int[] startEnd)
        {
            // Actually we found elements 2-5
            var firstIsBlack = row[startEnd[0]];
            var firstElementStart = startEnd[0] - 1;
            // Locate element 1
            while (firstElementStart >= 0 &&
                   firstIsBlack ^ row[firstElementStart])
                firstElementStart--;
            firstElementStart++;
            var firstCounter = startEnd[0] - firstElementStart;
            // Make 'counters' hold 1-4
            var counters = getDecodeFinderCounters();
            Array.Copy(counters, 0, counters, 1, counters.Length - 1);
            counters[0] = firstCounter;
            int value;
            if (!parseFinderValue(counters, FINDER_PATTERNS, out value))
                return null;
            var start = firstElementStart;
            var end = startEnd[1];
            if (right)
            {
                // row is actually reversed
                start = row.Size - 1 - start;
                end = row.Size - 1 - end;
            }
            return new FinderPattern(value, new[] {firstElementStart, startEnd[1]}, start, end, rowNumber);
        }

        private bool adjustOddEvenCounts(bool outsideChar, int numModules)
        {
            var oddSum = count(getOddCounts());
            var evenSum = count(getEvenCounts());
            var mismatch = oddSum + evenSum - numModules;
            var oddParityBad = (oddSum & 0x01) == (outsideChar ? 1 : 0);
            var evenParityBad = (evenSum & 0x01) == 1;

            var incrementOdd = false;
            var decrementOdd = false;
            var incrementEven = false;
            var decrementEven = false;

            if (outsideChar)
            {
                if (oddSum > 12)
                    decrementOdd = true;
                else if (oddSum < 4)
                    incrementOdd = true;
                if (evenSum > 12)
                    decrementEven = true;
                else if (evenSum < 4)
                    incrementEven = true;
            }
            else
            {
                if (oddSum > 11)
                    decrementOdd = true;
                else if (oddSum < 5)
                    incrementOdd = true;
                if (evenSum > 10)
                    decrementEven = true;
                else if (evenSum < 4)
                    incrementEven = true;
            }

            /*if (mismatch == 2) {
           if (!(oddParityBad && evenParityBad)) {
             throw ReaderException.Instance;
           }
           decrementOdd = true;
           decrementEven = true;
         } else if (mismatch == -2) {
           if (!(oddParityBad && evenParityBad)) {
             throw ReaderException.Instance;
           }
           incrementOdd = true;
           incrementEven = true;
         } else */
            if (mismatch == 1)
                if (oddParityBad)
                {
                    if (evenParityBad)
                        return false;
                    decrementOdd = true;
                }
                else
                {
                    if (!evenParityBad)
                        return false;
                    decrementEven = true;
                }
            else if (mismatch == -1)
                if (oddParityBad)
                {
                    if (evenParityBad)
                        return false;
                    incrementOdd = true;
                }
                else
                {
                    if (!evenParityBad)
                        return false;
                    incrementEven = true;
                }
            else if (mismatch == 0)
            {
                if (oddParityBad)
                {
                    if (!evenParityBad)
                        return false;
                    // Both bad
                    if (oddSum < evenSum)
                    {
                        incrementOdd = true;
                        decrementEven = true;
                    }
                    else
                    {
                        decrementOdd = true;
                        incrementEven = true;
                    }
                }
                else if (evenParityBad)
                    return false;
                    // Nothing to do!
            }
            else
                return false;

            if (incrementOdd)
            {
                if (decrementOdd)
                    return false;
                increment(getOddCounts(), getOddRoundingErrors());
            }
            if (decrementOdd)
                decrement(getOddCounts(), getOddRoundingErrors());
            if (incrementEven)
            {
                if (decrementEven)
                    return false;
                increment(getEvenCounts(), getOddRoundingErrors());
            }
            if (decrementEven)
                decrement(getEvenCounts(), getEvenRoundingErrors());

            return true;
        }
    }
}

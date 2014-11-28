using System;
using System.Collections.Generic;
using ZXing.Common;
using ZXing.OneD.RSS.Expanded.Decoders;

namespace ZXing.OneD.RSS.Expanded
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    public sealed class RSSExpandedReader : AbstractRSSReader
    {
        private static readonly int[] SYMBOL_WIDEST = {7, 5, 4, 3, 1};
        private static readonly int[] EVEN_TOTAL_SUBSET = {4, 20, 52, 104, 204};
        private static readonly int[] GSUM = {0, 348, 1388, 2948, 3988};

        private static readonly int[][] FINDER_PATTERNS =
            {
                new[] {1, 8, 4, 1}, // A
                new[] {3, 6, 4, 1}, // B
                new[] {3, 4, 6, 1}, // C
                new[] {3, 2, 8, 1}, // D
                new[] {2, 6, 5, 1}, // E
                new[] {2, 2, 9, 1} // F
            };

        private static readonly int[][] WEIGHTS =
            {
                new[] {1, 3, 9, 27, 81, 32, 96, 77},
                new[] {20, 60, 180, 118, 143, 7, 21, 63},
                new[] {189, 145, 13, 39, 117, 140, 209, 205},
                new[] {193, 157, 49, 147, 19, 57, 171, 91},
                new[] {62, 186, 136, 197, 169, 85, 44, 132},
                new[] {185, 133, 188, 142, 4, 12, 36, 108},
                new[] {113, 128, 173, 97, 80, 29, 87, 50},
                new[] {150, 28, 84, 41, 123, 158, 52, 156},
                new[] {46, 138, 203, 187, 139, 206, 196, 166},
                new[] {76, 17, 51, 153, 37, 111, 122, 155},
                new[] {43, 129, 176, 106, 107, 110, 119, 146},
                new[] {16, 48, 144, 10, 30, 90, 59, 177},
                new[] {109, 116, 137, 200, 178, 112, 125, 164},
                new[] {70, 210, 208, 202, 184, 130, 179, 115},
                new[] {134, 191, 151, 31, 93, 68, 204, 190},
                new[] {148, 22, 66, 198, 172, 94, 71, 2},
                new[] {6, 18, 54, 162, 64, 192, 154, 40},
                new[] {120, 149, 25, 75, 14, 42, 126, 167},
                new[] {79, 26, 78, 23, 69, 207, 199, 175},
                new[] {103, 98, 83, 38, 114, 131, 182, 124},
                new[] {161, 61, 183, 127, 170, 88, 53, 159},
                new[] {55, 165, 73, 8, 24, 72, 5, 15},
                new[] {45, 135, 194, 160, 58, 174, 100, 89}
            };

        private const int FINDER_PAT_A = 0;
        private const int FINDER_PAT_B = 1;
        private const int FINDER_PAT_C = 2;
        private const int FINDER_PAT_D = 3;
        private const int FINDER_PAT_E = 4;
        private const int FINDER_PAT_F = 5;

        private static readonly int[][] FINDER_PATTERN_SEQUENCES =
            {
                new[] {FINDER_PAT_A, FINDER_PAT_A},
                new[] {FINDER_PAT_A, FINDER_PAT_B, FINDER_PAT_B},
                new[] {FINDER_PAT_A, FINDER_PAT_C, FINDER_PAT_B, FINDER_PAT_D},
                new[] {FINDER_PAT_A, FINDER_PAT_E, FINDER_PAT_B, FINDER_PAT_D, FINDER_PAT_C},
                new[] {FINDER_PAT_A, FINDER_PAT_E, FINDER_PAT_B, FINDER_PAT_D, FINDER_PAT_D, FINDER_PAT_F},
                new[] {FINDER_PAT_A, FINDER_PAT_E, FINDER_PAT_B, FINDER_PAT_D, FINDER_PAT_E, FINDER_PAT_F, FINDER_PAT_F},
                new[]
                    {
                        FINDER_PAT_A, FINDER_PAT_A, FINDER_PAT_B, FINDER_PAT_B, FINDER_PAT_C, FINDER_PAT_C, FINDER_PAT_D,
                        FINDER_PAT_D
                    },
                new[]
                    {
                        FINDER_PAT_A, FINDER_PAT_A, FINDER_PAT_B, FINDER_PAT_B, FINDER_PAT_C, FINDER_PAT_C, FINDER_PAT_D,
                        FINDER_PAT_E, FINDER_PAT_E
                    },
                new[]
                    {
                        FINDER_PAT_A, FINDER_PAT_A, FINDER_PAT_B, FINDER_PAT_B, FINDER_PAT_C, FINDER_PAT_C, FINDER_PAT_D,
                        FINDER_PAT_E, FINDER_PAT_F, FINDER_PAT_F
                    },
                new[]
                    {
                        FINDER_PAT_A, FINDER_PAT_A, FINDER_PAT_B, FINDER_PAT_B, FINDER_PAT_C, FINDER_PAT_D, FINDER_PAT_D,
                        FINDER_PAT_E, FINDER_PAT_E, FINDER_PAT_F, FINDER_PAT_F
                    }
            };

        // private static readonly int LONGEST_SEQUENCE_SIZE = FINDER_PATTERN_SEQUENCES[FINDER_PATTERN_SEQUENCES.Length - 1].Length;

        private const int MAX_PAIRS = 11;

        private readonly List<ExpandedPair> pairs = new List<ExpandedPair>(MAX_PAIRS);
        private readonly List<ExpandedRow> rows = new List<ExpandedRow>();
        private readonly int[] startEnd = new int[2];
        //private readonly int[] currentSequence = new int[LONGEST_SEQUENCE_SIZE];
        private bool startFromEven;

        internal List<ExpandedPair> Pairs { get { return pairs; } }

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
            // Rows can start with even pattern in case in prev rows there where odd number of patters.
            // So lets try twice
            pairs.Clear();
            startFromEven = false;
            if (decodeRow2pairs(rowNumber, row))
                return constructResult(pairs);

            pairs.Clear();
            startFromEven = true;
            if (decodeRow2pairs(rowNumber, row))
                return constructResult(pairs);
            return null;
        }

        /// <summary>
        ///     Resets this instance.
        /// </summary>
        public override void reset()
        {
            pairs.Clear();
            rows.Clear();
        }

        // Not private for testing
        internal bool decodeRow2pairs(int rowNumber, BitArray row)
        {
            while (true)
            {
                var nextPair = retrieveNextPair(row, pairs, rowNumber);
                if (nextPair == null)
                    break;
                pairs.Add(nextPair);
                //System.out.println(this.pairs.size()+" pairs found so far on row "+rowNumber+": "+this.pairs);
                // exit this loop when retrieveNextPair() fails and throws
            }
            if (pairs.Count == 0)
                return false;

            // TODO: verify sequence of finder patterns as in checkPairSequence()
            if (checkChecksum())
                return true;

            var tryStackedDecode = rows.Count != 0;
            var wasReversed = false; // TODO: deal with reversed rows
            storeRow(rowNumber, wasReversed);
            if (tryStackedDecode)
            {
                // When the image is 180-rotated, then rows are sorted in wrong dirrection.
                // Try twice with both the directions.
                var ps = checkRows(false);
                if (ps != null)
                    return true;
                ps = checkRows(true);
                if (ps != null)
                    return true;
            }

            return false;
        }

        private List<ExpandedPair> checkRows(bool reverse)
        {
            // Limit number of rows we are checking
            // We use recursive algorithm with pure complexity and don't want it to take forever
            // Stacked barcode can have up to 11 rows, so 25 seems resonable enough
            if (rows.Count > 25)
            {
                rows.Clear(); // We will never have a chance to get result, so clear it
                return null;
            }

            pairs.Clear();
            if (reverse)
                rows.Reverse();

            var ps = checkRows(new List<ExpandedRow>(), 0);

            if (reverse)
                rows.Reverse();

            return ps;
        }

        // Try to construct a valid rows sequence
        // Recursion is used to implement backtracking
        private List<ExpandedPair> checkRows(List<ExpandedRow> collectedRows, int currentRow)
        {
            for (var i = currentRow; i < rows.Count; i++)
            {
                var row = rows[i];
                pairs.Clear();
                var size = collectedRows.Count;
                for (var j = 0; j < size; j++)
                    pairs.AddRange(collectedRows[j].Pairs);
                pairs.AddRange(row.Pairs);

                if (!isValidSequence(pairs))
                    continue;

                if (checkChecksum())
                    return pairs;

                var rs = new List<ExpandedRow>();
                rs.AddRange(collectedRows);
                rs.Add(row);
                // Recursion: try to add more rows
                var result = checkRows(rs, i + 1);
                if (result == null)
                    // We failed, try the next candidate
                    continue;
                return result;
            }

            return null;
        }

        // Whether the pairs form a valid find pattern seqience,
        // either complete or a prefix
        private static bool isValidSequence(List<ExpandedPair> pairs)
        {
            foreach (var sequence in FINDER_PATTERN_SEQUENCES)
            {
                if (pairs.Count > sequence.Length)
                    continue;

                var stop = true;
                for (var j = 0; j < pairs.Count; j++)
                    if (pairs[j].FinderPattern.Value != sequence[j])
                    {
                        stop = false;
                        break;
                    }

                if (stop)
                    return true;
            }

            return false;
        }

        private void storeRow(int rowNumber, bool wasReversed)
        {
            // Discard if duplicate above or below; otherwise insert in order by row number.
            var insertPos = 0;
            var prevIsSame = false;
            var nextIsSame = false;
            while (insertPos < rows.Count)
            {
                var erow = rows[insertPos];
                if (erow.RowNumber > rowNumber)
                {
                    nextIsSame = erow.IsEquivalent(pairs);
                    break;
                }
                prevIsSame = erow.IsEquivalent(pairs);
                insertPos++;
            }
            if (nextIsSame || prevIsSame)
                return;

            // When the row was partially decoded (e.g. 2 pairs found instead of 3),
            // it will prevent us from detecting the barcode.
            // Try to merge partial rows

            // Check whether the row is part of an allready detected row
            if (isPartialRow(pairs, rows))
                return;

            rows.Insert(insertPos, new ExpandedRow(pairs, rowNumber, wasReversed));

            removePartialRows(pairs, rows);
        }

        // Remove all the rows that contains only specified pairs 
        private static void removePartialRows(List<ExpandedPair> pairs, List<ExpandedRow> rows)
        {
            for (var index = 0; index < rows.Count; index++)
            {
                var r = rows[index];
                if (r.Pairs.Count == pairs.Count)
                    continue;
                var allFound = true;
                foreach (var p in r.Pairs)
                {
                    var found = false;
                    foreach (var pp in pairs)
                        if (p.Equals(pp))
                        {
                            found = true;
                            break;
                        }
                    if (!found)
                    {
                        allFound = false;
                        break;
                    }
                }
                if (allFound)
                    // 'pairs' contains all the pairs from the row 'r'
                    rows.RemoveAt(index);
            }
        }

        // Returns true when one of the rows already contains all the pairs
        private static bool isPartialRow(IEnumerable<ExpandedPair> pairs, IEnumerable<ExpandedRow> rows)
        {
            foreach (var r in rows)
            {
                var allFound = true;
                foreach (var p in pairs)
                {
                    var found = false;
                    foreach (var pp in r.Pairs)
                        if (p.Equals(pp))
                        {
                            found = true;
                            break;
                        }
                    if (!found)
                    {
                        allFound = false;
                        break;
                    }
                }
                if (allFound)
                    // the row 'r' contain all the pairs from 'pairs'
                    return true;
            }
            return false;
        }

        // Only used for unit testing
        internal List<ExpandedRow> Rows { get { return rows; } }

        internal static Result constructResult(List<ExpandedPair> pairs)
        {
            var binary = BitArrayBuilder.buildBitArray(pairs);

            var decoder = AbstractExpandedDecoder.createDecoder(binary);
            var resultingString = decoder.parseInformation();
            if (resultingString == null)
                return null;

            var firstPoints = pairs[0].FinderPattern.ResultPoints;
            var lastPoints = pairs[pairs.Count - 1].FinderPattern.ResultPoints;

            return new Result(
                resultingString,
                null,
                new[] {firstPoints[0], firstPoints[1], lastPoints[0], lastPoints[1]},
                BarcodeFormat.RSS_EXPANDED
                );
        }

        private bool checkChecksum()
        {
            var firstPair = pairs[0];
            var checkCharacter = firstPair.LeftChar;
            var firstCharacter = firstPair.RightChar;

            if (firstCharacter == null)
                return false;

            var checksum = firstCharacter.ChecksumPortion;
            var s = 2;

            for (var i = 1; i < pairs.Count; ++i)
            {
                var currentPair = pairs[i];
                checksum += currentPair.LeftChar.ChecksumPortion;
                s++;
                var currentRightChar = currentPair.RightChar;
                if (currentRightChar != null)
                {
                    checksum += currentRightChar.ChecksumPortion;
                    s++;
                }
            }

            checksum %= 211;

            var checkCharacterValue = 211 * (s - 4) + checksum;

            return checkCharacterValue == checkCharacter.Value;
        }

        private static int getNextSecondBar(BitArray row, int initialPos)
        {
            int currentPos;
            if (row[initialPos])
            {
                currentPos = row.getNextUnset(initialPos);
                currentPos = row.getNextSet(currentPos);
            }
            else
            {
                currentPos = row.getNextSet(initialPos);
                currentPos = row.getNextUnset(currentPos);
            }
            return currentPos;
        }

        // not private for testing
        internal ExpandedPair retrieveNextPair(BitArray row, List<ExpandedPair> previousPairs, int rowNumber)
        {
            var isOddPattern = previousPairs.Count % 2 == 0;
            if (startFromEven)
                isOddPattern = !isOddPattern;

            FinderPattern pattern;

            var keepFinding = true;
            var forcedOffset = -1;
            do
            {
                if (!findNextPair(row, previousPairs, forcedOffset))
                    return null;
                pattern = parseFoundFinderPattern(row, rowNumber, isOddPattern);
                if (pattern == null)
                    forcedOffset = getNextSecondBar(row, startEnd[0]);
                else
                    keepFinding = false;
            } while (keepFinding);

            // When stacked symbol is split over multiple rows, there's no way to guess if this pair can be last or not.
            // bool mayBeLast;
            // if (!checkPairSequence(previousPairs, pattern, out mayBeLast))
            //   return null;

            var leftChar = decodeDataCharacter(row, pattern, isOddPattern, true);
            if (leftChar == null)
                return null;

            if (previousPairs.Count != 0 &&
                previousPairs[previousPairs.Count - 1].MustBeLast)
                return null;

            var rightChar = decodeDataCharacter(row, pattern, isOddPattern, false);

            return new ExpandedPair(leftChar, rightChar, pattern, true);
        }

        private bool findNextPair(BitArray row, List<ExpandedPair> previousPairs, int forcedOffset)
        {
            var counters = getDecodeFinderCounters();
            counters[0] = 0;
            counters[1] = 0;
            counters[2] = 0;
            counters[3] = 0;

            var width = row.Size;

            int rowOffset;
            if (forcedOffset >= 0)
                rowOffset = forcedOffset;
            else if (previousPairs.Count == 0)
                rowOffset = 0;
            else
            {
                var lastPair = previousPairs[previousPairs.Count - 1];
                rowOffset = lastPair.FinderPattern.StartEnd[1];
            }
            var searchingEvenPair = previousPairs.Count % 2 != 0;
            if (startFromEven)
                searchingEvenPair = !searchingEvenPair;

            var isWhite = false;
            while (rowOffset < width)
            {
                isWhite = !row[rowOffset];
                if (!isWhite)
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
                        if (searchingEvenPair)
                            reverseCounters(counters);

                        if (isFinderPattern(counters))
                        {
                            startEnd[0] = patternStart;
                            startEnd[1] = x;
                            return true;
                        }

                        if (searchingEvenPair)
                            reverseCounters(counters);

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
            return false;
        }

        private static void reverseCounters(int[] counters)
        {
            var length = counters.Length;
            for (var i = 0; i < length / 2; ++i)
            {
                var tmp = counters[i];
                counters[i] = counters[length - i - 1];
                counters[length - i - 1] = tmp;
            }
        }

        private FinderPattern parseFoundFinderPattern(BitArray row, int rowNumber, bool oddPattern)
        {
            // Actually we found elements 2-5.
            int firstCounter;
            int start;
            int end;

            if (oddPattern)
            {
                // If pattern number is odd, we need to locate element 1 *before* the current block.

                var firstElementStart = startEnd[0] - 1;
                // Locate element 1
                while (firstElementStart >= 0 &&
                       !row[firstElementStart])
                    firstElementStart--;

                firstElementStart++;
                firstCounter = startEnd[0] - firstElementStart;
                start = firstElementStart;
                end = startEnd[1];
            }
            else
            {
                // If pattern number is even, the pattern is reversed, so we need to locate element 1 *after* the current block.
                start = startEnd[0];
                end = row.getNextUnset(startEnd[1] + 1);
                firstCounter = end - startEnd[1];
            }

            // Make 'counters' hold 1-4
            var counters = getDecodeFinderCounters();
            Array.Copy(counters, 0, counters, 1, counters.Length - 1);

            counters[0] = firstCounter;
            int value;
            if (!parseFinderValue(counters, FINDER_PATTERNS, out value))
                return null;

            return new FinderPattern(value, new[] {start, end}, start, end, rowNumber);
        }

        internal DataCharacter decodeDataCharacter(BitArray row,
                                                   FinderPattern pattern,
                                                   bool isOddPattern,
                                                   bool leftChar)
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

            if (leftChar)
            {
                if (!recordPatternInReverse(row, pattern.StartEnd[0], counters))
                    return null;
            }
            else
            {
                if (!recordPattern(row, pattern.StartEnd[1], counters))
                    return null;
                // reverse it
                for (int i = 0, j = counters.Length - 1; i < j; i++, j--)
                {
                    var temp = counters[i];
                    counters[i] = counters[j];
                    counters[j] = temp;
                }
            } //counters[] has the pixels of the module

            const int numModules = 17; //left and right data characters have all the same length
            var elementWidth = count(counters) / (float)numModules;

            // Sanity check: element width for pattern and the character should match
            var expectedElementWidth = (pattern.StartEnd[1] - pattern.StartEnd[0]) / 15.0f;
            if (Math.Abs(elementWidth - expectedElementWidth) / expectedElementWidth > 0.3f)
                return null;

            var oddCounts = getOddCounts();
            var evenCounts = getEvenCounts();
            var oddRoundingErrors = getOddRoundingErrors();
            var evenRoundingErrors = getEvenRoundingErrors();

            for (var i = 0; i < counters.Length; i++)
            {
                var divided = 1.0f * counters[i] / elementWidth;
                var rounded = (int)(divided + 0.5f); // Round
                if (rounded < 1)
                {
                    if (divided < 0.3f)
                        return null;
                    rounded = 1;
                }
                else if (rounded > 8)
                {
                    if (divided > 8.7f)
                        return null;
                    rounded = 8;
                }
                var offset = i >> 1;
                if ((i & 0x01) == 0)
                {
                    oddCounts[offset] = rounded;
                    oddRoundingErrors[offset] = divided - rounded;
                }
                else
                {
                    evenCounts[offset] = rounded;
                    evenRoundingErrors[offset] = divided - rounded;
                }
            }

            if (!adjustOddEvenCounts(numModules))
                return null;

            var weightRowNumber = 4 * pattern.Value + (isOddPattern ? 0 : 2) + (leftChar ? 0 : 1) - 1;

            var oddSum = 0;
            var oddChecksumPortion = 0;
            for (var i = oddCounts.Length - 1; i >= 0; i--)
            {
                if (isNotA1left(pattern, isOddPattern, leftChar))
                {
                    var weight = WEIGHTS[weightRowNumber][2 * i];
                    oddChecksumPortion += oddCounts[i] * weight;
                }
                oddSum += oddCounts[i];
            }
            var evenChecksumPortion = 0;
            //int evenSum = 0;
            for (var i = evenCounts.Length - 1; i >= 0; i--)
                if (isNotA1left(pattern, isOddPattern, leftChar))
                {
                    var weight = WEIGHTS[weightRowNumber][2 * i + 1];
                    evenChecksumPortion += evenCounts[i] * weight;
                }
                //evenSum += evenCounts[i];
            var checksumPortion = oddChecksumPortion + evenChecksumPortion;

            if ((oddSum & 0x01) != 0 ||
                oddSum > 13 ||
                oddSum < 4)
                return null;

            var group = (13 - oddSum) / 2;
            var oddWidest = SYMBOL_WIDEST[group];
            var evenWidest = 9 - oddWidest;
            var vOdd = RSSUtils.getRSSvalue(oddCounts, oddWidest, true);
            var vEven = RSSUtils.getRSSvalue(evenCounts, evenWidest, false);
            var tEven = EVEN_TOTAL_SUBSET[group];
            var gSum = GSUM[group];
            var value = vOdd * tEven + vEven + gSum;

            return new DataCharacter(value, checksumPortion);
        }

        private static bool isNotA1left(FinderPattern pattern, bool isOddPattern, bool leftChar)
        {
            // A1: pattern.getValue is 0 (A), and it's an oddPattern, and it is a left char
            return !(pattern.Value == 0 && isOddPattern && leftChar);
        }

        private bool adjustOddEvenCounts(int numModules)
        {
            var oddSum = count(getOddCounts());
            var evenSum = count(getEvenCounts());
            var mismatch = oddSum + evenSum - numModules;
            var oddParityBad = (oddSum & 0x01) == 1;
            var evenParityBad = (evenSum & 0x01) == 0;

            var incrementOdd = false;
            var decrementOdd = false;

            if (oddSum > 13)
                decrementOdd = true;
            else if (oddSum < 4)
                incrementOdd = true;
            var incrementEven = false;
            var decrementEven = false;
            if (evenSum > 13)
                decrementEven = true;
            else if (evenSum < 4)
                incrementEven = true;

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

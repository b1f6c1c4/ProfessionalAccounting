using System;
using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         This class attempts to find finder patterns in a QR Code. Finder patterns are the square
    ///         markers at three corners of a QR Code.
    ///     </p>
    ///     <p>This class is thread-safe but not reentrant. Each thread must allocate its own object.
    /// </summary>
    /// <author>Sean Owen</author>
    public class FinderPatternFinder
    {
        private const int CENTER_QUORUM = 2;

        /// <summary>
        ///     1 pixel/module times 3 modules/center
        /// </summary>
        protected internal const int MIN_SKIP = 3;

        /// <summary>
        ///     support up to version 10 for mobile clients
        /// </summary>
        protected internal const int MAX_MODULES = 57;

        private const int INTEGER_MATH_SHIFT = 8;

        private readonly BitMatrix image;
        private List<FinderPattern> possibleCenters;
        private bool hasSkipped;
        private readonly int[] crossCheckStateCount;
        private readonly ResultPointCallback resultPointCallback;

        /// <summary>
        ///     <p>Creates a finder that will search the image for three finder patterns.</p>
        /// </summary>
        /// <param name="image">image to search</param>
        public FinderPatternFinder(BitMatrix image)
            : this(image, null) {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="FinderPatternFinder" /> class.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="resultPointCallback">The result point callback.</param>
        public FinderPatternFinder(BitMatrix image, ResultPointCallback resultPointCallback)
        {
            this.image = image;
            possibleCenters = new List<FinderPattern>();
            crossCheckStateCount = new int[5];
            this.resultPointCallback = resultPointCallback;
        }

        /// <summary>
        ///     Gets the image.
        /// </summary>
        protected internal virtual BitMatrix Image { get { return image; } }

        /// <summary>
        ///     Gets the possible centers.
        /// </summary>
        protected internal virtual List<FinderPattern> PossibleCenters { get { return possibleCenters; } }

        internal virtual FinderPatternInfo find(IDictionary<DecodeHintType, object> hints)
        {
            var tryHarder = hints != null && hints.ContainsKey(DecodeHintType.TRY_HARDER);
            var pureBarcode = hints != null && hints.ContainsKey(DecodeHintType.PURE_BARCODE);
            var maxI = image.Height;
            var maxJ = image.Width;
            // We are looking for black/white/black/white/black modules in
            // 1:1:3:1:1 ratio; this tracks the number of such modules seen so far

            // Let's assume that the maximum version QR Code we support takes up 1/4 the height of the
            // image, and then account for the center being 3 modules in size. This gives the smallest
            // number of pixels the center could be, so skip this often. When trying harder, look for all
            // QR versions regardless of how dense they are.
            var iSkip = (3 * maxI) / (4 * MAX_MODULES);
            if (iSkip < MIN_SKIP || tryHarder)
                iSkip = MIN_SKIP;

            var done = false;
            var stateCount = new int[5];
            for (var i = iSkip - 1; i < maxI && !done; i += iSkip)
            {
                // Get a row of black/white values
                stateCount[0] = 0;
                stateCount[1] = 0;
                stateCount[2] = 0;
                stateCount[3] = 0;
                stateCount[4] = 0;
                var currentState = 0;
                for (var j = 0; j < maxJ; j++)
                    if (image[j, i])
                    {
                        // Black pixel
                        if ((currentState & 1) == 1)
                            // Counting white pixels
                            currentState++;
                        stateCount[currentState]++;
                    }
                    else
                        // White pixel
                        if ((currentState & 1) == 0)
                            // Counting black pixels
                            if (currentState == 4)
                                // A winner?
                                if (foundPatternCross(stateCount))
                                {
                                    // Yes
                                    var confirmed = handlePossibleCenter(stateCount, i, j, pureBarcode);
                                    if (confirmed)
                                    {
                                        // Start examining every other line. Checking each line turned out to be too
                                        // expensive and didn't improve performance.
                                        iSkip = 2;
                                        if (hasSkipped)
                                            done = haveMultiplyConfirmedCenters();
                                        else
                                        {
                                            var rowSkip = findRowSkip();
                                            if (rowSkip > stateCount[2])
                                            {
                                                // Skip rows between row of lower confirmed center
                                                // and top of presumed third confirmed center
                                                // but back up a bit to get a full chance of detecting
                                                // it, entire width of center of finder pattern

                                                // Skip by rowSkip, but back off by stateCount[2] (size of last center
                                                // of pattern we saw) to be conservative, and also back off by iSkip which
                                                // is about to be re-added
                                                i += rowSkip - stateCount[2] - iSkip;
                                                j = maxJ - 1;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        stateCount[0] = stateCount[2];
                                        stateCount[1] = stateCount[3];
                                        stateCount[2] = stateCount[4];
                                        stateCount[3] = 1;
                                        stateCount[4] = 0;
                                        currentState = 3;
                                        continue;
                                    }
                                    // Clear state to start looking again
                                    currentState = 0;
                                    stateCount[0] = 0;
                                    stateCount[1] = 0;
                                    stateCount[2] = 0;
                                    stateCount[3] = 0;
                                    stateCount[4] = 0;
                                }
                                else
                                {
                                    // No, shift counts back by two
                                    stateCount[0] = stateCount[2];
                                    stateCount[1] = stateCount[3];
                                    stateCount[2] = stateCount[4];
                                    stateCount[3] = 1;
                                    stateCount[4] = 0;
                                    currentState = 3;
                                }
                            else
                                stateCount[++currentState]++;
                        else
                            // Counting white pixels
                            stateCount[currentState]++;
                if (foundPatternCross(stateCount))
                {
                    var confirmed = handlePossibleCenter(stateCount, i, maxJ, pureBarcode);
                    if (confirmed)
                    {
                        iSkip = stateCount[0];
                        if (hasSkipped)
                            // Found a third one
                            done = haveMultiplyConfirmedCenters();
                    }
                }
            }

            var patternInfo = selectBestPatterns();
            if (patternInfo == null)
                return null;

            ResultPoint.orderBestPatterns(patternInfo);

            return new FinderPatternInfo(patternInfo);
        }

        /// <summary>
        ///     Given a count of black/white/black/white/black pixels just seen and an end position,
        ///     figures the location of the center of this run.
        /// </summary>
        private static float? centerFromEnd(int[] stateCount, int end)
        {
            var result = (end - stateCount[4] - stateCount[3]) - stateCount[2] / 2.0f;
            if (Single.IsNaN(result))
                return null;
            return result;
        }

        /// <param name="stateCount">
        ///     count of black/white/black/white/black pixels just read
        /// </param>
        /// <returns>
        ///     true iff the proportions of the counts is close enough to the 1/1/3/1/1 ratios
        ///     used by finder patterns to be considered a match
        /// </returns>
        protected internal static bool foundPatternCross(int[] stateCount)
        {
            var totalModuleSize = 0;
            for (var i = 0; i < 5; i++)
            {
                var count = stateCount[i];
                if (count == 0)
                    return false;
                totalModuleSize += count;
            }
            if (totalModuleSize < 7)
                return false;
            var moduleSize = (totalModuleSize << INTEGER_MATH_SHIFT) / 7;
            var maxVariance = moduleSize / 2;
            // Allow less than 50% variance from 1-1-3-1-1 proportions
            return Math.Abs(moduleSize - (stateCount[0] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCount[1] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(3 * moduleSize - (stateCount[2] << INTEGER_MATH_SHIFT)) < 3 * maxVariance &&
                   Math.Abs(moduleSize - (stateCount[3] << INTEGER_MATH_SHIFT)) < maxVariance &&
                   Math.Abs(moduleSize - (stateCount[4] << INTEGER_MATH_SHIFT)) < maxVariance;
        }

        private int[] CrossCheckStateCount
        {
            get
            {
                crossCheckStateCount[0] = 0;
                crossCheckStateCount[1] = 0;
                crossCheckStateCount[2] = 0;
                crossCheckStateCount[3] = 0;
                crossCheckStateCount[4] = 0;
                return crossCheckStateCount;
            }
        }

        /// <summary>
        ///     After a vertical and horizontal scan finds a potential finder pattern, this method
        ///     "cross-cross-cross-checks" by scanning down diagonally through the center of the possible
        ///     finder pattern to see if the same proportion is detected.
        /// </summary>
        /// <param name="startI">row where a finder pattern was detected</param>
        /// <param name="centerJ">center of the section that appears to cross a finder pattern</param>
        /// <param name="maxCount">
        ///     maximum reasonable number of modules that should be observed in any reading state, based on the
        ///     results of the horizontal scan
        /// </param>
        /// <param name="originalStateCountTotal">The original state count total.</param>
        /// <returns>true if proportions are withing expected limits</returns>
        private bool crossCheckDiagonal(int startI, int centerJ, int maxCount, int originalStateCountTotal)
        {
            var stateCount = CrossCheckStateCount;

            // Start counting up, left from center finding black center mass
            var i = 0;
            while (startI >= i &&
                   centerJ >= i &&
                   image[centerJ - i, startI - i])
            {
                stateCount[2]++;
                i++;
            }

            if (startI < i ||
                centerJ < i)
                return false;

            // Continue up, left finding white space
            while (startI >= i &&
                   centerJ >= i &&
                   !image[centerJ - i, startI - i] &&
                   stateCount[1] <= maxCount)
            {
                stateCount[1]++;
                i++;
            }

            // If already too many modules in this state or ran off the edge:
            if (startI < i ||
                centerJ < i ||
                stateCount[1] > maxCount)
                return false;

            // Continue up, left finding black border
            while (startI >= i &&
                   centerJ >= i &&
                   image[centerJ - i, startI - i] &&
                   stateCount[0] <= maxCount)
            {
                stateCount[0]++;
                i++;
            }
            if (stateCount[0] > maxCount)
                return false;

            var maxI = image.Height;
            var maxJ = image.Width;

            // Now also count down, right from center
            i = 1;
            while (startI + i < maxI &&
                   centerJ + i < maxJ &&
                   image[centerJ + i, startI + i])
            {
                stateCount[2]++;
                i++;
            }

            // Ran off the edge?
            if (startI + i >= maxI ||
                centerJ + i >= maxJ)
                return false;

            while (startI + i < maxI &&
                   centerJ + i < maxJ &&
                   !image[centerJ + i, startI + i] &&
                   stateCount[3] < maxCount)
            {
                stateCount[3]++;
                i++;
            }

            if (startI + i >= maxI ||
                centerJ + i >= maxJ ||
                stateCount[3] >= maxCount)
                return false;

            while (startI + i < maxI &&
                   centerJ + i < maxJ &&
                   image[centerJ + i, startI + i] &&
                   stateCount[4] < maxCount)
            {
                stateCount[4]++;
                i++;
            }

            if (stateCount[4] >= maxCount)
                return false;

            // If we found a finder-pattern-like section, but its size is more than 100% different than
            // the original, assume it's a false positive
            var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
            return Math.Abs(stateCountTotal - originalStateCountTotal) < 2 * originalStateCountTotal &&
                   foundPatternCross(stateCount);
        }

        /// <summary>
        ///     <p>
        ///         After a horizontal scan finds a potential finder pattern, this method
        ///         "cross-checks" by scanning down vertically through the center of the possible
        ///         finder pattern to see if the same proportion is detected.
        ///     </p>
        /// </summary>
        /// <param name="startI">row where a finder pattern was detected</param>
        /// <param name="centerJ">center of the section that appears to cross a finder pattern</param>
        /// <param name="maxCount">
        ///     maximum reasonable number of modules that should be
        ///     observed in any reading state, based on the results of the horizontal scan
        /// </param>
        /// <param name="originalStateCountTotal">The original state count total.</param>
        /// <returns>
        ///     vertical center of finder pattern, or null if not found
        /// </returns>
        private float? crossCheckVertical(int startI, int centerJ, int maxCount, int originalStateCountTotal)
        {
            var maxI = image.Height;
            var stateCount = CrossCheckStateCount;

            // Start counting up from center
            var i = startI;
            while (i >= 0 &&
                   image[centerJ, i])
            {
                stateCount[2]++;
                i--;
            }
            if (i < 0)
                return null;
            while (i >= 0 &&
                   !image[centerJ, i] &&
                   stateCount[1] <= maxCount)
            {
                stateCount[1]++;
                i--;
            }
            // If already too many modules in this state or ran off the edge:
            if (i < 0 ||
                stateCount[1] > maxCount)
                return null;
            while (i >= 0 &&
                   image[centerJ, i] &&
                   stateCount[0] <= maxCount)
            {
                stateCount[0]++;
                i--;
            }
            if (stateCount[0] > maxCount)
                return null;

            // Now also count down from center
            i = startI + 1;
            while (i < maxI &&
                   image[centerJ, i])
            {
                stateCount[2]++;
                i++;
            }
            if (i == maxI)
                return null;
            while (i < maxI &&
                   !image[centerJ, i] &&
                   stateCount[3] < maxCount)
            {
                stateCount[3]++;
                i++;
            }
            if (i == maxI ||
                stateCount[3] >= maxCount)
                return null;
            while (i < maxI &&
                   image[centerJ, i] &&
                   stateCount[4] < maxCount)
            {
                stateCount[4]++;
                i++;
            }
            if (stateCount[4] >= maxCount)
                return null;

            // If we found a finder-pattern-like section, but its size is more than 40% different than
            // the original, assume it's a false positive
            var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
            if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= 2 * originalStateCountTotal)
                return null;

            return foundPatternCross(stateCount) ? centerFromEnd(stateCount, i) : null;
        }

        /// <summary>
        ///     <p>
        ///         Like {@link #crossCheckVertical(int, int, int, int)}, and in fact is basically identical,
        ///         except it reads horizontally instead of vertically. This is used to cross-cross
        ///         check a vertical cross check and locate the real center of the alignment pattern.
        ///     </p>
        /// </summary>
        private float? crossCheckHorizontal(int startJ, int centerI, int maxCount, int originalStateCountTotal)
        {
            var maxJ = image.Width;
            var stateCount = CrossCheckStateCount;

            var j = startJ;
            while (j >= 0 &&
                   image[j, centerI])
            {
                stateCount[2]++;
                j--;
            }
            if (j < 0)
                return null;
            while (j >= 0 &&
                   !image[j, centerI] &&
                   stateCount[1] <= maxCount)
            {
                stateCount[1]++;
                j--;
            }
            if (j < 0 ||
                stateCount[1] > maxCount)
                return null;
            while (j >= 0 &&
                   image[j, centerI] &&
                   stateCount[0] <= maxCount)
            {
                stateCount[0]++;
                j--;
            }
            if (stateCount[0] > maxCount)
                return null;

            j = startJ + 1;
            while (j < maxJ &&
                   image[j, centerI])
            {
                stateCount[2]++;
                j++;
            }
            if (j == maxJ)
                return null;
            while (j < maxJ &&
                   !image[j, centerI] &&
                   stateCount[3] < maxCount)
            {
                stateCount[3]++;
                j++;
            }
            if (j == maxJ ||
                stateCount[3] >= maxCount)
                return null;
            while (j < maxJ &&
                   image[j, centerI] &&
                   stateCount[4] < maxCount)
            {
                stateCount[4]++;
                j++;
            }
            if (stateCount[4] >= maxCount)
                return null;

            // If we found a finder-pattern-like section, but its size is significantly different than
            // the original, assume it's a false positive
            var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] + stateCount[4];
            if (5 * Math.Abs(stateCountTotal - originalStateCountTotal) >= originalStateCountTotal)
                return null;

            return foundPatternCross(stateCount) ? centerFromEnd(stateCount, j) : null;
        }

        /// <summary>
        ///     <p>
        ///         This is called when a horizontal scan finds a possible alignment pattern. It will
        ///         cross check with a vertical scan, and if successful, will, ah, cross-cross-check
        ///         with another horizontal scan. This is needed primarily to locate the real horizontal
        ///         center of the pattern in cases of extreme skew.
        ///         And then we cross-cross-cross check with another diagonal scan.
        ///     </p>
        ///     If that succeeds the finder pattern location is added to a list that tracks
        ///     the number of times each location has been nearly-matched as a finder pattern.
        ///     Each additional find is more evidence that the location is in fact a finder
        ///     pattern center
        /// </summary>
        /// <param name="stateCount">reading state module counts from horizontal scan</param>
        /// <param name="i">row where finder pattern may be found</param>
        /// <param name="j">end of possible finder pattern in row</param>
        /// <param name="pureBarcode">true if in "pure barcode" mode</param>
        /// <returns>
        ///     true if a finder pattern candidate was found this time
        /// </returns>
        protected bool handlePossibleCenter(int[] stateCount, int i, int j, bool pureBarcode)
        {
            var stateCountTotal = stateCount[0] + stateCount[1] + stateCount[2] + stateCount[3] +
                                  stateCount[4];
            var centerJ = centerFromEnd(stateCount, j);
            if (centerJ == null)
                return false;
            var centerI = crossCheckVertical(i, (int)centerJ.Value, stateCount[2], stateCountTotal);
            if (centerI != null)
            {
                // Re-cross check
                centerJ = crossCheckHorizontal((int)centerJ.Value, (int)centerI.Value, stateCount[2], stateCountTotal);
                if (centerJ != null &&
                    (!pureBarcode || crossCheckDiagonal((int)centerI, (int)centerJ, stateCount[2], stateCountTotal)))
                {
                    var estimatedModuleSize = stateCountTotal / 7.0f;
                    var found = false;
                    for (var index = 0; index < possibleCenters.Count; index++)
                    {
                        var center = possibleCenters[index];
                        // Look for about the same center and module size:
                        if (center.aboutEquals(estimatedModuleSize, centerI.Value, centerJ.Value))
                        {
                            possibleCenters.RemoveAt(index);
                            possibleCenters.Insert(
                                                   index,
                                                   center.combineEstimate(
                                                                          centerI.Value,
                                                                          centerJ.Value,
                                                                          estimatedModuleSize));

                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var point = new FinderPattern(centerJ.Value, centerI.Value, estimatedModuleSize);

                        possibleCenters.Add(point);
                        if (resultPointCallback != null)
                            resultPointCallback(point);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <returns>
        ///     number of rows we could safely skip during scanning, based on the first
        ///     two finder patterns that have been located. In some cases their position will
        ///     allow us to infer that the third pattern must lie below a certain point farther
        ///     down in the image.
        /// </returns>
        private int findRowSkip()
        {
            var max = possibleCenters.Count;
            if (max <= 1)
                return 0;
            ResultPoint firstConfirmedCenter = null;
            foreach (var center in possibleCenters)
                if (center.Count >= CENTER_QUORUM)
                    if (firstConfirmedCenter == null)
                        firstConfirmedCenter = center;
                    else
                    {
                        // We have two confirmed centers
                        // How far down can we skip before resuming looking for the next
                        // pattern? In the worst case, only the difference between the
                        // difference in the x / y coordinates of the two centers.
                        // This is the case where you find top left last.
                        hasSkipped = true;
                        //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
                        return
                            (int)
                            (Math.Abs(firstConfirmedCenter.X - center.X) - Math.Abs(firstConfirmedCenter.Y - center.Y)) /
                            2;
                    }
            return 0;
        }

        /// <returns>
        ///     true iff we have found at least 3 finder patterns that have been detected
        ///     at least {@link #CENTER_QUORUM} times each, and, the estimated module size of the
        ///     candidates is "pretty similar"
        /// </returns>
        private bool haveMultiplyConfirmedCenters()
        {
            var confirmedCount = 0;
            var totalModuleSize = 0.0f;
            var max = possibleCenters.Count;
            foreach (var pattern in possibleCenters)
                if (pattern.Count >= CENTER_QUORUM)
                {
                    confirmedCount++;
                    totalModuleSize += pattern.EstimatedModuleSize;
                }
            if (confirmedCount < 3)
                return false;
            // OK, we have at least 3 confirmed centers, but, it's possible that one is a "false positive"
            // and that we need to keep looking. We detect this by asking if the estimated module sizes
            // vary too much. We arbitrarily say that when the total deviation from average exceeds
            // 5% of the total module size estimates, it's too much.
            var average = totalModuleSize / max;
            var totalDeviation = 0.0f;
            for (var i = 0; i < max; i++)
            {
                var pattern = possibleCenters[i];
                totalDeviation += Math.Abs(pattern.EstimatedModuleSize - average);
            }
            return totalDeviation <= 0.05f * totalModuleSize;
        }

        /// <returns>
        ///     the 3 best {@link FinderPattern}s from our list of candidates. The "best" are
        ///     those that have been detected at least {@link #CENTER_QUORUM} times, and whose module
        ///     size differs from the average among those patterns the least
        /// </returns>
        private FinderPattern[] selectBestPatterns()
        {
            var startSize = possibleCenters.Count;
            if (startSize < 3)
                // Couldn't find enough finder patterns
                return null;

            // Filter outlier possibilities whose module size is too different
            if (startSize > 3)
            {
                // But we can only afford to do so if we have at least 4 possibilities to choose from
                var totalModuleSize = 0.0f;
                var square = 0.0f;
                foreach (var center in possibleCenters)
                {
                    var size = center.EstimatedModuleSize;
                    totalModuleSize += size;
                    square += size * size;
                }
                var average = totalModuleSize / startSize;
                var stdDev = (float)Math.Sqrt(square / startSize - average * average);

                possibleCenters.Sort(new FurthestFromAverageComparator(average));

                var limit = Math.Max(0.2f * average, stdDev);

                for (var i = 0; i < possibleCenters.Count && possibleCenters.Count > 3; i++)
                {
                    var pattern = possibleCenters[i];
                    if (Math.Abs(pattern.EstimatedModuleSize - average) > limit)
                    {
                        possibleCenters.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (possibleCenters.Count > 3)
            {
                // Throw away all but those first size candidate points we found.

                var totalModuleSize = 0.0f;
                foreach (var possibleCenter in possibleCenters)
                    totalModuleSize += possibleCenter.EstimatedModuleSize;

                var average = totalModuleSize / possibleCenters.Count;

                possibleCenters.Sort(new CenterComparator(average));

                //possibleCenters.subList(3, possibleCenters.Count).clear();
                possibleCenters = possibleCenters.GetRange(0, 3);
            }

            return new[]
                       {
                           possibleCenters[0],
                           possibleCenters[1],
                           possibleCenters[2]
                       };
        }

        /// <summary>
        ///     Orders by furthest from average
        /// </summary>
        private sealed class FurthestFromAverageComparator : IComparer<FinderPattern>
        {
            private readonly float average;

            public FurthestFromAverageComparator(float f) { average = f; }

            public int Compare(FinderPattern x, FinderPattern y)
            {
                var dA = Math.Abs(y.EstimatedModuleSize - average);
                var dB = Math.Abs(x.EstimatedModuleSize - average);
                return dA < dB ? -1 : dA == dB ? 0 : 1;
            }
        }

        /// <summary>
        ///     <p>Orders by {@link FinderPattern#getCount()}, descending.</p>
        /// </summary>
        private sealed class CenterComparator : IComparer<FinderPattern>
        {
            private readonly float average;

            public CenterComparator(float f) { average = f; }

            public int Compare(FinderPattern x, FinderPattern y)
            {
                if (y.Count == x.Count)
                {
                    var dA = Math.Abs(y.EstimatedModuleSize - average);
                    var dB = Math.Abs(x.EstimatedModuleSize - average);
                    return dA < dB ? 1 : dA == dB ? 0 : -1;
                }
                return y.Count - x.Count;
            }
        }
    }
}

using System;

namespace ZXing.OneD.RSS
{
    /// <summary>
    /// </summary>
    public abstract class AbstractRSSReader : OneDReader
    {
        private static readonly int MAX_AVG_VARIANCE = (int)(PATTERN_MATCH_RESULT_SCALE_FACTOR * 0.2f);
        private static readonly int MAX_INDIVIDUAL_VARIANCE = (int)(PATTERN_MATCH_RESULT_SCALE_FACTOR * 0.45f);

        private const float MIN_FINDER_PATTERN_RATIO = 9.5f / 12.0f;
        private const float MAX_FINDER_PATTERN_RATIO = 12.5f / 14.0f;

        private readonly int[] decodeFinderCounters;
        private readonly int[] dataCharacterCounters;
        private readonly float[] oddRoundingErrors;
        private readonly float[] evenRoundingErrors;
        private readonly int[] oddCounts;
        private readonly int[] evenCounts;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AbstractRSSReader" /> class.
        /// </summary>
        protected AbstractRSSReader()
        {
            decodeFinderCounters = new int[4];
            dataCharacterCounters = new int[8];
            oddRoundingErrors = new float[4];
            evenRoundingErrors = new float[4];
            oddCounts = new int[dataCharacterCounters.Length / 2];
            evenCounts = new int[dataCharacterCounters.Length / 2];
        }

        /// <summary>
        ///     Gets the decode finder counters.
        /// </summary>
        /// <returns></returns>
        protected int[] getDecodeFinderCounters() { return decodeFinderCounters; }

        /// <summary>
        ///     Gets the data character counters.
        /// </summary>
        /// <returns></returns>
        protected int[] getDataCharacterCounters() { return dataCharacterCounters; }

        /// <summary>
        ///     Gets the odd rounding errors.
        /// </summary>
        /// <returns></returns>
        protected float[] getOddRoundingErrors() { return oddRoundingErrors; }

        /// <summary>
        ///     Gets the even rounding errors.
        /// </summary>
        /// <returns></returns>
        protected float[] getEvenRoundingErrors() { return evenRoundingErrors; }

        /// <summary>
        ///     Gets the odd counts.
        /// </summary>
        /// <returns></returns>
        protected int[] getOddCounts() { return oddCounts; }

        /// <summary>
        ///     Gets the even counts.
        /// </summary>
        /// <returns></returns>
        protected int[] getEvenCounts() { return evenCounts; }

        /// <summary>
        ///     Parses the finder value.
        /// </summary>
        /// <param name="counters">The counters.</param>
        /// <param name="finderPatterns">The finder patterns.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected static bool parseFinderValue(int[] counters,
                                               int[][] finderPatterns,
                                               out int value)
        {
            for (value = 0; value < finderPatterns.Length; value++)
                if (patternMatchVariance(counters, finderPatterns[value], MAX_INDIVIDUAL_VARIANCE) <
                    MAX_AVG_VARIANCE)
                    return true;
            return false;
        }

        /// <summary>
        ///     Counts the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        protected static int count(int[] array)
        {
            var count = 0;
            foreach (var a in array)
                count += a;
            return count;
        }

        /// <summary>
        ///     Increments the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="errors">The errors.</param>
        protected static void increment(int[] array, float[] errors)
        {
            var index = 0;
            var biggestError = errors[0];
            for (var i = 1; i < array.Length; i++)
                if (errors[i] > biggestError)
                {
                    biggestError = errors[i];
                    index = i;
                }
            array[index]++;
        }

        /// <summary>
        ///     Decrements the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="errors">The errors.</param>
        protected static void decrement(int[] array, float[] errors)
        {
            var index = 0;
            var biggestError = errors[0];
            for (var i = 1; i < array.Length; i++)
                if (errors[i] < biggestError)
                {
                    biggestError = errors[i];
                    index = i;
                }
            array[index]--;
        }

        /// <summary>
        ///     Determines whether [is finder pattern] [the specified counters].
        /// </summary>
        /// <param name="counters">The counters.</param>
        /// <returns>
        ///     <c>true</c> if [is finder pattern] [the specified counters]; otherwise, <c>false</c>.
        /// </returns>
        protected static bool isFinderPattern(int[] counters)
        {
            var firstTwoSum = counters[0] + counters[1];
            var sum = firstTwoSum + counters[2] + counters[3];
            var ratio = firstTwoSum / (float)sum;
            if (ratio >= MIN_FINDER_PATTERN_RATIO &&
                ratio <= MAX_FINDER_PATTERN_RATIO)
            {
                // passes ratio test in spec, but see if the counts are unreasonable
                var minCounter = Int32.MaxValue;
                var maxCounter = Int32.MinValue;
                foreach (var counter in counters)
                {
                    if (counter > maxCounter)
                        maxCounter = counter;
                    if (counter < minCounter)
                        minCounter = counter;
                }
                return maxCounter < 10 * minCounter;
            }
            return false;
        }
    }
}
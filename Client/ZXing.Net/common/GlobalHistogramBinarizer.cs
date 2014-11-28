namespace ZXing.Common
{
    /// <summary>
    ///     This Binarizer implementation uses the old ZXing global histogram approach. It is suitable
    ///     for low-end mobile devices which don't have enough CPU or memory to use a local thresholding
    ///     algorithm. However, because it picks a global black point, it cannot handle difficult shadows
    ///     and gradients.
    ///     Faster mobile devices and all desktop applications should probably use HybridBinarizer instead.
    ///     <author>dswitkin@google.com (Daniel Switkin)</author>
    ///     <author>Sean Owen</author>
    /// </summary>
    public class GlobalHistogramBinarizer : Binarizer
    {
        private const int LUMINANCE_BITS = 5;
        private const int LUMINANCE_SHIFT = 8 - LUMINANCE_BITS;
        private const int LUMINANCE_BUCKETS = 1 << LUMINANCE_BITS;
        private static readonly byte[] EMPTY = new byte[0];

        private byte[] luminances;
        private readonly int[] buckets;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalHistogramBinarizer" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public GlobalHistogramBinarizer(LuminanceSource source)
            : base(source)
        {
            luminances = EMPTY;
            buckets = new int[LUMINANCE_BUCKETS];
        }

        /// <summary>
        ///     Applies simple sharpening to the row data to improve performance of the 1D Readers.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public override BitArray getBlackRow(int y, BitArray row)
        {
            var source = LuminanceSource;
            var width = source.Width;
            if (row == null ||
                row.Size < width)
                row = new BitArray(width);
            else
                row.clear();

            initArrays(width);
            var localLuminances = source.getRow(y, luminances);
            var localBuckets = buckets;
            for (var x = 0; x < width; x++)
            {
                var pixel = localLuminances[x] & 0xff;
                localBuckets[pixel >> LUMINANCE_SHIFT]++;
            }
            int blackPoint;
            if (!estimateBlackPoint(localBuckets, out blackPoint))
                return null;

            var left = localLuminances[0] & 0xff;
            var center = localLuminances[1] & 0xff;
            for (var x = 1; x < width - 1; x++)
            {
                var right = localLuminances[x + 1] & 0xff;
                // A simple -1 4 -1 box filter with a weight of 2.
                var luminance = ((center << 2) - left - right) >> 1;
                row[x] = (luminance < blackPoint);
                left = center;
                center = right;
            }
            return row;
        }

        /// <summary>
        ///     Does not sharpen the data, as this call is intended to only be used by 2D Readers.
        /// </summary>
        public override BitMatrix BlackMatrix
        {
            get
            {
                var source = LuminanceSource;
                byte[] localLuminances;

                var width = source.Width;
                var height = source.Height;
                var matrix = new BitMatrix(width, height);

                // Quickly calculates the histogram by sampling four rows from the image. This proved to be
                // more robust on the blackbox tests than sampling a diagonal as we used to do.
                initArrays(width);
                var localBuckets = buckets;
                for (var y = 1; y < 5; y++)
                {
                    var row = height * y / 5;
                    localLuminances = source.getRow(row, luminances);
                    var right = (width << 2) / 5;
                    for (var x = width / 5; x < right; x++)
                    {
                        var pixel = localLuminances[x] & 0xff;
                        localBuckets[pixel >> LUMINANCE_SHIFT]++;
                    }
                }
                int blackPoint;
                if (!estimateBlackPoint(localBuckets, out blackPoint))
                    return null;

                // We delay reading the entire image luminance until the black point estimation succeeds.
                // Although we end up reading four rows twice, it is consistent with our motto of
                // "fail quickly" which is necessary for continuous scanning.
                localLuminances = source.Matrix;
                for (var y = 0; y < height; y++)
                {
                    var offset = y * width;
                    for (var x = 0; x < width; x++)
                    {
                        var pixel = localLuminances[offset + x] & 0xff;
                        matrix[x, y] = (pixel < blackPoint);
                    }
                }

                return matrix;
            }
        }

        /// <summary>
        ///     Creates a new object with the same type as this Binarizer implementation, but with pristine
        ///     state. This is needed because Binarizer implementations may be stateful, e.g. keeping a cache
        ///     of 1 bit data. See Effective Java for why we can't use Java's clone() method.
        /// </summary>
        /// <param name="source">The LuminanceSource this Binarizer will operate on.</param>
        /// <returns>
        ///     A new concrete Binarizer implementation object.
        /// </returns>
        public override Binarizer createBinarizer(LuminanceSource source)
        {
            return new GlobalHistogramBinarizer(source);
        }

        private void initArrays(int luminanceSize)
        {
            if (luminances.Length < luminanceSize)
                luminances = new byte[luminanceSize];
            for (var x = 0; x < LUMINANCE_BUCKETS; x++)
                buckets[x] = 0;
        }

        private static bool estimateBlackPoint(int[] buckets, out int blackPoint)
        {
            blackPoint = 0;
            // Find the tallest peak in the histogram.
            var numBuckets = buckets.Length;
            var maxBucketCount = 0;
            var firstPeak = 0;
            var firstPeakSize = 0;
            for (var x = 0; x < numBuckets; x++)
            {
                if (buckets[x] > firstPeakSize)
                {
                    firstPeak = x;
                    firstPeakSize = buckets[x];
                }
                if (buckets[x] > maxBucketCount)
                    maxBucketCount = buckets[x];
            }

            // Find the second-tallest peak which is somewhat far from the tallest peak.
            var secondPeak = 0;
            var secondPeakScore = 0;
            for (var x = 0; x < numBuckets; x++)
            {
                var distanceToBiggest = x - firstPeak;
                // Encourage more distant second peaks by multiplying by square of distance.
                var score = buckets[x] * distanceToBiggest * distanceToBiggest;
                if (score > secondPeakScore)
                {
                    secondPeak = x;
                    secondPeakScore = score;
                }
            }

            // Make sure firstPeak corresponds to the black peak.
            if (firstPeak > secondPeak)
            {
                var temp = firstPeak;
                firstPeak = secondPeak;
                secondPeak = temp;
            }

            // If there is too little contrast in the image to pick a meaningful black point, throw rather
            // than waste time trying to decode the image, and risk false positives.
            // TODO: It might be worth comparing the brightest and darkest pixels seen, rather than the
            // two peaks, to determine the contrast.
            if (secondPeak - firstPeak <= numBuckets >> 4)
                return false;

            // Find a valley between them that is low and closer to the white peak.
            var bestValley = secondPeak - 1;
            var bestValleyScore = -1;
            for (var x = secondPeak - 1; x > firstPeak; x--)
            {
                var fromFirst = x - firstPeak;
                var score = fromFirst * fromFirst * (secondPeak - x) * (maxBucketCount - buckets[x]);
                if (score > bestValleyScore)
                {
                    bestValley = x;
                    bestValleyScore = score;
                }
            }

            blackPoint = bestValley << LUMINANCE_SHIFT;
            return true;
        }
    }
}

using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     PDF 417 Detector Result class.  Skipped private backing stores.
    ///     <author>Guenther Grau</author>
    /// </summary>
    public sealed class PDF417DetectorResult
    {
        public BitMatrix Bits { get; private set; }
        public List<ResultPoint[]> Points { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZXing.PDF417.Internal.PDF417DetectorResult" /> class.
        /// </summary>
        /// <param name="bits">Bits.</param>
        /// <param name="points">Points.</param>
        public PDF417DetectorResult(BitMatrix bits, List<ResultPoint[]> points)
        {
            Bits = bits;
            Points = points;
        }
    }
}

using System.Collections.Generic;
using ZXing.Common;
using ZXing.QrCode.Internal;

namespace ZXing.Multi.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         Encapsulates logic that can detect one or more QR Codes in an image, even if the QR Code
    ///         is rotated or skewed, or partially obscured.
    ///     </p>
    ///     <author>Sean Owen</author>
    ///     <author>Hannes Erven</author>
    /// </summary>
    public sealed class MultiDetector : Detector
    {
        private static readonly DetectorResult[] EMPTY_DETECTOR_RESULTS = new DetectorResult[0];

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultiDetector" /> class.
        /// </summary>
        /// <param name="image">The image.</param>
        public MultiDetector(BitMatrix image)
            : base(image) {}

        /// <summary>
        ///     Detects the multi.
        /// </summary>
        /// <param name="hints">The hints.</param>
        /// <returns></returns>
        public DetectorResult[] detectMulti(IDictionary<DecodeHintType, object> hints)
        {
            var image = Image;
            var resultPointCallback =
                hints == null || !hints.ContainsKey(DecodeHintType.NEED_RESULT_POINT_CALLBACK)
                    ? null
                    : (ResultPointCallback)hints[DecodeHintType.NEED_RESULT_POINT_CALLBACK];
            var finder = new MultiFinderPatternFinder(image, resultPointCallback);
            var infos = finder.findMulti(hints);

            if (infos.Length == 0)
                return EMPTY_DETECTOR_RESULTS;

            var result = new List<DetectorResult>();
            foreach (var info in infos)
            {
                var oneResult = processFinderPatternInfo(info);
                if (oneResult != null)
                    result.Add(oneResult);
            }
            if (result.Count == 0)
                return EMPTY_DETECTOR_RESULTS;
            return result.ToArray();
        }
    }
}

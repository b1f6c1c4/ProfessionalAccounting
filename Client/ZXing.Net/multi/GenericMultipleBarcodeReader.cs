using System.Collections.Generic;

namespace ZXing.Multi
{
    /// <summary>
    ///     <p>
    ///         Attempts to locate multiple barcodes in an image by repeatedly decoding portion of the image.
    ///         After one barcode is found, the areas left, above, right and below the barcode's
    ///         {@link com.google.zxing.ResultPoint}s are scanned, recursively.
    ///     </p>
    ///     <p>
    ///         A caller may want to also employ {@link ByQuadrantReader} when attempting to find multiple
    ///         2D barcodes, like QR Codes, in an image, where the presence of multiple barcodes might prevent
    ///         detecting any one of them.
    ///     </p>
    ///     <p>
    ///         That is, instead of passing a {@link Reader} a caller might pass
    ///         <code>new ByQuadrantReader(reader)</code>.
    ///     </p>
    ///     <author>Sean Owen</author>
    /// </summary>
    public sealed class GenericMultipleBarcodeReader : MultipleBarcodeReader, Reader
    {
        private const int MIN_DIMENSION_TO_RECUR = 30;
        private const int MAX_DEPTH = 4;

        private readonly Reader _delegate;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GenericMultipleBarcodeReader" /> class.
        /// </summary>
        /// <param name="delegate">The @delegate.</param>
        public GenericMultipleBarcodeReader(Reader @delegate)
        {
            _delegate = @delegate;
        }

        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns></returns>
        public Result[] decodeMultiple(BinaryBitmap image)
        {
            return decodeMultiple(image, null);
        }

        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="hints">The hints.</param>
        /// <returns></returns>
        public Result[] decodeMultiple(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
        {
            var results = new List<Result>();
            doDecodeMultiple(image, hints, results, 0, 0, 0);
            if ((results.Count == 0))
                return null;
            var numResults = results.Count;
            var resultArray = new Result[numResults];
            for (var i = 0; i < numResults; i++)
                resultArray[i] = results[i];
            return resultArray;
        }

        private void doDecodeMultiple(BinaryBitmap image, IDictionary<DecodeHintType, object> hints,
                                      IList<Result> results, int xOffset, int yOffset, int currentDepth)
        {
            if (currentDepth > MAX_DEPTH)
                return;

            var result = _delegate.decode(image, hints);
            if (result == null)
                return;

            var alreadyFound = false;
            for (var i = 0; i < results.Count; i++)
            {
                var existingResult = results[i];
                if (existingResult.Text.Equals(result.Text))
                {
                    alreadyFound = true;
                    break;
                }
            }
            if (!alreadyFound)
                results.Add(translateResultPoints(result, xOffset, yOffset));

            var resultPoints = result.ResultPoints;
            if (resultPoints == null ||
                resultPoints.Length == 0)
                return;
            var width = image.Width;
            var height = image.Height;
            float minX = width;
            float minY = height;
            var maxX = 0.0f;
            var maxY = 0.0f;
            for (var i = 0; i < resultPoints.Length; i++)
            {
                var point = resultPoints[i];
                var x = point.X;
                var y = point.Y;
                if (x < minX)
                    minX = x;
                if (y < minY)
                    minY = y;
                if (x > maxX)
                    maxX = x;
                if (y > maxY)
                    maxY = y;
            }

            // Decode left of barcode
            if (minX > MIN_DIMENSION_TO_RECUR)
                doDecodeMultiple(
                                 image.crop(0, 0, (int)minX, height),
                                 hints,
                                 results,
                                 xOffset,
                                 yOffset,
                                 currentDepth + 1);
            // Decode above barcode
            if (minY > MIN_DIMENSION_TO_RECUR)
                doDecodeMultiple(image.crop(0, 0, width, (int)minY), hints, results, xOffset, yOffset, currentDepth + 1);
            // Decode right of barcode
            if (maxX < width - MIN_DIMENSION_TO_RECUR)
                doDecodeMultiple(
                                 image.crop((int)maxX, 0, width - (int)maxX, height),
                                 hints,
                                 results,
                                 xOffset + (int)maxX,
                                 yOffset,
                                 currentDepth + 1);
            // Decode below barcode
            if (maxY < height - MIN_DIMENSION_TO_RECUR)
                doDecodeMultiple(
                                 image.crop(0, (int)maxY, width, height - (int)maxY),
                                 hints,
                                 results,
                                 xOffset,
                                 yOffset + (int)maxY,
                                 currentDepth + 1);
        }

        private static Result translateResultPoints(Result result, int xOffset, int yOffset)
        {
            var oldResultPoints = result.ResultPoints;
            var newResultPoints = new ResultPoint[oldResultPoints.Length];
            for (var i = 0; i < oldResultPoints.Length; i++)
            {
                var oldPoint = oldResultPoints[i];
                newResultPoints[i] = new ResultPoint(oldPoint.X + xOffset, oldPoint.Y + yOffset);
            }
            var newResult = new Result(result.Text, result.RawBytes, newResultPoints, result.BarcodeFormat);
            newResult.putAllMetadata(result.ResultMetadata);
            return newResult;
        }

        /// <summary>
        ///     Locates and decodes a barcode in some format within an image.
        /// </summary>
        /// <param name="image">image of barcode to decode</param>
        /// <returns>
        ///     String which the barcode encodes
        /// </returns>
        public Result decode(BinaryBitmap image)
        {
            return _delegate.decode(image);
        }

        /// <summary>
        ///     Locates and decodes a barcode in some format within an image. This method also accepts
        ///     hints, each possibly associated to some data, which may help the implementation decode.
        /// </summary>
        /// <param name="image">image of barcode to decode</param>
        /// <param name="hints">
        ///     passed as a <see cref="IDictionary{TKey, TValue}" /> from <see cref="DecodeHintType" />
        ///     to arbitrary data. The
        ///     meaning of the data depends upon the hint type. The implementation may or may not do
        ///     anything with these hints.
        /// </param>
        /// <returns>
        ///     String which the barcode encodes
        /// </returns>
        public Result decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
        {
            return _delegate.decode(image, hints);
        }

        /// <summary>
        ///     Resets any internal state the implementation has after a decode, to prepare it
        ///     for reuse.
        /// </summary>
        public void reset()
        {
            _delegate.reset();
        }
    }
}

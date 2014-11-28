using System.Collections.Generic;

namespace ZXing.Multi
{
    /// <summary>
    ///     This class attempts to decode a barcode from an image, not by scanning the whole image,
    ///     but by scanning subsets of the image. This is important when there may be multiple barcodes in
    ///     an image, and detecting a barcode may find parts of multiple barcode and fail to decode
    ///     (e.g. QR Codes). Instead this scans the four quadrants of the image -- and also the center
    ///     'quadrant' to cover the case where a barcode is found in the center.
    /// </summary>
    /// <seealso cref="GenericMultipleBarcodeReader" />
    public sealed class ByQuadrantReader : Reader
    {
        private readonly Reader @delegate;

        public ByQuadrantReader(Reader @delegate) { this.@delegate = @delegate; }

        public Result decode(BinaryBitmap image) { return decode(image, null); }

        public Result decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
        {
            var width = image.Width;
            var height = image.Height;
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            var topLeft = image.crop(0, 0, halfWidth, halfHeight);
            var result = @delegate.decode(topLeft, hints);
            if (result != null)
                return result;

            var topRight = image.crop(halfWidth, 0, halfWidth, halfHeight);
            result = @delegate.decode(topRight, hints);
            if (result != null)
                return result;

            var bottomLeft = image.crop(0, halfHeight, halfWidth, halfHeight);
            result = @delegate.decode(bottomLeft, hints);
            if (result != null)
                return result;

            var bottomRight = image.crop(halfWidth, halfHeight, halfWidth, halfHeight);
            result = @delegate.decode(bottomRight, hints);
            if (result != null)
                return result;

            var quarterWidth = halfWidth / 2;
            var quarterHeight = halfHeight / 2;
            var center = image.crop(quarterWidth, quarterHeight, halfWidth, halfHeight);
            return @delegate.decode(center, hints);
        }

        public void reset() { @delegate.reset(); }
    }
}

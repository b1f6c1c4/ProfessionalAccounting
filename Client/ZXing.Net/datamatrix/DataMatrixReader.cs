using System.Collections.Generic;
using ZXing.Common;
using ZXing.Datamatrix.Internal;

namespace ZXing.Datamatrix
{
    /// <summary>
    ///     This implementation can detect and decode Data Matrix codes in an image.
    ///     <author>bbrown@google.com (Brian Brown)</author>
    /// </summary>
    public sealed class DataMatrixReader : Reader
    {
        private static readonly ResultPoint[] NO_POINTS = new ResultPoint[0];

        private readonly Decoder decoder = new Decoder();

        /// <summary>
        ///     Locates and decodes a Data Matrix code in an image.
        ///     <returns>a String representing the content encoded by the Data Matrix code</returns>
        ///     <exception cref="FormatException">if a Data Matrix code cannot be decoded</exception>
        /// </summary>
        public Result decode(BinaryBitmap image)
        {
            return decode(image, null);
        }

        public Result decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints)
        {
            DecoderResult decoderResult;
            ResultPoint[] points;
            if (hints != null &&
                hints.ContainsKey(DecodeHintType.PURE_BARCODE))
            {
                var bits = extractPureBits(image.BlackMatrix);
                if (bits == null)
                    return null;
                decoderResult = decoder.decode(bits);
                points = NO_POINTS;
            }
            else
            {
                var detectorResult = new Detector(image.BlackMatrix).detect();
                if (detectorResult == null)
                    return null;
                decoderResult = decoder.decode(detectorResult.Bits);
                points = detectorResult.Points;
            }
            if (decoderResult == null)
                return null;

            var result = new Result(
                decoderResult.Text,
                decoderResult.RawBytes,
                points,
                BarcodeFormat.DATA_MATRIX);
            var byteSegments = decoderResult.ByteSegments;
            if (byteSegments != null)
                result.putMetadata(ResultMetadataType.BYTE_SEGMENTS, byteSegments);
            var ecLevel = decoderResult.ECLevel;
            if (ecLevel != null)
                result.putMetadata(ResultMetadataType.ERROR_CORRECTION_LEVEL, ecLevel);
            return result;
        }

        public void reset()
        {
            // do nothing
        }

        /// <summary>
        ///     This method detects a code in a "pure" image -- that is, pure monochrome image
        ///     which contains only an unrotated, unskewed, image of a code, with some white border
        ///     around it. This is a specialized method that works exceptionally fast in this special
        ///     case.
        ///     <seealso cref="ZXing.QrCode.QRCodeReader.extractPureBits(BitMatrix)" />
        /// </summary>
        private static BitMatrix extractPureBits(BitMatrix image)
        {
            var leftTopBlack = image.getTopLeftOnBit();
            var rightBottomBlack = image.getBottomRightOnBit();
            if (leftTopBlack == null ||
                rightBottomBlack == null)
                return null;

            int moduleSize;
            if (!DataMatrixReader.moduleSize(leftTopBlack, image, out moduleSize))
                return null;

            var top = leftTopBlack[1];
            var bottom = rightBottomBlack[1];
            var left = leftTopBlack[0];
            var right = rightBottomBlack[0];

            var matrixWidth = (right - left + 1) / moduleSize;
            var matrixHeight = (bottom - top + 1) / moduleSize;
            if (matrixWidth <= 0 ||
                matrixHeight <= 0)
                return null;

            // Push in the "border" by half the module width so that we start
            // sampling in the middle of the module. Just in case the image is a
            // little off, this will help recover.
            var nudge = moduleSize >> 1;
            top += nudge;
            left += nudge;

            // Now just read off the bits
            var bits = new BitMatrix(matrixWidth, matrixHeight);
            for (var y = 0; y < matrixHeight; y++)
            {
                var iOffset = top + y * moduleSize;
                for (var x = 0; x < matrixWidth; x++)
                    if (image[left + x * moduleSize, iOffset])
                        bits[x, y] = true;
            }
            return bits;
        }

        private static bool moduleSize(int[] leftTopBlack, BitMatrix image, out int modulesize)
        {
            var width = image.Width;
            var x = leftTopBlack[0];
            var y = leftTopBlack[1];
            while (x < width &&
                   image[x, y])
                x++;
            if (x == width)
            {
                modulesize = 0;
                return false;
            }

            modulesize = x - leftTopBlack[0];
            if (modulesize == 0)
                return false;
            return true;
        }
    }
}

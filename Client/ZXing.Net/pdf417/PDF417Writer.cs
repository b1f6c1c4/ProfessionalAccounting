using System;
using System.Collections.Generic;
using ZXing.Common;
using ZXing.PDF417.Internal;

namespace ZXing.PDF417
{
    /// <summary>
    ///     <author>Jacob Haynes</author>
    ///     <author>qwandor@google.com (Andrew Walbran)</author>
    /// </summary>
    public sealed class PDF417Writer : Writer
    {
        /// <summary>
        ///     default white space (margin) around the code
        /// </summary>
        private const int WHITE_SPACE = 30;

        /// <summary>
        /// </summary>
        /// <param name="contents">The contents to encode in the barcode</param>
        /// <param name="format">The barcode format to generate</param>
        /// <param name="width">The preferred width in pixels</param>
        /// <param name="height">The preferred height in pixels</param>
        /// <param name="hints">Additional parameters to supply to the encoder</param>
        /// <returns>
        ///     The generated barcode as a Matrix of unsigned bytes (0 == black, 255 == white)
        /// </returns>
        public BitMatrix encode(String contents,
                                BarcodeFormat format,
                                int width,
                                int height,
                                IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.PDF_417)
                throw new ArgumentException("Can only encode PDF_417, but got " + format);

            var encoder = new Internal.PDF417();
            var margin = WHITE_SPACE;
            var errorCorrectionLevel = 2;

            if (hints != null)
            {
                if (hints.ContainsKey(EncodeHintType.PDF417_COMPACT))
                    encoder.setCompact((Boolean)hints[EncodeHintType.PDF417_COMPACT]);
                if (hints.ContainsKey(EncodeHintType.PDF417_COMPACTION))
                    encoder.setCompaction((Compaction)hints[EncodeHintType.PDF417_COMPACTION]);
                if (hints.ContainsKey(EncodeHintType.PDF417_DIMENSIONS))
                {
                    var dimensions = (Dimensions)hints[EncodeHintType.PDF417_DIMENSIONS];
                    encoder.setDimensions(
                                          dimensions.MaxCols,
                                          dimensions.MinCols,
                                          dimensions.MaxRows,
                                          dimensions.MinRows);
                }
                if (hints.ContainsKey(EncodeHintType.MARGIN))
                    margin = (int)(hints[EncodeHintType.MARGIN]);
                if (hints.ContainsKey(EncodeHintType.ERROR_CORRECTION))
                {
                    var value = hints[EncodeHintType.ERROR_CORRECTION];
                    if (value is PDF417ErrorCorrectionLevel ||
                        value is int)
                        errorCorrectionLevel = (int)value;
                }
                if (hints.ContainsKey(EncodeHintType.CHARACTER_SET))
                {
#if !SILVERLIGHT || WINDOWS_PHONE
                    var encoding = (String)hints[EncodeHintType.CHARACTER_SET];
                    if (encoding != null)
                        encoder.setEncoding(encoding);
#else
    // Silverlight supports only UTF-8 and UTF-16 out-of-the-box
               encoder.setEncoding("UTF-8");
#endif
                }
                if (hints.ContainsKey(EncodeHintType.DISABLE_ECI))
                    encoder.setDisableEci((bool)hints[EncodeHintType.DISABLE_ECI]);
            }

            return bitMatrixFromEncoder(encoder, contents, width, height, margin, errorCorrectionLevel);
        }

        /// <summary>
        ///     Encode a barcode using the default settings.
        /// </summary>
        /// <param name="contents">The contents to encode in the barcode</param>
        /// <param name="format">The barcode format to generate</param>
        /// <param name="width">The preferred width in pixels</param>
        /// <param name="height">The preferred height in pixels</param>
        /// <returns>
        ///     The generated barcode as a Matrix of unsigned bytes (0 == black, 255 == white)
        /// </returns>
        public BitMatrix encode(String contents,
                                BarcodeFormat format,
                                int width,
                                int height)
        {
            return encode(contents, format, width, height, null);
        }

        /// <summary>
        ///     Takes encoder, accounts for width/height, and retrieves bit matrix
        /// </summary>
        private static BitMatrix bitMatrixFromEncoder(Internal.PDF417 encoder,
                                                      String contents,
                                                      int width,
                                                      int height,
                                                      int margin,
                                                      int errorCorrectionLevel)
        {
            encoder.generateBarcodeLogic(contents, errorCorrectionLevel);

            const int lineThickness = 2;
            const int aspectRatio = 4;
            var originalScale = encoder.BarcodeMatrix.getScaledMatrix(lineThickness, aspectRatio * lineThickness);
            var rotated = false;
            if ((height > width) ^ (originalScale[0].Length < originalScale.Length))
            {
                originalScale = rotateArray(originalScale);
                rotated = true;
            }

            var scaleX = width / originalScale[0].Length;
            var scaleY = height / originalScale.Length;

            int scale;
            if (scaleX < scaleY)
                scale = scaleX;
            else
                scale = scaleY;

            if (scale > 1)
            {
                var scaledMatrix =
                    encoder.BarcodeMatrix.getScaledMatrix(scale * lineThickness, scale * aspectRatio * lineThickness);
                if (rotated)
                    scaledMatrix = rotateArray(scaledMatrix);
                return bitMatrixFrombitArray(scaledMatrix, margin);
            }
            return bitMatrixFrombitArray(originalScale, margin);
        }

        /// <summary>
        ///     This takes an array holding the values of the PDF 417
        /// </summary>
        /// <param name="input">a byte array of information with 0 is black, and 1 is white</param>
        /// <param name="margin">border around the barcode</param>
        /// <returns>BitMatrix of the input</returns>
        private static BitMatrix bitMatrixFrombitArray(sbyte[][] input, int margin)
        {
            // Creates the bitmatrix with extra space for whitespace
            var output = new BitMatrix(input[0].Length + 2 * margin, input.Length + 2 * margin);
            var yOutput = output.Height - margin - 1;
            for (var y = 0; y < input.Length; y++)
            {
                var currentInput = input[y];
                var currentInputLength = currentInput.Length;
                for (var x = 0; x < currentInputLength; x++)
                    // Zero is white in the bytematrix
                    if (currentInput[x] == 1)
                        output[x + margin, yOutput] = true;
                yOutput--;
            }
            return output;
        }

        /// <summary>
        ///     Takes and rotates the it 90 degrees
        /// </summary>
        private static sbyte[][] rotateArray(sbyte[][] bitarray)
        {
            var temp = new sbyte[bitarray[0].Length][];
            for (var idx = 0; idx < bitarray[0].Length; idx++)
                temp[idx] = new sbyte[bitarray.Length];
            for (var ii = 0; ii < bitarray.Length; ii++)
            {
                // This makes the direction consistent on screen when rotating the
                // screen;
                var inverseii = bitarray.Length - ii - 1;
                for (var jj = 0; jj < bitarray[0].Length; jj++)
                    temp[jj][inverseii] = bitarray[ii][jj];
            }
            return temp;
        }
    }
}

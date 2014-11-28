using System;
using System.Collections.Generic;
using ZXing.Common;
using ZXing.QrCode.Internal;

namespace ZXing.QrCode
{
    /// <summary>
    ///     This object renders a QR Code as a BitMatrix 2D array of greyscale values.
    ///     <author>dswitkin@google.com (Daniel Switkin)</author>
    /// </summary>
    public sealed class QRCodeWriter : Writer
    {
        private const int QUIET_ZONE_SIZE = 4;

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
        public BitMatrix encode(String contents, BarcodeFormat format, int width, int height)
        {
            return encode(contents, format, width, height, null);
        }

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
            if (String.IsNullOrEmpty(contents))
                throw new ArgumentException("Found empty contents");

            if (format != BarcodeFormat.QR_CODE)
                throw new ArgumentException("Can only encode QR_CODE, but got " + format);

            if (width < 0 ||
                height < 0)
                throw new ArgumentException("Requested dimensions are too small: " + width + 'x' + height);

            var errorCorrectionLevel = ErrorCorrectionLevel.L;
            var quietZone = QUIET_ZONE_SIZE;
            if (hints != null)
            {
                var requestedECLevel = hints.ContainsKey(EncodeHintType.ERROR_CORRECTION)
                                           ? (ErrorCorrectionLevel)hints[EncodeHintType.ERROR_CORRECTION]
                                           : null;
                if (requestedECLevel != null)
                    errorCorrectionLevel = requestedECLevel;
                var quietZoneInt = hints.ContainsKey(EncodeHintType.MARGIN)
                                       ? (int)hints[EncodeHintType.MARGIN]
                                       : (int?)null;
                if (quietZoneInt != null)
                    quietZone = quietZoneInt.Value;
            }

            var code = Encoder.encode(contents, errorCorrectionLevel, hints);
            return renderResult(code, width, height, quietZone);
        }

        // Note that the input matrix uses 0 == white, 1 == black, while the output matrix uses
        // 0 == black, 255 == white (i.e. an 8 bit greyscale bitmap).
        private static BitMatrix renderResult(QRCode code, int width, int height, int quietZone)
        {
            var input = code.Matrix;
            if (input == null)
                throw new InvalidOperationException();
            var inputWidth = input.Width;
            var inputHeight = input.Height;
            var qrWidth = inputWidth + (quietZone << 1);
            var qrHeight = inputHeight + (quietZone << 1);
            var outputWidth = Math.Max(width, qrWidth);
            var outputHeight = Math.Max(height, qrHeight);

            var multiple = Math.Min(outputWidth / qrWidth, outputHeight / qrHeight);
            // Padding includes both the quiet zone and the extra white pixels to accommodate the requested
            // dimensions. For example, if input is 25x25 the QR will be 33x33 including the quiet zone.
            // If the requested size is 200x160, the multiple will be 4, for a QR of 132x132. These will
            // handle all the padding from 100x100 (the actual QR) up to 200x160.
            var leftPadding = (outputWidth - (inputWidth * multiple)) / 2;
            var topPadding = (outputHeight - (inputHeight * multiple)) / 2;

            var output = new BitMatrix(outputWidth, outputHeight);

            for (int inputY = 0, outputY = topPadding; inputY < inputHeight; inputY++, outputY += multiple)
                // Write the contents of this row of the barcode
                for (int inputX = 0, outputX = leftPadding; inputX < inputWidth; inputX++, outputX += multiple)
                    if (input[inputX, inputY] == 1)
                        output.setRegion(outputX, outputY, multiple, multiple);

            return output;
        }
    }
}

using System;
using System.Collections.Generic;
using ZXing.Common;

namespace ZXing
{
    /// <summary>
    ///     The base class for all objects which encode/generate a barcode image.
    /// </summary>
    /// <author>
    ///     dswitkin@google.com (Daniel Switkin)
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public interface Writer
    {
        /// <summary>
        ///     Encode a barcode using the default settings.
        /// </summary>
        /// <param name="contents">The contents to encode in the barcode</param>
        /// <param name="format">The barcode format to generate</param>
        /// <param name="width">The preferred width in pixels</param>
        /// <param name="height">The preferred height in pixels</param>
        /// <returns> The generated barcode as a Matrix of unsigned bytes (0 == black, 255 == white)</returns>
        BitMatrix encode(String contents, BarcodeFormat format, int width, int height);

        /// <summary> </summary>
        /// <param name="contents">The contents to encode in the barcode</param>
        /// <param name="format">The barcode format to generate</param>
        /// <param name="width">The preferred width in pixels</param>
        /// <param name="height">The preferred height in pixels</param>
        /// <param name="hints">Additional parameters to supply to the encoder</param>
        /// <returns> The generated barcode as a Matrix of unsigned bytes (0 == black, 255 == white)</returns>
        BitMatrix encode(String contents, BarcodeFormat format, int width, int height,
                         IDictionary<EncodeHintType, object> hints);
    }
}

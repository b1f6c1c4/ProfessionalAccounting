using System.Collections.Generic;

namespace ZXing
{
    /// <summary>
    ///     Implementations of this interface can decode an image of a barcode in some format into
    ///     the String it encodes. For example, <see cref="ZXing.QrCode.QRCodeReader" /> can
    ///     decode a QR code. The decoder may optionally receive hints from the caller which may help
    ///     it decode more quickly or accurately.
    ///     See <see cref="MultiFormatReader" />, which attempts to determine what barcode
    ///     format is present within the image as well, and then decodes it accordingly.
    /// </summary>
    /// <author>Sean Owen</author>
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    public interface Reader
    {
        /// <summary>
        ///     Locates and decodes a barcode in some format within an image.
        /// </summary>
        /// <param name="image">image of barcode to decode</param>
        /// <returns>String which the barcode encodes</returns>
        Result decode(BinaryBitmap image);

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
        /// <returns>String which the barcode encodes</returns>
        Result decode(BinaryBitmap image, IDictionary<DecodeHintType, object> hints);

        /// <summary>
        ///     Resets any internal state the implementation has after a decode, to prepare it
        ///     for reuse.
        /// </summary>
        void reset();
    }
}

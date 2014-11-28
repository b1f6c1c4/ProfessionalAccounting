using System.Collections.Generic;

namespace ZXing.Multi
{
    /// <summary>
    ///     Implementation of this interface attempt to read several barcodes from one image.
    ///     <author>Sean Owen</author>
    ///     <seealso cref="Reader" />
    /// </summary>
    public interface MultipleBarcodeReader
    {
        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns></returns>
        Result[] decodeMultiple(BinaryBitmap image);

        /// <summary>
        ///     Decodes the multiple.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="hints">The hints.</param>
        /// <returns></returns>
        Result[] decodeMultiple(BinaryBitmap image, IDictionary<DecodeHintType, object> hints);
    }
}

using ZXing.Common;

namespace ZXing
{
#if !(WINDOWS_PHONE || WindowsCE)
    /// <summary>
    ///     Interface for a smart class to encode some content into a barcode
    /// </summary>
    public interface IBarcodeWriterGeneric<out TOutput>
#else
    /// <summary>
    /// Interface for a smart class to encode some content into a barcode
    /// </summary>
   public interface IBarcodeWriterGeneric<TOutput>
#endif
    {
        /// <summary>
        ///     Encodes the specified contents.
        /// </summary>
        /// <param name="contents">The contents.</param>
        /// <returns></returns>
        BitMatrix Encode(string contents);

        /// <summary>
        ///     Creates a visual representation of the contents
        /// </summary>
        /// <param name="contents">The contents.</param>
        /// <returns></returns>
        TOutput Write(string contents);

        /// <summary>
        ///     Returns a rendered instance of the barcode which is given by a BitMatrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns></returns>
        TOutput Write(BitMatrix matrix);
    }
}

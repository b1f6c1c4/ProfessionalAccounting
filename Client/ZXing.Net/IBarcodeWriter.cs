using ZXing.Common;
#if MONOTOUCH
#if __UNIFIED__
using UIKit;
#else
using MonoTouch.UIKit;

#endif
#endif

namespace ZXing
{
    /// <summary>
    ///     Interface for a smart class to encode some content into a barcode
    /// </summary>
    public interface IBarcodeWriter
    {
        /// <summary>
        ///     Encodes the specified contents.
        /// </summary>
        /// <param name="contents">The contents.</param>
        /// <returns></returns>
        BitMatrix Encode(string contents);

#if MONOTOUCH
        /// <summary>
        ///     Creates a visual representation of the contents
        /// </summary>
        UIImage Write(string contents);

        /// <summary>
        ///     Returns a rendered instance of the barcode which is given by a BitMatrix.
        /// </summary>
        UIImage Write(BitMatrix matrix);
#endif

#if MONOANDROID
    /// <summary>
    /// Creates a visual representation of the contents
    /// </summary>
      Android.Graphics.Bitmap Write(string contents);
      /// <summary>
      /// Returns a rendered instance of the barcode which is given by a BitMatrix.
      /// </summary>
      Android.Graphics.Bitmap Write(BitMatrix matrix);
#endif

#if UNITY
    /// <summary>
    /// Creates a visual representation of the contents
    /// </summary>
      UnityEngine.Color32[] Write(string contents);
      /// <summary>
      /// Returns a rendered instance of the barcode which is given by a BitMatrix.
      /// </summary>
      UnityEngine.Color32[] Write(BitMatrix matrix);
#endif

#if SILVERLIGHT
    /// <summary>
    /// Creates a visual representation of the contents
    /// </summary>
      System.Windows.Media.Imaging.WriteableBitmap Write(string contents);
      /// <summary>
      /// Returns a rendered instance of the barcode which is given by a BitMatrix.
      /// </summary>
      System.Windows.Media.Imaging.WriteableBitmap Write(BitMatrix matrix);
#endif

#if NETFX_CORE
    /// <summary>
    /// Creates a visual representation of the contents
    /// </summary>
      Windows.UI.Xaml.Media.Imaging.WriteableBitmap Write(string contents);
      /// <summary>
      /// Returns a rendered instance of the barcode which is given by a BitMatrix.
      /// </summary>
      Windows.UI.Xaml.Media.Imaging.WriteableBitmap Write(BitMatrix matrix);
#endif

#if (NET40 || NET35 || NET20) && !UNITY
    /// <summary>
    /// Creates a visual representation of the contents
    /// </summary>
      System.Drawing.Bitmap Write(string contents);
      /// <summary>
      /// Returns a rendered instance of the barcode which is given by a BitMatrix.
      /// </summary>
      System.Drawing.Bitmap Write(BitMatrix matrix);
#endif
    }
}

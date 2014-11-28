using ZXing.Rendering;
#if MONOTOUCH
#if __UNIFIED__
using UIKit;
#else
using MonoTouch.UIKit;

#endif
#endif

namespace ZXing
{
#if MONOTOUCH
    /// <summary>
    ///     A smart class to encode some content to a barcode image
    /// </summary>
    public class BarcodeWriter : BarcodeWriterGeneric<UIImage>, IBarcodeWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BarcodeWriter" /> class.
        /// </summary>
        public BarcodeWriter() { Renderer = new BitmapRenderer(); }
    }
#endif

#if MONOANDROID
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<Android.Graphics.Bitmap>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new BitmapRenderer();
      }
   }
#endif

#if UNITY
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<UnityEngine.Color32[]>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new Color32Renderer();
      }
   }
#endif

#if SILVERLIGHT
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<System.Windows.Media.Imaging.WriteableBitmap>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new WriteableBitmapRenderer();
      }
   }
#endif

#if NETFX_CORE
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<Windows.UI.Xaml.Media.Imaging.WriteableBitmap>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new WriteableBitmapRenderer();
      }
   }
#endif

#if (NET45 || NET40 || NET35 || NET20 || WindowsCE) && !UNITY
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<System.Drawing.Bitmap>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new BitmapRenderer();
      }
   }
#endif

#if PORTABLE
    /// <summary>
    /// A smart class to encode some content to a barcode image
    /// </summary>
   public class BarcodeWriter : BarcodeWriterGeneric<byte[]>, IBarcodeWriter
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="BarcodeWriter"/> class.
      /// </summary>
      public BarcodeWriter()
      {
         Renderer = new RawRenderer();
      }
   }
#endif

    /// <summary>
    ///     A smart class to encode some content to a svg barcode image
    /// </summary>
    public class BarcodeWriterSvg : BarcodeWriterGeneric<SvgRenderer.SvgImage>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BarcodeWriter" /> class.
        /// </summary>
        public BarcodeWriterSvg() { Renderer = new SvgRenderer(); }
    }
}

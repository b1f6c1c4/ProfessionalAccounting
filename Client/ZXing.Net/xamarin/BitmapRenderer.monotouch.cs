using ZXing.Common;
#if __UNIFIED__
using Foundation;
using CoreFoundation;
using CoreGraphics;
using UIKit;
#else
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
using CGRect = System.Drawing.RectangleF;
using CGPoint = System.Drawing.PointF;
using CGSize = System.Drawing.SizeF;
using nfloat = System.Single;
using nint = System.Int32;
using nuint = System.UInt32;
#endif

namespace ZXing.Rendering
{
    public class BitmapRenderer : IBarcodeRenderer<UIImage>
    {
        public UIImage Render(BitMatrix matrix, BarcodeFormat format, string content)
        {
            return Render(matrix, format, content, new EncodingOptions());
        }

        public UIImage Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
        {
            UIGraphics.BeginImageContext(new CGSize(matrix.Width, matrix.Height));
            var context = UIGraphics.GetCurrentContext();

            var black = new CGColor(0f, 0f, 0f);
            var white = new CGColor(1.0f, 1.0f, 1.0f);

            for (var x = 0; x < matrix.Width; x++)
                for (var y = 0; y < matrix.Height; y++)
                {
                    context.SetFillColor(matrix[x, y] ? black : white);
                    context.FillRect(new CGRect(x, y, 1, 1));
                }


            var img = UIGraphics.GetImageFromCurrentImageContext();

            UIGraphics.EndImageContext();

            return img;
        }
    }
}

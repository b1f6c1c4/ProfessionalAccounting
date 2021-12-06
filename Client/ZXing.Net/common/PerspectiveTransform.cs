namespace ZXing.Common
{
    /// <summary>
    ///     <p>
    ///         This class implements a perspective transform in two dimensions. Given four source and four
    ///         destination points, it will compute the transformation implied between them. The code is based
    ///         directly upon section 3.4.2 of George Wolberg's "Digital Image Warping"; see pages 54-56.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class PerspectiveTransform
    {
        private readonly float a11;
        private readonly float a12;
        private readonly float a13;
        private readonly float a21;
        private readonly float a22;
        private readonly float a23;
        private readonly float a31;
        private readonly float a32;
        private readonly float a33;

        private PerspectiveTransform(float a11, float a21, float a31, float a12, float a22, float a32, float a13,
                                     float a23, float a33)
        {
            this.a11 = a11;
            this.a12 = a12;
            this.a13 = a13;
            this.a21 = a21;
            this.a22 = a22;
            this.a23 = a23;
            this.a31 = a31;
            this.a32 = a32;
            this.a33 = a33;
        }

        public static PerspectiveTransform quadrilateralToQuadrilateral(float x0, float y0, float x1, float y1, float x2,
                                                                        float y2, float x3, float y3, float x0p,
                                                                        float y0p, float x1p, float y1p, float x2p,
                                                                        float y2p, float x3p, float y3p)
        {
            var qToS = quadrilateralToSquare(x0, y0, x1, y1, x2, y2, x3, y3);
            var sToQ = squareToQuadrilateral(x0p, y0p, x1p, y1p, x2p, y2p, x3p, y3p);
            return sToQ.times(qToS);
        }

        public void transformPoints(float[] points)
        {
            var max = points.Length;
            var a11 = this.a11;
            var a12 = this.a12;
            var a13 = this.a13;
            var a21 = this.a21;
            var a22 = this.a22;
            var a23 = this.a23;
            var a31 = this.a31;
            var a32 = this.a32;
            var a33 = this.a33;
            for (var i = 0; i < max; i += 2)
            {
                var x = points[i];
                var y = points[i + 1];
                var denominator = a13 * x + a23 * y + a33;
                points[i] = (a11 * x + a21 * y + a31) / denominator;
                points[i + 1] = (a12 * x + a22 * y + a32) / denominator;
            }
        }

        /// <summary>Convenience method, not optimized for performance. </summary>
        public void transformPoints(float[] xValues, float[] yValues)
        {
            var n = xValues.Length;
            for (var i = 0; i < n; i++)
            {
                var x = xValues[i];
                var y = yValues[i];
                var denominator = a13 * x + a23 * y + a33;
                xValues[i] = (a11 * x + a21 * y + a31) / denominator;
                yValues[i] = (a12 * x + a22 * y + a32) / denominator;
            }
        }

        public static PerspectiveTransform squareToQuadrilateral(float x0, float y0,
                                                                 float x1, float y1,
                                                                 float x2, float y2,
                                                                 float x3, float y3)
        {
            var dx3 = x0 - x1 + x2 - x3;
            var dy3 = y0 - y1 + y2 - y3;
            if (dx3 == 0.0f &&
                dy3 == 0.0f)
                // Affine
                return new PerspectiveTransform(
                    x1 - x0,
                    x2 - x1,
                    x0,
                    y1 - y0,
                    y2 - y1,
                    y0,
                    0.0f,
                    0.0f,
                    1.0f);
            var dx1 = x1 - x2;
            var dx2 = x3 - x2;
            var dy1 = y1 - y2;
            var dy2 = y3 - y2;
            var denominator = dx1 * dy2 - dx2 * dy1;
            var a13 = (dx3 * dy2 - dx2 * dy3) / denominator;
            var a23 = (dx1 * dy3 - dx3 * dy1) / denominator;
            return new PerspectiveTransform(
                x1 - x0 + a13 * x1,
                x3 - x0 + a23 * x3,
                x0,
                y1 - y0 + a13 * y1,
                y3 - y0 + a23 * y3,
                y0,
                a13,
                a23,
                1.0f);
        }

        public static PerspectiveTransform quadrilateralToSquare(float x0, float y0, float x1, float y1, float x2,
                                                                 float y2, float x3, float y3)
        {
            // Here, the adjoint serves as the inverse:
            return squareToQuadrilateral(x0, y0, x1, y1, x2, y2, x3, y3).buildAdjoint();
        }

        internal PerspectiveTransform buildAdjoint()
        {
            // Adjoint is the transpose of the cofactor matrix:
            return new PerspectiveTransform(
                a22 * a33 - a23 * a32,
                a23 * a31 - a21 * a33,
                a21 * a32 - a22 * a31,
                a13 * a32 - a12 * a33,
                a11 * a33 - a13 * a31,
                a12 * a31 - a11 * a32,
                a12 * a23 - a13 * a22,
                a13 * a21 - a11 * a23,
                a11 * a22 - a12 * a21);
        }

        internal PerspectiveTransform times(PerspectiveTransform other)
        {
            return new PerspectiveTransform(
                a11 * other.a11 + a21 * other.a12 + a31 * other.a13,
                a11 * other.a21 + a21 * other.a22 + a31 * other.a23,
                a11 * other.a31 + a21 * other.a32 + a31 * other.a33,
                a12 * other.a11 + a22 * other.a12 + a32 * other.a13,
                a12 * other.a21 + a22 * other.a22 + a32 * other.a23,
                a12 * other.a31 + a22 * other.a32 + a32 * other.a33,
                a13 * other.a11 + a23 * other.a12 + a33 * other.a13,
                a13 * other.a21 + a23 * other.a22 + a33 * other.a23,
                a13 * other.a31 + a23 * other.a32 + a33 * other.a33);
        }
    }
}

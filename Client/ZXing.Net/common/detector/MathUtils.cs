using System;

namespace ZXing.Common.Detector
{
    public static class MathUtils
    {
        /// <summary>
        ///     Ends up being a bit faster than {@link Math#round(float)}. This merely rounds its
        ///     argument to the nearest int, where x.5 rounds up to x+1. Semantics of this shortcut
        ///     differ slightly from {@link Math#round(float)} in that half rounds down for negative
        ///     values. -2.5 rounds to -3, not -2. For purposes here it makes no difference.
        /// </summary>
        /// <param name="d">real value to round</param>
        /// <returns>nearest <c>int</c></returns>
        public static int round(float d)
        {
            if (float.IsNaN(d))
                return 0;
            if (float.IsPositiveInfinity(d))
                return int.MaxValue;
            return (int)(d + (d < 0.0f ? -0.5f : 0.5f));
        }

        public static float distance(float aX, float aY, float bX, float bY)
        {
            var xDiff = aX - bX;
            var yDiff = aY - bY;
            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }

        public static float distance(int aX, int aY, int bX, int bY)
        {
            var xDiff = aX - bX;
            var yDiff = aY - bY;
            return (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff);
        }
    }
}

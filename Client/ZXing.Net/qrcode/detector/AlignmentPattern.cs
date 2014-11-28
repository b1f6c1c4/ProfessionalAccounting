using System;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         Encapsulates an alignment pattern, which are the smaller square patterns found in
    ///         all but the simplest QR Codes.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class AlignmentPattern : ResultPoint
    {
        private readonly float estimatedModuleSize;

        internal AlignmentPattern(float posX, float posY, float estimatedModuleSize)
            : base(posX, posY) { this.estimatedModuleSize = estimatedModuleSize; }

        /// <summary>
        ///     <p>
        ///         Determines if this alignment pattern "about equals" an alignment pattern at the stated
        ///         position and size -- meaning, it is at nearly the same center with nearly the same size.
        ///     </p>
        /// </summary>
        internal bool aboutEquals(float moduleSize, float i, float j)
        {
            if (Math.Abs(i - Y) <= moduleSize &&
                Math.Abs(j - X) <= moduleSize)
            {
                var moduleSizeDiff = Math.Abs(moduleSize - estimatedModuleSize);
                return moduleSizeDiff <= 1.0f || moduleSizeDiff <= estimatedModuleSize;
            }
            return false;
        }

        /// <summary>
        ///     Combines this object's current estimate of a finder pattern position and module size
        ///     with a new estimate. It returns a new {@code FinderPattern} containing an average of the two.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <param name="newModuleSize">New size of the module.</param>
        /// <returns></returns>
        internal AlignmentPattern combineEstimate(float i, float j, float newModuleSize)
        {
            var combinedX = (X + j) / 2.0f;
            var combinedY = (Y + i) / 2.0f;
            var combinedModuleSize = (estimatedModuleSize + newModuleSize) / 2.0f;
            return new AlignmentPattern(combinedX, combinedY, combinedModuleSize);
        }
    }
}

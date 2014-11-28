using System;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         Encapsulates a finder pattern, which are the three square patterns found in
    ///         the corners of QR Codes. It also encapsulates a count of similar finder patterns,
    ///         as a convenience to the finder's bookkeeping.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public sealed class FinderPattern : ResultPoint
    {
        private readonly float estimatedModuleSize;
        private readonly int count;

        internal FinderPattern(float posX, float posY, float estimatedModuleSize)
            : this(posX, posY, estimatedModuleSize, 1)
        {
            this.estimatedModuleSize = estimatedModuleSize;
            count = 1;
        }

        internal FinderPattern(float posX, float posY, float estimatedModuleSize, int count)
            : base(posX, posY)
        {
            this.estimatedModuleSize = estimatedModuleSize;
            this.count = count;
        }

        /// <summary>
        ///     Gets the size of the estimated module.
        /// </summary>
        /// <value>
        ///     The size of the estimated module.
        /// </value>
        public float EstimatedModuleSize { get { return estimatedModuleSize; } }

        internal int Count { get { return count; } }

        /*
      internal void incrementCount()
      {
         this.count++;
      }
      */

        /// <summary>
        ///     <p>
        ///         Determines if this finder pattern "about equals" a finder pattern at the stated
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
        ///     with a new estimate. It returns a new {@code FinderPattern} containing a weighted average
        ///     based on count.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <param name="newModuleSize">New size of the module.</param>
        /// <returns></returns>
        internal FinderPattern combineEstimate(float i, float j, float newModuleSize)
        {
            var combinedCount = count + 1;
            var combinedX = (count * X + j) / combinedCount;
            var combinedY = (count * Y + i) / combinedCount;
            var combinedModuleSize = (count * estimatedModuleSize + newModuleSize) / combinedCount;
            return new FinderPattern(combinedX, combinedY, combinedModuleSize, combinedCount);
        }
    }
}

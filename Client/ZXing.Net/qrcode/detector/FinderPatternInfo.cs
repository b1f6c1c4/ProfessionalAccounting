namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         Encapsulates information about finder patterns in an image, including the location of
    ///         the three finder patterns, and their estimated module size.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public sealed class FinderPatternInfo
    {
        private readonly FinderPattern bottomLeft;
        private readonly FinderPattern topLeft;
        private readonly FinderPattern topRight;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FinderPatternInfo" /> class.
        /// </summary>
        /// <param name="patternCenters">The pattern centers.</param>
        public FinderPatternInfo(FinderPattern[] patternCenters)
        {
            bottomLeft = patternCenters[0];
            topLeft = patternCenters[1];
            topRight = patternCenters[2];
        }

        /// <summary>
        ///     Gets the bottom left.
        /// </summary>
        public FinderPattern BottomLeft { get { return bottomLeft; } }

        /// <summary>
        ///     Gets the top left.
        /// </summary>
        public FinderPattern TopLeft { get { return topLeft; } }

        /// <summary>
        ///     Gets the top right.
        /// </summary>
        public FinderPattern TopRight { get { return topRight; } }
    }
}

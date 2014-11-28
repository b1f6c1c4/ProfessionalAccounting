using ZXing.Common;

namespace ZXing.Aztec.Internal
{
    /// <summary>
    ///     Aztec 2D code representation
    /// </summary>
    /// <author>Rustam Abdullaev</author>
    public sealed class AztecCode
    {
        /// <summary>
        ///     Compact or full symbol indicator
        /// </summary>
        public bool isCompact { get; set; }

        /// <summary>
        ///     Size in pixels (width and height)
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///     Number of levels
        /// </summary>
        public int Layers { get; set; }

        /// <summary>
        ///     Number of data codewords
        /// </summary>
        public int CodeWords { get; set; }

        /// <summary>
        ///     The symbol image
        /// </summary>
        public BitMatrix Matrix { get; set; }
    }
}

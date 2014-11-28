using ZXing.Common;

namespace ZXing.Aztec.Internal
{
    /// <summary>
    ///     The class contains all information about the Aztec code which was found
    /// </summary>
    public class AztecDetectorResult : DetectorResult
    {
        /// <summary>
        ///     Gets a value indicating whether this Aztec code is compact.
        /// </summary>
        /// <value>
        ///     <c>true</c> if compact; otherwise, <c>false</c>.
        /// </value>
        public bool Compact { get; private set; }

        /// <summary>
        ///     Gets the nb datablocks.
        /// </summary>
        public int NbDatablocks { get; private set; }

        /// <summary>
        ///     Gets the nb layers.
        /// </summary>
        public int NbLayers { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AztecDetectorResult" /> class.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <param name="points">The points.</param>
        /// <param name="compact">if set to <c>true</c> [compact].</param>
        /// <param name="nbDatablocks">The nb datablocks.</param>
        /// <param name="nbLayers">The nb layers.</param>
        public AztecDetectorResult(BitMatrix bits,
                                   ResultPoint[] points,
                                   bool compact,
                                   int nbDatablocks,
                                   int nbLayers)
            : base(bits, points)
        {
            Compact = compact;
            NbDatablocks = nbDatablocks;
            NbLayers = nbLayers;
        }
    }
}

namespace ZXing.Aztec
{
    /// <summary>
    ///     Aztec result meta data.
    /// </summary>
    public sealed class AztecResultMetadata
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
        public int Datablocks { get; private set; }

        /// <summary>
        ///     Gets the nb layers.
        /// </summary>
        public int Layers { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="compact"></param>
        /// <param name="datablocks"></param>
        /// <param name="layers"></param>
        public AztecResultMetadata(bool compact, int datablocks, int layers)
        {
            Compact = compact;
            Datablocks = datablocks;
            Layers = layers;
        }
    }
}

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class BlockParsedResult
    {
        private readonly DecodedInformation decodedInformation;
        private readonly bool finished;

        internal BlockParsedResult(bool finished)
            : this(null, finished) { }

        internal BlockParsedResult(DecodedInformation information, bool finished)
        {
            this.finished = finished;
            decodedInformation = information;
        }

        internal DecodedInformation getDecodedInformation() { return decodedInformation; }

        internal bool isFinished() { return finished; }
    }
}

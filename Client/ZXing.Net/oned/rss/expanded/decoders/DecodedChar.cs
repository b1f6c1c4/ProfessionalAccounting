namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class DecodedChar : DecodedObject
    {
        private readonly char value;

        internal static char FNC1 = '$'; // It's not in Alphanumeric neither in ISO/IEC 646 charset

        internal DecodedChar(int newPosition, char value)
            : base(newPosition) { this.value = value; }

        internal char getValue() { return value; }

        internal bool isFNC1() { return value == FNC1; }
    }
}

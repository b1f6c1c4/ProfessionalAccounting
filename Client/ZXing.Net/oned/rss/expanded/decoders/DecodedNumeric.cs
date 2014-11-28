namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class DecodedNumeric : DecodedObject
    {
        private readonly int firstDigit;
        private readonly int secondDigit;

        internal static int FNC1 = 10;

        internal DecodedNumeric(int newPosition, int firstDigit, int secondDigit)
            : base(newPosition)
        {
            if (firstDigit < 0 ||
                firstDigit > 10 ||
                secondDigit < 0 ||
                secondDigit > 10)
                throw FormatException.Instance;

            this.firstDigit = firstDigit;
            this.secondDigit = secondDigit;
        }

        internal int getFirstDigit() { return firstDigit; }

        internal int getSecondDigit() { return secondDigit; }

        internal int getValue() { return firstDigit * 10 + secondDigit; }

        internal bool isFirstDigitFNC1() { return firstDigit == FNC1; }

        internal bool isSecondDigitFNC1() { return secondDigit == FNC1; }

        internal bool isAnyFNC1() { return firstDigit == FNC1 || secondDigit == FNC1; }
    }
}

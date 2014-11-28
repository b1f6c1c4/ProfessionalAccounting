using System;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class DecodedInformation : DecodedObject
    {
        private readonly String newString;
        private readonly int remainingValue;
        private readonly bool remaining;

        internal DecodedInformation(int newPosition, String newString)
            : base(newPosition)
        {
            this.newString = newString;
            remaining = false;
            remainingValue = 0;
        }

        internal DecodedInformation(int newPosition, String newString, int remainingValue)
            : base(newPosition)
        {
            remaining = true;
            this.remainingValue = remainingValue;
            this.newString = newString;
        }

        internal String getNewString() { return newString; }

        internal bool isRemaining() { return remaining; }

        internal int getRemainingValue() { return remainingValue; }
    }
}

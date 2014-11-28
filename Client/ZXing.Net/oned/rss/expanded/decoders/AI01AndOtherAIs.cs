using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class AI01AndOtherAIs : AI01decoder
    {
        private static int HEADER_SIZE = 1 + 1 + 2; //first bit encodes the linkage flag,
        //the second one is the encodation method, and the other two are for the variable length

        internal AI01AndOtherAIs(BitArray information)
            : base(information) { }

        public override String parseInformation()
        {
            var buff = new StringBuilder();

            buff.Append("(01)");
            var initialGtinPosition = buff.Length;
            var firstGtinDigit = getGeneralDecoder().extractNumericValueFromBitArray(HEADER_SIZE, 4);
            buff.Append(firstGtinDigit);

            encodeCompressedGtinWithoutAI(buff, HEADER_SIZE + 4, initialGtinPosition);

            return getGeneralDecoder().decodeAllCodes(buff, HEADER_SIZE + 44);
        }
    }
}

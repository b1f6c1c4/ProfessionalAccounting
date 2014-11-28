using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class AI01392xDecoder : AI01decoder
    {
        private const int HEADER_SIZE = 5 + 1 + 2;
        private const int LAST_DIGIT_SIZE = 2;

        internal AI01392xDecoder(BitArray information)
            : base(information) { }

        public override String parseInformation()
        {
            if (getInformation().Size < HEADER_SIZE + GTIN_SIZE)
                return null;

            var buf = new StringBuilder();

            encodeCompressedGtin(buf, HEADER_SIZE);

            var lastAIdigit =
                getGeneralDecoder().extractNumericValueFromBitArray(HEADER_SIZE + GTIN_SIZE, LAST_DIGIT_SIZE);
            buf.Append("(392");
            buf.Append(lastAIdigit);
            buf.Append(')');

            var decodedInformation =
                getGeneralDecoder().decodeGeneralPurposeField(HEADER_SIZE + GTIN_SIZE + LAST_DIGIT_SIZE, null);
            buf.Append(decodedInformation.getNewString());

            return buf.ToString();
        }
    }
}

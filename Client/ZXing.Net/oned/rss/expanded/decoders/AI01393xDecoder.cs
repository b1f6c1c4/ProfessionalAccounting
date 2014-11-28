using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class AI01393xDecoder : AI01decoder
    {
        private static int HEADER_SIZE = 5 + 1 + 2;
        private static int LAST_DIGIT_SIZE = 2;
        private static int FIRST_THREE_DIGITS_SIZE = 10;

        internal AI01393xDecoder(BitArray information)
            : base(information) {}

        public override String parseInformation()
        {
            if (getInformation().Size < HEADER_SIZE + GTIN_SIZE)
                return null;

            var buf = new StringBuilder();

            encodeCompressedGtin(buf, HEADER_SIZE);

            var lastAIdigit =
                getGeneralDecoder().extractNumericValueFromBitArray(HEADER_SIZE + GTIN_SIZE, LAST_DIGIT_SIZE);

            buf.Append("(393");
            buf.Append(lastAIdigit);
            buf.Append(')');

            var firstThreeDigits =
                getGeneralDecoder()
                    .extractNumericValueFromBitArray(HEADER_SIZE + GTIN_SIZE + LAST_DIGIT_SIZE, FIRST_THREE_DIGITS_SIZE);
            if (firstThreeDigits / 100 == 0)
                buf.Append('0');
            if (firstThreeDigits / 10 == 0)
                buf.Append('0');
            buf.Append(firstThreeDigits);

            var generalInformation =
                getGeneralDecoder()
                    .decodeGeneralPurposeField(
                                               HEADER_SIZE + GTIN_SIZE + LAST_DIGIT_SIZE + FIRST_THREE_DIGITS_SIZE,
                                               null);
            buf.Append(generalInformation.getNewString());

            return buf.ToString();
        }
    }
}

using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal abstract class AI01decoder : AbstractExpandedDecoder
    {
        protected static int GTIN_SIZE = 40;

        internal AI01decoder(BitArray information)
            : base(information) { }

        protected void encodeCompressedGtin(StringBuilder buf, int currentPos)
        {
            buf.Append("(01)");
            var initialPosition = buf.Length;
            buf.Append('9');

            encodeCompressedGtinWithoutAI(buf, currentPos, initialPosition);
        }

        protected void encodeCompressedGtinWithoutAI(StringBuilder buf, int currentPos, int initialBufferPosition)
        {
            for (var i = 0; i < 4; ++i)
            {
                var currentBlock = getGeneralDecoder().extractNumericValueFromBitArray(currentPos + 10 * i, 10);
                if (currentBlock / 100 == 0)
                    buf.Append('0');
                if (currentBlock / 10 == 0)
                    buf.Append('0');
                buf.Append(currentBlock);
            }

            appendCheckDigit(buf, initialBufferPosition);
        }

        private static void appendCheckDigit(StringBuilder buf, int currentPos)
        {
            var checkDigit = 0;
            for (var i = 0; i < 13; i++)
            {
                var digit = buf[i + currentPos] - '0';
                checkDigit += (i & 0x01) == 0 ? 3 * digit : digit;
            }

            checkDigit = 10 - (checkDigit % 10);
            if (checkDigit == 10)
                checkDigit = 0;

            buf.Append(checkDigit);
        }
    }
}

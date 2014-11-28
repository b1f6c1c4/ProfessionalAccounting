using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class AI013x0x1xDecoder : AI01weightDecoder
    {
        private static int HEADER_SIZE = 7 + 1;
        private static int WEIGHT_SIZE = 20;
        private static int DATE_SIZE = 16;

        private readonly String dateCode;
        private readonly String firstAIdigits;

        internal AI013x0x1xDecoder(BitArray information, String firstAIdigits, String dateCode)
            : base(information)
        {
            this.dateCode = dateCode;
            this.firstAIdigits = firstAIdigits;
        }

        public override String parseInformation()
        {
            if (getInformation().Size != HEADER_SIZE + GTIN_SIZE + WEIGHT_SIZE + DATE_SIZE)
                return null;

            var buf = new StringBuilder();

            encodeCompressedGtin(buf, HEADER_SIZE);
            encodeCompressedWeight(buf, HEADER_SIZE + GTIN_SIZE, WEIGHT_SIZE);
            encodeCompressedDate(buf, HEADER_SIZE + GTIN_SIZE + WEIGHT_SIZE);

            return buf.ToString();
        }

        private void encodeCompressedDate(StringBuilder buf, int currentPos)
        {
            var numericDate = getGeneralDecoder().extractNumericValueFromBitArray(currentPos, DATE_SIZE);
            if (numericDate == 38400)
                return;

            buf.Append('(');
            buf.Append(dateCode);
            buf.Append(')');

            var day = numericDate % 32;
            numericDate /= 32;
            var month = numericDate % 12 + 1;
            numericDate /= 12;
            var year = numericDate;

            if (year / 10 == 0)
                buf.Append('0');
            buf.Append(year);
            if (month / 10 == 0)
                buf.Append('0');
            buf.Append(month);
            if (day / 10 == 0)
                buf.Append('0');
            buf.Append(day);
        }

        protected override void addWeightCode(StringBuilder buf, int weight)
        {
            var lastAI = weight / 100000;
            buf.Append('(');
            buf.Append(firstAIdigits);
            buf.Append(lastAI);
            buf.Append(')');
        }

        protected override int checkWeight(int weight) { return weight % 100000; }
    }
}

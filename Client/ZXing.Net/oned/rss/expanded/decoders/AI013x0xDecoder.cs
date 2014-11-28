using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal abstract class AI013x0xDecoder : AI01weightDecoder
    {
        private static int HEADER_SIZE = 4 + 1;
        private static int WEIGHT_SIZE = 15;

        internal AI013x0xDecoder(BitArray information)
            : base(information) { }

        public override String parseInformation()
        {
            if (getInformation().Size != HEADER_SIZE + GTIN_SIZE + WEIGHT_SIZE)
                return null;

            var buf = new StringBuilder();

            encodeCompressedGtin(buf, HEADER_SIZE);
            encodeCompressedWeight(buf, HEADER_SIZE + GTIN_SIZE, WEIGHT_SIZE);

            return buf.ToString();
        }
    }
}

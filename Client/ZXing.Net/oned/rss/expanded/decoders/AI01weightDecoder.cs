using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal abstract class AI01weightDecoder : AI01decoder
    {
        internal AI01weightDecoder(BitArray information)
            : base(information) { }

        protected void encodeCompressedWeight(StringBuilder buf, int currentPos, int weightSize)
        {
            var originalWeightNumeric = getGeneralDecoder().extractNumericValueFromBitArray(currentPos, weightSize);
            addWeightCode(buf, originalWeightNumeric);

            var weightNumeric = checkWeight(originalWeightNumeric);

            var currentDivisor = 100000;
            for (var i = 0; i < 5; ++i)
            {
                if (weightNumeric / currentDivisor == 0)
                    buf.Append('0');
                currentDivisor /= 10;
            }
            buf.Append(weightNumeric);
        }

        protected abstract void addWeightCode(StringBuilder buf, int weight);
        protected abstract int checkWeight(int weight);
    }
}

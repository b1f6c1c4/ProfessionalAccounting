using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class AI01320xDecoder : AI013x0xDecoder
    {
        internal AI01320xDecoder(BitArray information)
            : base(information) { }

        protected override void addWeightCode(StringBuilder buf, int weight)
        {
            if (weight < 10000)
                buf.Append("(3202)");
            else
                buf.Append("(3203)");
        }

        protected override int checkWeight(int weight)
        {
            if (weight < 10000)
                return weight;
            return weight - 10000;
        }
    }
}

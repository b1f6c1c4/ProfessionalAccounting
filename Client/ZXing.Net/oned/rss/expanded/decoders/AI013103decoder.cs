using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class AI013103decoder : AI013x0xDecoder
    {
        internal AI013103decoder(BitArray information)
            : base(information) { }

        protected override void addWeightCode(StringBuilder buf, int weight) { buf.Append("(3103)"); }

        protected override int checkWeight(int weight) { return weight; }
    }
}

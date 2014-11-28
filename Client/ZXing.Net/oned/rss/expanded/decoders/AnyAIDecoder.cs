using System;
using System.Text;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded.Decoders
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal sealed class AnyAIDecoder : AbstractExpandedDecoder
    {
        private static int HEADER_SIZE = 2 + 1 + 2;

        internal AnyAIDecoder(BitArray information)
            : base(information) { }

        public override String parseInformation()
        {
            var buf = new StringBuilder();
            return getGeneralDecoder().decodeAllCodes(buf, HEADER_SIZE);
        }
    }
}

using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.OneD.RSS.Expanded
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    ///     <author>Eduardo Castillejo, University of Deusto (eduardo.castillejo@deusto.es)</author>
    /// </summary>
    internal static class BitArrayBuilder
    {
        internal static BitArray buildBitArray(List<ExpandedPair> pairs)
        {
            var charNumber = (pairs.Count << 1) - 1;
            if (pairs[pairs.Count - 1].RightChar == null)
                charNumber -= 1;

            var size = 12 * charNumber;

            var binary = new BitArray(size);
            var accPos = 0;

            var firstPair = pairs[0];
            var firstValue = firstPair.RightChar.Value;
            for (var i = 11; i >= 0; --i)
            {
                if ((firstValue & (1 << i)) != 0)
                    binary[accPos] = true;
                accPos++;
            }

            for (var i = 1; i < pairs.Count; ++i)
            {
                var currentPair = pairs[i];

                var leftValue = currentPair.LeftChar.Value;
                for (var j = 11; j >= 0; --j)
                {
                    if ((leftValue & (1 << j)) != 0)
                        binary[accPos] = true;
                    accPos++;
                }

                if (currentPair.RightChar != null)
                {
                    var rightValue = currentPair.RightChar.Value;
                    for (var j = 11; j >= 0; --j)
                    {
                        if ((rightValue & (1 << j)) != 0)
                            binary[accPos] = true;
                        accPos++;
                    }
                }
            }
            return binary;
        }
    }
}

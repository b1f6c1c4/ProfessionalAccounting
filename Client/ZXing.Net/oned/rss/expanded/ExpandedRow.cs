using System;
using System.Collections.Generic;

namespace ZXing.OneD.RSS.Expanded
{
    /// <summary>
    ///     One row of an RSS Expanded Stacked symbol, consisting of 1+ expanded pairs.
    /// </summary>
    internal sealed class ExpandedRow
    {
        internal ExpandedRow(List<ExpandedPair> pairs, int rowNumber, bool wasReversed)
        {
            Pairs = new List<ExpandedPair>(pairs);
            RowNumber = rowNumber;
            IsReversed = wasReversed;
        }

        internal List<ExpandedPair> Pairs { get; private set; }

        internal int RowNumber { get; private set; }

        /// <summary>
        ///     Did this row of the image have to be reversed (mirrored) to recognize the pairs?
        /// </summary>
        internal bool IsReversed { get; private set; }

        internal bool IsEquivalent(List<ExpandedPair> otherPairs) { return Pairs.Equals(otherPairs); }

        public override String ToString() { return "{ " + Pairs + " }"; }

        /// <summary>
        ///     Two rows are equal if they contain the same pairs in the same order.
        /// </summary>
        public override bool Equals(Object o)
        {
            if (!(o is ExpandedRow))
                return false;
            var that = (ExpandedRow)o;
            return Pairs.Equals(that.Pairs) && IsReversed == that.IsReversed;
        }

        public override int GetHashCode() { return Pairs.GetHashCode() ^ IsReversed.GetHashCode(); }
    }
}

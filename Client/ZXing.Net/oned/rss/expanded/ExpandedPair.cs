using System;

namespace ZXing.OneD.RSS.Expanded
{
    /// <summary>
    ///     <author>Pablo Orduña, University of Deusto (pablo.orduna@deusto.es)</author>
    /// </summary>
    internal sealed class ExpandedPair
    {
        internal bool MayBeLast { get; private set; }
        internal DataCharacter LeftChar { get; private set; }
        internal DataCharacter RightChar { get; private set; }
        internal FinderPattern FinderPattern { get; private set; }

        internal ExpandedPair(DataCharacter leftChar,
                              DataCharacter rightChar,
                              FinderPattern finderPattern,
                              bool mayBeLast)
        {
            LeftChar = leftChar;
            RightChar = rightChar;
            FinderPattern = finderPattern;
            MayBeLast = mayBeLast;
        }

        public bool MustBeLast { get { return RightChar == null; } }

        public override String ToString()
        {
            return
                "[ " + LeftChar + " , " + RightChar + " : " +
                (FinderPattern == null ? "null" : FinderPattern.Value.ToString()) + " ]";
        }

        public override bool Equals(Object o)
        {
            if (!(o is ExpandedPair))
                return false;
            var that = (ExpandedPair)o;
            return
                EqualsOrNull(LeftChar, that.LeftChar) &&
                EqualsOrNull(RightChar, that.RightChar) &&
                EqualsOrNull(FinderPattern, that.FinderPattern);
        }

        private static bool EqualsOrNull(Object o1, Object o2) { return o1 == null ? o2 == null : o1.Equals(o2); }

        public override int GetHashCode()
        {
            return hashNotNull(LeftChar) ^ hashNotNull(RightChar) ^ hashNotNull(FinderPattern);
        }

        private static int hashNotNull(Object o) { return o == null ? 0 : o.GetHashCode(); }
    }
}

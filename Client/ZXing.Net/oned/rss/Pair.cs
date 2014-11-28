namespace ZXing.OneD.RSS
{
    internal sealed class Pair : DataCharacter
    {
        public FinderPattern FinderPattern { get; private set; }
        public int Count { get; private set; }

        internal Pair(int value, int checksumPortion, FinderPattern finderPattern)
            : base(value, checksumPortion) { FinderPattern = finderPattern; }

        public void incrementCount() { Count++; }
    }
}

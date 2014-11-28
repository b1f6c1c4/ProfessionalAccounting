using ZXing.Common;

namespace ZXing.Aztec.Internal
{
    public abstract class Token
    {
        public static Token EMPTY = new SimpleToken(null, 0, 0);

        private readonly Token previous;

        protected Token(Token previous) { this.previous = previous; }

        public Token Previous { get { return previous; } }

        public Token add(int value, int bitCount) { return new SimpleToken(this, value, bitCount); }

        public Token addBinaryShift(int start, int byteCount)
        {
            var bitCount = (byteCount * 8) + (byteCount <= 31 ? 10 : byteCount <= 62 ? 20 : 21);
            return new BinaryShiftToken(this, start, byteCount);
        }

        public abstract void appendTo(BitArray bitArray, byte[] text);
    }
}

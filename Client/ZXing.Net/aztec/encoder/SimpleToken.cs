using System;
using ZXing.Common;

namespace ZXing.Aztec.Internal
{
    public sealed class SimpleToken : Token
    {
        // For normal words, indicates value and bitCount
        private readonly short value;
        private readonly short bitCount;

        public SimpleToken(Token previous, int value, int bitCount)
            : base(previous)
        {
            this.value = (short)value;
            this.bitCount = (short)bitCount;
        }

        public override void appendTo(BitArray bitArray, byte[] text) { bitArray.appendBits(value, bitCount); }

        public override String ToString()
        {
            var value = this.value & ((1 << bitCount) - 1);
            value |= 1 << bitCount;
            return '<' + SupportClass.ToBinaryString(value | (1 << bitCount)).Substring(1) + '>';
        }
    }
}

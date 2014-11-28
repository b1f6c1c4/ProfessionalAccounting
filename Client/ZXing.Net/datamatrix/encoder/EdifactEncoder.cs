using System;
using System.Text;

namespace ZXing.Datamatrix.Encoder
{
    internal sealed class EdifactEncoder : Encoder
    {
        public int EncodingMode { get { return Encodation.EDIFACT; } }

        public void encode(EncoderContext context)
        {
            //step F
            var buffer = new StringBuilder();
            while (context.HasMoreCharacters)
            {
                var c = context.CurrentChar;
                encodeChar(c, buffer);
                context.Pos++;

                var count = buffer.Length;
                if (count >= 4)
                {
                    context.writeCodewords(encodeToCodewords(buffer, 0));
                    buffer.Remove(0, 4);

                    var newMode = HighLevelEncoder.lookAheadTest(context.Message, context.Pos, EncodingMode);
                    if (newMode != EncodingMode)
                    {
                        context.signalEncoderChange(Encodation.ASCII);
                        break;
                    }
                }
            }
            buffer.Append((char)31); //Unlatch
            handleEOD(context, buffer);
        }

        /// <summary>
        ///     Handle "end of data" situations
        /// </summary>
        /// <param name="context">the encoder context</param>
        /// <param name="buffer">the buffer with the remaining encoded characters</param>
        private static void handleEOD(EncoderContext context, StringBuilder buffer)
        {
            try
            {
                var count = buffer.Length;
                if (count == 0)
                    return; //Already finished
                if (count == 1)
                {
                    //Only an unlatch at the end
                    context.updateSymbolInfo();
                    var available = context.SymbolInfo.dataCapacity - context.CodewordCount;
                    var remaining = context.RemainingCharacters;
                    if (remaining == 0 &&
                        available <= 2)
                        return; //No unlatch
                }

                if (count > 4)
                    throw new InvalidOperationException("Count must not exceed 4");
                var restChars = count - 1;
                var encoded = encodeToCodewords(buffer, 0);
                var endOfSymbolReached = !context.HasMoreCharacters;
                var restInAscii = endOfSymbolReached && restChars <= 2;

                if (restChars <= 2)
                {
                    context.updateSymbolInfo(context.CodewordCount + restChars);
                    var available = context.SymbolInfo.dataCapacity - context.CodewordCount;
                    if (available >= 3)
                    {
                        restInAscii = false;
                        context.updateSymbolInfo(context.CodewordCount + encoded.Length);
                        //available = context.symbolInfo.dataCapacity - context.getCodewordCount();
                    }
                }

                if (restInAscii)
                {
                    context.resetSymbolInfo();
                    context.Pos -= restChars;
                }
                else
                    context.writeCodewords(encoded);
            }
            finally
            {
                context.signalEncoderChange(Encodation.ASCII);
            }
        }

        private static void encodeChar(char c, StringBuilder sb)
        {
            if (c >= ' ' &&
                c <= '?')
                sb.Append(c);
            else if (c >= '@' &&
                     c <= '^')
                sb.Append((char)(c - 64));
            else
                HighLevelEncoder.illegalCharacter(c);
        }

        private static String encodeToCodewords(StringBuilder sb, int startPos)
        {
            var len = sb.Length - startPos;
            if (len == 0)
                throw new InvalidOperationException("StringBuilder must not be empty");
            var c1 = sb[startPos];
            var c2 = len >= 2 ? sb[startPos + 1] : (char)0;
            var c3 = len >= 3 ? sb[startPos + 2] : (char)0;
            var c4 = len >= 4 ? sb[startPos + 3] : (char)0;

            var v = (c1 << 18) + (c2 << 12) + (c3 << 6) + c4;
            var cw1 = (char)((v >> 16) & 255);
            var cw2 = (char)((v >> 8) & 255);
            var cw3 = (char)(v & 255);
            var res = new StringBuilder(3);
            res.Append(cw1);
            if (len >= 2)
                res.Append(cw2);
            if (len >= 3)
                res.Append(cw3);
            return res.ToString();
        }
    }
}

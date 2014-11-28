using System.Text;

namespace ZXing.Datamatrix.Encoder
{
    internal sealed class X12Encoder : C40Encoder
    {
        public override int EncodingMode { get { return Encodation.X12; } }

        public override void encode(EncoderContext context)
        {
            //step C
            var buffer = new StringBuilder();
            var currentMode = EncodingMode;
            while (context.HasMoreCharacters)
            {
                var c = context.CurrentChar;
                context.Pos++;

                encodeChar(c, buffer);

                var count = buffer.Length;
                if ((count % 3) == 0)
                {
                    writeNextTriplet(context, buffer);

                    var newMode = HighLevelEncoder.lookAheadTest(context.Message, context.Pos, currentMode);
                    if (newMode != currentMode)
                    {
                        handleEOD(context, buffer);
                        context.signalEncoderChange(newMode);
                        return;
                    }
                }
            }
            handleEOD(context, buffer);
        }

        protected override int encodeChar(char c, StringBuilder sb)
        {
            if (c == '\r')
                sb.Append('\u0000');
            else if (c == '*')
                sb.Append('\u0001');
            else if (c == '>')
                sb.Append('\u0002');
            else if (c == ' ')
                sb.Append('\u0003');
            else if (c >= '0' &&
                     c <= '9')
                sb.Append((char)(c - 48 + 4));
            else if (c >= 'A' &&
                     c <= 'Z')
                sb.Append((char)(c - 65 + 14));
            else
                HighLevelEncoder.illegalCharacter(c);
            return 1;
        }

        protected override void handleEOD(EncoderContext context, StringBuilder buffer)
        {
            context.updateSymbolInfo();
            var available = context.SymbolInfo.dataCapacity - context.CodewordCount;
            var count = buffer.Length;
            if (count == 2)
            {
                context.writeCodeword(HighLevelEncoder.X12_UNLATCH);
                context.Pos -= 2;
                context.signalEncoderChange(Encodation.ASCII);
            }
            else if (count == 1)
            {
                context.Pos--;
                if (context.RemainingCharacters > 1)
                {
                    context.writeCodeword(HighLevelEncoder.X12_UNLATCH);
                    if (available < 1)
                        context.updateSymbolInfo();
                }
                context.signalEncoderChange(Encodation.ASCII);
            }
            else if (count == 0)
                if (context.RemainingCharacters > 1)
                {
                    context.writeCodeword(HighLevelEncoder.X12_UNLATCH);
                    if (available < 1)
                        context.updateSymbolInfo();
                }
        }
    }
}

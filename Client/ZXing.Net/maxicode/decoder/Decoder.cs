using System;
using System.Collections.Generic;
using ZXing.Common;
using ZXing.Common.ReedSolomon;

namespace ZXing.Maxicode.Internal
{
    /// <summary>
    ///     <p>
    ///         The main class which implements MaxiCode decoding -- as opposed to locating and extracting
    ///         the MaxiCode from an image.
    ///     </p>
    ///     <author>Manuel Kasten</author>
    /// </summary>
    public sealed class Decoder
    {
        private const int ALL = 0;
        private const int EVEN = 1;
        private const int ODD = 2;

        private readonly ReedSolomonDecoder rsDecoder;

        public Decoder() { rsDecoder = new ReedSolomonDecoder(GenericGF.MAXICODE_FIELD_64); }

        public DecoderResult decode(BitMatrix bits) { return decode(bits, null); }

        public DecoderResult decode(BitMatrix bits,
                                    IDictionary<DecodeHintType, object> hints)
        {
            var parser = new BitMatrixParser(bits);
            var codewords = parser.readCodewords();

            if (!correctErrors(codewords, 0, 10, 10, ALL))
                return null;

            var mode = codewords[0] & 0x0F;
            byte[] datawords;
            switch (mode)
            {
                case 2:
                case 3:
                case 4:
                    if (!correctErrors(codewords, 20, 84, 40, EVEN))
                        return null;
                    if (!correctErrors(codewords, 20, 84, 40, ODD))
                        return null;
                    datawords = new byte[94];
                    break;
                case 5:
                    if (!correctErrors(codewords, 20, 68, 56, EVEN))
                        return null;
                    if (!correctErrors(codewords, 20, 68, 56, ODD))
                        return null;
                    datawords = new byte[78];
                    break;
                default:
                    return null;
            }

            Array.Copy(codewords, 0, datawords, 0, 10);
            Array.Copy(codewords, 20, datawords, 10, datawords.Length - 10);

            return DecodedBitStreamParser.decode(datawords, mode);
        }

        private bool correctErrors(byte[] codewordBytes,
                                   int start,
                                   int dataCodewords,
                                   int ecCodewords,
                                   int mode)
        {
            var codewords = dataCodewords + ecCodewords;

            // in EVEN or ODD mode only half the codewords
            var divisor = mode == ALL ? 1 : 2;

            // First read into an array of ints
            var codewordsInts = new int[codewords / divisor];
            for (var i = 0; i < codewords; i++)
                if ((mode == ALL) ||
                    (i % 2 == (mode - 1)))
                    codewordsInts[i / divisor] = codewordBytes[i + start] & 0xFF;

            if (!rsDecoder.decode(codewordsInts, ecCodewords / divisor))
                return false;

            // Copy back into array of bytes -- only need to worry about the bytes that were data
            // We don't care about errors in the error-correction codewords
            for (var i = 0; i < dataCodewords; i++)
                if ((mode == ALL) ||
                    (i % 2 == (mode - 1)))
                    codewordBytes[i + start] = (byte)codewordsInts[i / divisor];

            return true;
        }
    }
}

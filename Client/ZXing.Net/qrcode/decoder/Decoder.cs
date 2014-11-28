using System.Collections.Generic;
using ZXing.Common;
using ZXing.Common.ReedSolomon;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         The main class which implements QR Code decoding -- as opposed to locating and extracting
    ///         the QR Code from an image.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    public sealed class Decoder
    {
        private readonly ReedSolomonDecoder rsDecoder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Decoder" /> class.
        /// </summary>
        public Decoder() { rsDecoder = new ReedSolomonDecoder(GenericGF.QR_CODE_FIELD_256); }

        /// <summary>
        ///     <p>
        ///         Convenience method that can decode a QR Code represented as a 2D array of booleans.
        ///         "true" is taken to mean a black module.
        ///     </p>
        /// </summary>
        /// <param name="image">booleans representing white/black QR Code modules</param>
        /// <param name="hints">decoding hints that should be used to influence decoding</param>
        /// <returns>
        ///     text and bytes encoded within the QR Code
        /// </returns>
        public DecoderResult decode(bool[][] image, IDictionary<DecodeHintType, object> hints)
        {
            var dimension = image.Length;
            var bits = new BitMatrix(dimension);
            for (var i = 0; i < dimension; i++)
                for (var j = 0; j < dimension; j++)
                    bits[j, i] = image[i][j];
            return decode(bits, hints);
        }

        /// <summary>
        ///     <p>Decodes a QR Code represented as a {@link BitMatrix}. A 1 or "true" is taken to mean a black module.</p>
        /// </summary>
        /// <param name="bits">booleans representing white/black QR Code modules</param>
        /// <param name="hints">decoding hints that should be used to influence decoding</param>
        /// <returns>
        ///     text and bytes encoded within the QR Code
        /// </returns>
        public DecoderResult decode(BitMatrix bits, IDictionary<DecodeHintType, object> hints)
        {
            // Construct a parser and read version, error-correction level
            var parser = BitMatrixParser.createBitMatrixParser(bits);
            if (parser == null)
                return null;

            var result = decode(parser, hints);
            if (result == null)
            {
                // Revert the bit matrix
                parser.remask();

                // Will be attempting a mirrored reading of the version and format info.
                parser.setMirror(true);

                // Preemptively read the version.
                var version = parser.readVersion();
                if (version == null)
                    return null;

                // Preemptively read the format information.
                var formatinfo = parser.readFormatInformation();
                if (formatinfo == null)
                    return null;

                /*
             * Since we're here, this means we have successfully detected some kind
             * of version and format information when mirrored. This is a good sign,
             * that the QR code may be mirrored, and we should try once more with a
             * mirrored content.
             */
                // Prepare for a mirrored reading.
                parser.mirror();

                result = decode(parser, hints);

                if (result != null)
                    // Success! Notify the caller that the code was mirrored.
                    result.Other = new QRCodeDecoderMetaData(true);
            }

            return result;
        }

        private DecoderResult decode(BitMatrixParser parser, IDictionary<DecodeHintType, object> hints)
        {
            var version = parser.readVersion();
            if (version == null)
                return null;
            var formatinfo = parser.readFormatInformation();
            if (formatinfo == null)
                return null;
            var ecLevel = formatinfo.ErrorCorrectionLevel;

            // Read codewords
            var codewords = parser.readCodewords();
            if (codewords == null)
                return null;
            // Separate into data blocks
            var dataBlocks = DataBlock.getDataBlocks(codewords, version, ecLevel);

            // Count total number of data bytes
            var totalBytes = 0;
            foreach (var dataBlock in dataBlocks)
                totalBytes += dataBlock.NumDataCodewords;
            var resultBytes = new byte[totalBytes];
            var resultOffset = 0;

            // Error-correct and copy data blocks together into a stream of bytes
            foreach (var dataBlock in dataBlocks)
            {
                var codewordBytes = dataBlock.Codewords;
                var numDataCodewords = dataBlock.NumDataCodewords;
                if (!correctErrors(codewordBytes, numDataCodewords))
                    return null;
                for (var i = 0; i < numDataCodewords; i++)
                    resultBytes[resultOffset++] = codewordBytes[i];
            }

            // Decode the contents of that stream of bytes
            return DecodedBitStreamParser.decode(resultBytes, version, ecLevel, hints);
        }

        /// <summary>
        ///     <p>
        ///         Given data and error-correction codewords received, possibly corrupted by errors, attempts to
        ///         correct the errors in-place using Reed-Solomon error correction.
        ///     </p>
        /// </summary>
        /// <param name="codewordBytes">data and error correction codewords</param>
        /// <param name="numDataCodewords">number of codewords that are data bytes</param>
        /// <returns></returns>
        private bool correctErrors(byte[] codewordBytes, int numDataCodewords)
        {
            var numCodewords = codewordBytes.Length;
            // First read into an array of ints
            var codewordsInts = new int[numCodewords];
            for (var i = 0; i < numCodewords; i++)
                codewordsInts[i] = codewordBytes[i] & 0xFF;
            var numECCodewords = codewordBytes.Length - numDataCodewords;

            if (!rsDecoder.decode(codewordsInts, numECCodewords))
                return false;

            // Copy back into array of bytes -- only need to worry about the bytes that were data
            // We don't care about errors in the error-correction codewords
            for (var i = 0; i < numDataCodewords; i++)
                codewordBytes[i] = (byte)codewordsInts[i];

            return true;
        }
    }
}

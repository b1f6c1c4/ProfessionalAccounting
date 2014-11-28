using ZXing.Common;

namespace ZXing.QrCode.Internal
{
    /// <author>Sean Owen</author>
    internal sealed class BitMatrixParser
    {
        private readonly BitMatrix bitMatrix;
        private Version parsedVersion;
        private FormatInformation parsedFormatInfo;
        private bool mirrored;

        /// <param name="bitMatrix">{@link BitMatrix} to parse</param>
        /// <throws>ReaderException if dimension is not >= 21 and 1 mod 4</throws>
        internal static BitMatrixParser createBitMatrixParser(BitMatrix bitMatrix)
        {
            var dimension = bitMatrix.Height;
            if (dimension < 21 ||
                (dimension & 0x03) != 1)
                return null;
            return new BitMatrixParser(bitMatrix);
        }

        private BitMatrixParser(BitMatrix bitMatrix)
        {
            // Should only be called from createBitMatrixParser with the important checks before
            this.bitMatrix = bitMatrix;
        }

        /// <summary>
        ///     <p>Reads format information from one of its two locations within the QR Code.</p>
        /// </summary>
        /// <returns>
        ///     {@link FormatInformation} encapsulating the QR Code's format info
        /// </returns>
        /// <throws>  ReaderException if both format information locations cannot be parsed as </throws>
        /// <summary>
        ///     the valid encoding of format information
        /// </summary>
        internal FormatInformation readFormatInformation()
        {
            if (parsedFormatInfo != null)
                return parsedFormatInfo;

            // Read top-left format info bits
            var formatInfoBits1 = 0;
            for (var i = 0; i < 6; i++)
                formatInfoBits1 = copyBit(i, 8, formatInfoBits1);
            // .. and skip a bit in the timing pattern ...
            formatInfoBits1 = copyBit(7, 8, formatInfoBits1);
            formatInfoBits1 = copyBit(8, 8, formatInfoBits1);
            formatInfoBits1 = copyBit(8, 7, formatInfoBits1);
            // .. and skip a bit in the timing pattern ...
            for (var j = 5; j >= 0; j--)
                formatInfoBits1 = copyBit(8, j, formatInfoBits1);
            // Read the top-right/bottom-left pattern too
            var dimension = bitMatrix.Height;
            var formatInfoBits2 = 0;
            var jMin = dimension - 7;
            for (var j = dimension - 1; j >= jMin; j--)
                formatInfoBits2 = copyBit(8, j, formatInfoBits2);
            for (var i = dimension - 8; i < dimension; i++)
                formatInfoBits2 = copyBit(i, 8, formatInfoBits2);

            parsedFormatInfo = FormatInformation.decodeFormatInformation(formatInfoBits1, formatInfoBits2);
            if (parsedFormatInfo != null)
                return parsedFormatInfo;
            return null;
        }

        /// <summary>
        ///     <p>Reads version information from one of its two locations within the QR Code.</p>
        /// </summary>
        /// <returns>
        ///     {@link Version} encapsulating the QR Code's version
        /// </returns>
        /// <throws>  ReaderException if both version information locations cannot be parsed as </throws>
        /// <summary>
        ///     the valid encoding of version information
        /// </summary>
        internal Version readVersion()
        {
            if (parsedVersion != null)
                return parsedVersion;

            var dimension = bitMatrix.Height;

            var provisionalVersion = (dimension - 17) >> 2;
            if (provisionalVersion <= 6)
                return Version.getVersionForNumber(provisionalVersion);

            // Read top-right version info: 3 wide by 6 tall
            var versionBits = 0;
            var ijMin = dimension - 11;
            for (var j = 5; j >= 0; j--)
                for (var i = dimension - 9; i >= ijMin; i--)
                    versionBits = copyBit(i, j, versionBits);

            parsedVersion = Version.decodeVersionInformation(versionBits);
            if (parsedVersion != null &&
                parsedVersion.DimensionForVersion == dimension)
                return parsedVersion;

            // Hmm, failed. Try bottom left: 6 wide by 3 tall
            versionBits = 0;
            for (var i = 5; i >= 0; i--)
                for (var j = dimension - 9; j >= ijMin; j--)
                    versionBits = copyBit(i, j, versionBits);

            parsedVersion = Version.decodeVersionInformation(versionBits);
            if (parsedVersion != null &&
                parsedVersion.DimensionForVersion == dimension)
                return parsedVersion;
            return null;
        }

        private int copyBit(int i, int j, int versionBits)
        {
            var bit = mirrored ? bitMatrix[j, i] : bitMatrix[i, j];
            return bit ? (versionBits << 1) | 0x1 : versionBits << 1;
        }

        /// <summary>
        ///     <p>
        ///         Reads the bits in the {@link BitMatrix} representing the finder pattern in the
        ///         correct order in order to reconstruct the codewords bytes contained within the
        ///         QR Code.
        ///     </p>
        /// </summary>
        /// <returns>
        ///     bytes encoded within the QR Code
        /// </returns>
        /// <throws>  ReaderException if the exact number of bytes expected is not read </throws>
        internal byte[] readCodewords()
        {
            var formatInfo = readFormatInformation();
            if (formatInfo == null)
                return null;
            var version = readVersion();
            if (version == null)
                return null;

            // Get the data mask for the format used in this QR Code. This will exclude
            // some bits from reading as we wind through the bit matrix.
            var dataMask = DataMask.forReference(formatInfo.DataMask);
            var dimension = bitMatrix.Height;
            dataMask.unmaskBitMatrix(bitMatrix, dimension);

            var functionPattern = version.buildFunctionPattern();

            var readingUp = true;
            var result = new byte[version.TotalCodewords];
            var resultOffset = 0;
            var currentByte = 0;
            var bitsRead = 0;
            // Read columns in pairs, from right to left
            for (var j = dimension - 1; j > 0; j -= 2)
            {
                if (j == 6)
                    // Skip whole column with vertical alignment pattern;
                    // saves time and makes the other code proceed more cleanly
                    j--;
                // Read alternatingly from bottom to top then top to bottom
                for (var count = 0; count < dimension; count++)
                {
                    var i = readingUp ? dimension - 1 - count : count;
                    for (var col = 0; col < 2; col++)
                        // Ignore bits covered by the function pattern
                        if (!functionPattern[j - col, i])
                        {
                            // Read a bit
                            bitsRead++;
                            currentByte <<= 1;
                            if (bitMatrix[j - col, i])
                                currentByte |= 1;
                            // If we've made a whole byte, save it off
                            if (bitsRead == 8)
                            {
                                result[resultOffset++] = (byte)currentByte;
                                bitsRead = 0;
                                currentByte = 0;
                            }
                        }
                }
                readingUp ^= true; // readingUp = !readingUp; // switch directions
            }
            if (resultOffset != version.TotalCodewords)
                return null;
            return result;
        }

        /**
       * Revert the mask removal done while reading the code words. The bit matrix should revert to its original state.
       */

        internal void remask()
        {
            if (parsedFormatInfo == null)
                return; // We have no format information, and have no data mask
            var dataMask = DataMask.forReference(parsedFormatInfo.DataMask);
            var dimension = bitMatrix.Height;
            dataMask.unmaskBitMatrix(bitMatrix, dimension);
        }

        /**
       * Prepare the parser for a mirrored operation.
       * This flag has effect only on the {@link #readFormatInformation()} and the
       * {@link #readVersion()}. Before proceeding with {@link #readCodewords()} the
       * {@link #mirror()} method should be called.
       * 
       * @param mirror Whether to read version and format information mirrored.
       */

        internal void setMirror(bool mirror)
        {
            parsedVersion = null;
            parsedFormatInfo = null;
            mirrored = mirror;
        }

        /** Mirror the bit matrix in order to attempt a second reading. */

        internal void mirror()
        {
            for (var x = 0; x < bitMatrix.Width; x++)
                for (var y = x + 1; y < bitMatrix.Height; y++)
                    if (bitMatrix[x, y] != bitMatrix[y, x])
                    {
                        bitMatrix.flip(y, x);
                        bitMatrix.flip(x, y);
                    }
        }
    }
}

using System;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         Encapsulates a block of data within a QR Code. QR Codes may split their data into
    ///         multiple blocks, each of which is a unit of data and error-correction codewords. Each
    ///         is represented by an instance of this class.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class DataBlock
    {
        private readonly int numDataCodewords;
        private readonly byte[] codewords;

        private DataBlock(int numDataCodewords, byte[] codewords)
        {
            this.numDataCodewords = numDataCodewords;
            this.codewords = codewords;
        }

        /// <summary>
        ///     <p>
        ///         When QR Codes use multiple data blocks, they are actually interleaved.
        ///         That is, the first byte of data block 1 to n is written, then the second bytes, and so on. This
        ///         method will separate the data into original blocks.
        ///     </p>
        /// </summary>
        /// <param name="rawCodewords">
        ///     bytes as read directly from the QR Code
        /// </param>
        /// <param name="version">
        ///     version of the QR Code
        /// </param>
        /// <param name="ecLevel">
        ///     error-correction level of the QR Code
        /// </param>
        /// <returns>
        ///     {@link DataBlock}s containing original bytes, "de-interleaved" from representation in the
        ///     QR Code
        /// </returns>
        internal static DataBlock[] getDataBlocks(byte[] rawCodewords, Version version, ErrorCorrectionLevel ecLevel)
        {
            if (rawCodewords.Length != version.TotalCodewords)
                throw new ArgumentException();

            // Figure out the number and size of data blocks used by this version and
            // error correction level
            var ecBlocks = version.getECBlocksForLevel(ecLevel);

            // First count the total number of data blocks
            var totalBlocks = 0;
            var ecBlockArray = ecBlocks.getECBlocks();
            foreach (var ecBlock in ecBlockArray)
                totalBlocks += ecBlock.Count;

            // Now establish DataBlocks of the appropriate size and number of data codewords
            var result = new DataBlock[totalBlocks];
            var numResultBlocks = 0;
            foreach (var ecBlock in ecBlockArray)
                for (var i = 0; i < ecBlock.Count; i++)
                {
                    var numDataCodewords = ecBlock.DataCodewords;
                    var numBlockCodewords = ecBlocks.ECCodewordsPerBlock + numDataCodewords;
                    result[numResultBlocks++] = new DataBlock(numDataCodewords, new byte[numBlockCodewords]);
                }

            // All blocks have the same amount of data, except that the last n
            // (where n may be 0) have 1 more byte. Figure out where these start.
            var shorterBlocksTotalCodewords = result[0].codewords.Length;
            var longerBlocksStartAt = result.Length - 1;
            while (longerBlocksStartAt >= 0)
            {
                var numCodewords = result[longerBlocksStartAt].codewords.Length;
                if (numCodewords == shorterBlocksTotalCodewords)
                    break;
                longerBlocksStartAt--;
            }
            longerBlocksStartAt++;

            var shorterBlocksNumDataCodewords = shorterBlocksTotalCodewords - ecBlocks.ECCodewordsPerBlock;
            // The last elements of result may be 1 element longer;
            // first fill out as many elements as all of them have
            var rawCodewordsOffset = 0;
            for (var i = 0; i < shorterBlocksNumDataCodewords; i++)
                for (var j = 0; j < numResultBlocks; j++)
                    result[j].codewords[i] = rawCodewords[rawCodewordsOffset++];
            // Fill out the last data block in the longer ones
            for (var j = longerBlocksStartAt; j < numResultBlocks; j++)
                result[j].codewords[shorterBlocksNumDataCodewords] = rawCodewords[rawCodewordsOffset++];
            // Now add in error correction blocks
            var max = result[0].codewords.Length;
            for (var i = shorterBlocksNumDataCodewords; i < max; i++)
                for (var j = 0; j < numResultBlocks; j++)
                {
                    var iOffset = j < longerBlocksStartAt ? i : i + 1;
                    result[j].codewords[iOffset] = rawCodewords[rawCodewordsOffset++];
                }
            return result;
        }

        internal int NumDataCodewords { get { return numDataCodewords; } }

        internal byte[] Codewords { get { return codewords; } }
    }
}

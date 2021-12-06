using System;
using System.Collections.Generic;
using ZXing.Common;
using ZXing.Datamatrix.Encoder;
using ZXing.QrCode.Internal;

namespace ZXing.Datamatrix
{
    /// <summary>
    ///     This object renders a Data Matrix code as a BitMatrix 2D array of greyscale values.
    /// </summary>
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    /// <author>Guillaume Le Biller Added to zxing lib.</author>
    public sealed class DataMatrixWriter : Writer
    {
        public BitMatrix encode(String contents, BarcodeFormat format, int width, int height)
        {
            return encode(contents, format, width, height, null);
        }

        public BitMatrix encode(String contents, BarcodeFormat format, int width, int height,
                                IDictionary<EncodeHintType, object> hints)
        {
            if (String.IsNullOrEmpty(contents))
                throw new ArgumentException("Found empty contents", contents);

            if (format != BarcodeFormat.DATA_MATRIX)
                throw new ArgumentException("Can only encode DATA_MATRIX, but got " + format);

            if (width < 0 ||
                height < 0)
                throw new ArgumentException("Requested dimensions are too small: " + width + 'x' + height);

            // Try to get force shape & min / max size
            var shape = SymbolShapeHint.FORCE_NONE;
            var defaultEncodation = Encodation.ASCII;
            Dimension minSize = null;
            Dimension maxSize = null;
            if (hints != null)
            {
                var requestedShape = hints.ContainsKey(EncodeHintType.DATA_MATRIX_SHAPE)
                                         ? (SymbolShapeHint?)hints[EncodeHintType.DATA_MATRIX_SHAPE]
                                         : null;
                if (requestedShape != null)
                    shape = requestedShape.Value;
                var requestedMinSize = hints.ContainsKey(EncodeHintType.MIN_SIZE)
                                           ? (Dimension)hints[EncodeHintType.MIN_SIZE]
                                           : null;
                if (requestedMinSize != null)
                    minSize = requestedMinSize;
                var requestedMaxSize = hints.ContainsKey(EncodeHintType.MAX_SIZE)
                                           ? (Dimension)hints[EncodeHintType.MAX_SIZE]
                                           : null;
                if (requestedMaxSize != null)
                    maxSize = requestedMaxSize;
                var requestedDefaultEncodation = hints.ContainsKey(EncodeHintType.DATA_MATRIX_DEFAULT_ENCODATION)
                                                     ? (int?)hints[EncodeHintType.DATA_MATRIX_DEFAULT_ENCODATION]
                                                     : null;
                if (requestedDefaultEncodation != null)
                    defaultEncodation = requestedDefaultEncodation.Value;
            }


            //1. step: Data encodation
            var encoded = HighLevelEncoder.encodeHighLevel(contents, shape, minSize, maxSize, defaultEncodation);

            var symbolInfo = SymbolInfo.lookup(encoded.Length, shape, minSize, maxSize, true);

            //2. step: ECC generation
            var codewords = ErrorCorrection.encodeECC200(encoded, symbolInfo);

            //3. step: Module placement in Matrix
            var placement =
                new DefaultPlacement(codewords, symbolInfo.getSymbolDataWidth(), symbolInfo.getSymbolDataHeight());
            placement.place();

            //4. step: low-level encoding
            return encodeLowLevel(placement, symbolInfo);
        }

        /// <summary>
        ///     Encode the given symbol info to a bit matrix.
        /// </summary>
        /// <param name="placement">The DataMatrix placement.</param>
        /// <param name="symbolInfo">The symbol info to encode.</param>
        /// <returns>The bit matrix generated.</returns>
        private static BitMatrix encodeLowLevel(DefaultPlacement placement, SymbolInfo symbolInfo)
        {
            var symbolWidth = symbolInfo.getSymbolDataWidth();
            var symbolHeight = symbolInfo.getSymbolDataHeight();

            var matrix = new ByteMatrix(symbolInfo.getSymbolWidth(), symbolInfo.getSymbolHeight());

            var matrixY = 0;

            for (var y = 0; y < symbolHeight; y++)
            {
                // Fill the top edge with alternate 0 / 1
                int matrixX;
                if ((y % symbolInfo.matrixHeight) == 0)
                {
                    matrixX = 0;
                    for (var x = 0; x < symbolInfo.getSymbolWidth(); x++)
                    {
                        matrix.set(matrixX, matrixY, (x % 2) == 0);
                        matrixX++;
                    }
                    matrixY++;
                }
                matrixX = 0;
                for (var x = 0; x < symbolWidth; x++)
                {
                    // Fill the right edge with full 1
                    if ((x % symbolInfo.matrixWidth) == 0)
                    {
                        matrix.set(matrixX, matrixY, true);
                        matrixX++;
                    }
                    matrix.set(matrixX, matrixY, placement.getBit(x, y));
                    matrixX++;
                    // Fill the right edge with alternate 0 / 1
                    if ((x % symbolInfo.matrixWidth) == symbolInfo.matrixWidth - 1)
                    {
                        matrix.set(matrixX, matrixY, (y % 2) == 0);
                        matrixX++;
                    }
                }
                matrixY++;
                // Fill the bottom edge with full 1
                if ((y % symbolInfo.matrixHeight) == symbolInfo.matrixHeight - 1)
                {
                    matrixX = 0;
                    for (var x = 0; x < symbolInfo.getSymbolWidth(); x++)
                    {
                        matrix.set(matrixX, matrixY, true);
                        matrixX++;
                    }
                    matrixY++;
                }
            }

            return convertByteMatrixToBitMatrix(matrix);
        }

        /// <summary>
        ///     Convert the ByteMatrix to BitMatrix.
        /// </summary>
        /// <param name="matrix">The input matrix.</param>
        /// <returns>The output matrix.</returns>
        private static BitMatrix convertByteMatrixToBitMatrix(ByteMatrix matrix)
        {
            var matrixWidgth = matrix.Width;
            var matrixHeight = matrix.Height;

            var output = new BitMatrix(matrixWidgth, matrixHeight);
            output.clear();
            for (var i = 0; i < matrixWidgth; i++)
                for (var j = 0; j < matrixHeight; j++)
                    // Zero is white in the bytematrix
                    if (matrix[i, j] == 1)
                        output[i, j] = true;

            return output;
        }
    }
}

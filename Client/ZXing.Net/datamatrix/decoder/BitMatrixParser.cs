using System;
using ZXing.Common;

namespace ZXing.Datamatrix.Internal
{
    /// <summary>
    ///     <author>bbrown@google.com (Brian Brown)</author>
    /// </summary>
    internal sealed class BitMatrixParser
    {
        private readonly BitMatrix mappingBitMatrix;
        private readonly BitMatrix readMappingMatrix;
        private readonly Version version;

        /// <summary>
        ///     <param name="bitMatrix"><see cref="BitMatrix" />to parse</param>
        ///     <exception cref="FormatException">if dimension is < 8 or>144 or not 0 mod 2</exception>
        /// </summary>
        internal BitMatrixParser(BitMatrix bitMatrix)
        {
            var dimension = bitMatrix.Height;
            if (dimension < 8 ||
                dimension > 144 ||
                (dimension & 0x01) != 0)
                return;

            version = readVersion(bitMatrix);
            if (version != null)
            {
                mappingBitMatrix = extractDataRegion(bitMatrix);
                readMappingMatrix = new BitMatrix(mappingBitMatrix.Width, mappingBitMatrix.Height);
            }
        }

        public Version Version { get { return version; } }

        /// <summary>
        ///     <p>
        ///         Creates the version object based on the dimension of the original bit matrix from
        ///         the datamatrix code.
        ///     </p>
        ///     <p>See ISO 16022:2006 Table 7 - ECC 200 symbol attributes</p>
        ///     <param name="bitMatrix">Original <see cref="BitMatrix" />including alignment patterns</param>
        ///     <returns><see cref="Version" />encapsulating the Data Matrix Code's "version"</returns>
        ///     <exception cref="FormatException">if the dimensions of the mapping matrix are not valid</exception>
        ///     Data Matrix dimensions.
        /// </summary>
        internal static Version readVersion(BitMatrix bitMatrix)
        {
            var numRows = bitMatrix.Height;
            var numColumns = bitMatrix.Width;
            return Version.getVersionForDimensions(numRows, numColumns);
        }

        /// <summary>
        ///     <p>
        ///         Reads the bits in the <see cref="BitMatrix" />representing the mapping matrix (No alignment patterns)
        ///         in the correct order in order to reconstitute the codewords bytes contained within the
        ///         Data Matrix Code.
        ///     </p>
        ///     <returns>bytes encoded within the Data Matrix Code</returns>
        ///     <exception cref="FormatException">if the exact number of bytes expected is not read</exception>
        /// </summary>
        internal byte[] readCodewords()
        {
            var result = new byte[version.getTotalCodewords()];
            var resultOffset = 0;

            var row = 4;
            var column = 0;

            var numRows = mappingBitMatrix.Height;
            var numColumns = mappingBitMatrix.Width;

            var corner1Read = false;
            var corner2Read = false;
            var corner3Read = false;
            var corner4Read = false;

            // Read all of the codewords
            do
            {
                // Check the four corner cases
                if ((row == numRows) &&
                    (column == 0) &&
                    !corner1Read)
                {
                    result[resultOffset++] = (byte)readCorner1(numRows, numColumns);
                    row -= 2;
                    column += 2;
                    corner1Read = true;
                }
                else if ((row == numRows - 2) &&
                         (column == 0) &&
                         ((numColumns & 0x03) != 0) &&
                         !corner2Read)
                {
                    result[resultOffset++] = (byte)readCorner2(numRows, numColumns);
                    row -= 2;
                    column += 2;
                    corner2Read = true;
                }
                else if ((row == numRows + 4) &&
                         (column == 2) &&
                         ((numColumns & 0x07) == 0) &&
                         !corner3Read)
                {
                    result[resultOffset++] = (byte)readCorner3(numRows, numColumns);
                    row -= 2;
                    column += 2;
                    corner3Read = true;
                }
                else if ((row == numRows - 2) &&
                         (column == 0) &&
                         ((numColumns & 0x07) == 4) &&
                         !corner4Read)
                {
                    result[resultOffset++] = (byte)readCorner4(numRows, numColumns);
                    row -= 2;
                    column += 2;
                    corner4Read = true;
                }
                else
                {
                    // Sweep upward diagonally to the right
                    do
                    {
                        if ((row < numRows) &&
                            (column >= 0) &&
                            !readMappingMatrix[column, row])
                            result[resultOffset++] = (byte)readUtah(row, column, numRows, numColumns);
                        row -= 2;
                        column += 2;
                    } while ((row >= 0) &&
                             (column < numColumns));
                    row += 1;
                    column += 3;

                    // Sweep downward diagonally to the left
                    do
                    {
                        if ((row >= 0) &&
                            (column < numColumns) &&
                            !readMappingMatrix[column, row])
                            result[resultOffset++] = (byte)readUtah(row, column, numRows, numColumns);
                        row += 2;
                        column -= 2;
                    } while ((row < numRows) &&
                             (column >= 0));
                    row += 3;
                    column += 1;
                }
            } while ((row < numRows) ||
                     (column < numColumns));

            if (resultOffset != version.getTotalCodewords())
                return null;
            return result;
        }

        /// <summary>
        ///     <p>Reads a bit of the mapping matrix accounting for boundary wrapping.</p>
        ///     <param name="row">Row to read in the mapping matrix</param>
        ///     <param name="column">Column to read in the mapping matrix</param>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>value of the given bit in the mapping matrix</returns>
        /// </summary>
        private bool readModule(int row, int column, int numRows, int numColumns)
        {
            // Adjust the row and column indices based on boundary wrapping
            if (row < 0)
            {
                row += numRows;
                column += 4 - ((numRows + 4) & 0x07);
            }
            if (column < 0)
            {
                column += numColumns;
                row += 4 - ((numColumns + 4) & 0x07);
            }
            readMappingMatrix[column, row] = true;
            return mappingBitMatrix[column, row];
        }

        /// <summary>
        ///     <p>Reads the 8 bits of the standard Utah-shaped pattern.</p>
        ///     <p>See ISO 16022:2006, 5.8.1 Figure 6</p>
        ///     <param name="row">Current row in the mapping matrix, anchored at the 8th bit (LSB) of the pattern</param>
        ///     <param name="column">Current column in the mapping matrix, anchored at the 8th bit (LSB) of the pattern</param>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>byte from the utah shape</returns>
        /// </summary>
        private int readUtah(int row, int column, int numRows, int numColumns)
        {
            var currentByte = 0;
            if (readModule(row - 2, column - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row - 2, column - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row - 1, column - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row - 1, column - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row - 1, column, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row, column - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row, column - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(row, column, numRows, numColumns))
                currentByte |= 1;
            return currentByte;
        }

        /// <summary>
        ///     <p>Reads the 8 bits of the special corner condition 1.</p>
        ///     <p>See ISO 16022:2006, Figure F.3</p>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>byte from the Corner condition 1</returns>
        /// </summary>
        private int readCorner1(int numRows, int numColumns)
        {
            var currentByte = 0;
            if (readModule(numRows - 1, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 1, 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 1, 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(2, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(3, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            return currentByte;
        }

        /// <summary>
        ///     <p>Reads the 8 bits of the special corner condition 2.</p>
        ///     <p>See ISO 16022:2006, Figure F.4</p>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>byte from the Corner condition 2</returns>
        /// </summary>
        private int readCorner2(int numRows, int numColumns)
        {
            var currentByte = 0;
            if (readModule(numRows - 3, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 2, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 1, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 4, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 3, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            return currentByte;
        }

        /// <summary>
        ///     <p>Reads the 8 bits of the special corner condition 3.</p>
        ///     <p>See ISO 16022:2006, Figure F.5</p>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>byte from the Corner condition 3</returns>
        /// </summary>
        private int readCorner3(int numRows, int numColumns)
        {
            var currentByte = 0;
            if (readModule(numRows - 1, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 1, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 3, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 3, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            return currentByte;
        }

        /// <summary>
        ///     <p>Reads the 8 bits of the special corner condition 4.</p>
        ///     <p>See ISO 16022:2006, Figure F.6</p>
        ///     <param name="numRows">Number of rows in the mapping matrix</param>
        ///     <param name="numColumns">Number of columns in the mapping matrix</param>
        ///     <returns>byte from the Corner condition 4</returns>
        /// </summary>
        private int readCorner4(int numRows, int numColumns)
        {
            var currentByte = 0;
            if (readModule(numRows - 3, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 2, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(numRows - 1, 0, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 2, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(0, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(1, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(2, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            currentByte <<= 1;
            if (readModule(3, numColumns - 1, numRows, numColumns))
                currentByte |= 1;
            return currentByte;
        }

        /// <summary>
        ///     <p>
        ///         Extracts the data region from a <see cref="BitMatrix" />that contains
        ///         alignment patterns.
        ///     </p>
        ///     <param name="bitMatrix">Original <see cref="BitMatrix" />with alignment patterns</param>
        ///     <returns>BitMatrix that has the alignment patterns removed</returns>
        /// </summary>
        private BitMatrix extractDataRegion(BitMatrix bitMatrix)
        {
            var symbolSizeRows = version.getSymbolSizeRows();
            var symbolSizeColumns = version.getSymbolSizeColumns();

            if (bitMatrix.Height != symbolSizeRows)
                throw new ArgumentException("Dimension of bitMarix must match the version size");

            var dataRegionSizeRows = version.getDataRegionSizeRows();
            var dataRegionSizeColumns = version.getDataRegionSizeColumns();

            var numDataRegionsRow = symbolSizeRows / dataRegionSizeRows;
            var numDataRegionsColumn = symbolSizeColumns / dataRegionSizeColumns;

            var sizeDataRegionRow = numDataRegionsRow * dataRegionSizeRows;
            var sizeDataRegionColumn = numDataRegionsColumn * dataRegionSizeColumns;

            var bitMatrixWithoutAlignment = new BitMatrix(sizeDataRegionColumn, sizeDataRegionRow);
            for (var dataRegionRow = 0; dataRegionRow < numDataRegionsRow; ++dataRegionRow)
            {
                var dataRegionRowOffset = dataRegionRow * dataRegionSizeRows;
                for (var dataRegionColumn = 0; dataRegionColumn < numDataRegionsColumn; ++dataRegionColumn)
                {
                    var dataRegionColumnOffset = dataRegionColumn * dataRegionSizeColumns;
                    for (var i = 0; i < dataRegionSizeRows; ++i)
                    {
                        var readRowOffset = dataRegionRow * (dataRegionSizeRows + 2) + 1 + i;
                        var writeRowOffset = dataRegionRowOffset + i;
                        for (var j = 0; j < dataRegionSizeColumns; ++j)
                        {
                            var readColumnOffset = dataRegionColumn * (dataRegionSizeColumns + 2) + 1 + j;
                            if (bitMatrix[readColumnOffset, readRowOffset])
                            {
                                var writeColumnOffset = dataRegionColumnOffset + j;
                                bitMatrixWithoutAlignment[writeColumnOffset, writeRowOffset] = true;
                            }
                        }
                    }
                }
            }
            return bitMatrixWithoutAlignment;
        }
    }
}

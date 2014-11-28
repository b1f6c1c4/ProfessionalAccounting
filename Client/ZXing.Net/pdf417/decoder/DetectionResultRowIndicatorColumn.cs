using System;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     Represents a Column in the Detection Result
    /// </summary>
    /// <author>Guenther Grau</author>
    public sealed class DetectionResultRowIndicatorColumn : DetectionResultColumn
    {
        /// <summary>
        ///     Gets or sets a value indicating whether this instance is the left indicator
        /// </summary>
        /// <value><c>true</c> if this instance is left; otherwise, <c>false</c>.</value>
        public bool IsLeft { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZXing.PDF417.Internal.DetectionResultRowIndicatorColumn" /> class.
        /// </summary>
        /// <param name="box">Box.</param>
        /// <param name="isLeft">If set to <c>true</c> is left.</param>
        public DetectionResultRowIndicatorColumn(BoundingBox box, bool isLeft)
            : base(box)
        {
            IsLeft = isLeft;
        }

        /// <summary>
        ///     Sets the Row Numbers as Inidicator Columns
        /// </summary>
        public void setRowNumbers()
        {
            foreach (var cw in Codewords)
                if (cw != null)
                    cw.setRowNumberAsRowIndicatorColumn();
        }


        /// <summary>
        ///     TODO implement properly
        ///     TODO maybe we should add missing codewords to store the correct row number to make
        ///     finding row numbers for other columns easier
        ///     use row height count to make detection of invalid row numbers more reliable
        /// </summary>
        /// <returns>The indicator column row numbers.</returns>
        /// <param name="metadata">Metadata.</param>
        public int adjustCompleteIndicatorColumnRowNumbers(BarcodeMetadata metadata)
        {
            var codewords = Codewords;
            setRowNumbers(); // Assign this as an indicator column
            removeIncorrectCodewords(codewords, metadata);

            var top = IsLeft ? Box.TopLeft : Box.TopRight;
            var bottom = IsLeft ? Box.BottomLeft : Box.BottomRight;

            var firstRow = imageRowToCodewordIndex((int)top.Y);
            var lastRow = imageRowToCodewordIndex((int)bottom.Y);

            // We need to be careful using the average row height.  
            // Barcode could be skewed so that we have smaller and taller rows
            var averageRowHeight = (lastRow - firstRow) / (float)metadata.RowCount;

            // initialize loop
            var barcodeRow = -1;
            var maxRowHeight = 1;
            var currentRowHeight = 0;

            for (var codewordRow = firstRow; codewordRow < lastRow; codewordRow++)
            {
                var codeword = codewords[codewordRow];
                if (codeword == null)
                    continue;

                //      float expectedRowNumber = (codewordsRow - firstRow) / averageRowHeight;
                //      if (Math.abs(codeword.getRowNumber() - expectedRowNumber) > 2) {
                //        SimpleLog.log(LEVEL.WARNING,
                //            "Removing codeword, rowNumberSkew too high, codeword[" + codewordsRow + "]: Expected Row: " +
                //                expectedRowNumber + ", RealRow: " + codeword.getRowNumber() + ", value: " + codeword.getValue());
                //        codewords[codewordsRow] = null;
                //      }

                var rowDifference = codeword.RowNumber - barcodeRow;

                // TODO improve handling with case where first row indicator doesn't start with 0

                if (rowDifference == 0)
                    currentRowHeight++;
                else if (rowDifference == 1)
                {
                    maxRowHeight = Math.Max(maxRowHeight, currentRowHeight);
                    currentRowHeight = 1;
                    barcodeRow = codeword.RowNumber;
                }
                else if (rowDifference < 0 ||
                         codeword.RowNumber >= metadata.RowCount ||
                         rowDifference > codewordRow)
                    codewords[codewordRow] = null;
                else
                {
                    int checkedRows;
                    if (maxRowHeight > 2)
                        checkedRows = (maxRowHeight - 2) * rowDifference;
                    else
                        checkedRows = rowDifference;
                    var closePreviousCodewordFound = checkedRows > codewordRow;
                    for (var i = 1; i <= checkedRows && !closePreviousCodewordFound; i++)
                        // there must be (height * rowDifference) number of codewords missing. For now we assume height = 1.
                        // This should hopefully get rid of most problems already.
                        closePreviousCodewordFound = codewords[codewordRow - i] != null;
                    if (closePreviousCodewordFound)
                        codewords[codewordRow] = null;
                    else
                    {
                        barcodeRow = codeword.RowNumber;
                        currentRowHeight = 1;
                    }
                }
            }
            return (int)(averageRowHeight + 0.5);
        }

        /// <summary>
        ///     Gets the row heights.
        /// </summary>
        /// <returns>The row heights.</returns>
        public int[] getRowHeights()
        {
            var barcodeMetadata = getBarcodeMetadata();
            if (barcodeMetadata == null)
                return null;
            adjustIncompleteIndicatorColumnRowNumbers(barcodeMetadata);
            var result = new int[barcodeMetadata.RowCount];
            foreach (var codeword in Codewords)
                if (codeword != null)
                {
                    var rowNumber = codeword.RowNumber;
                    if (rowNumber >= result.Length)
                        return null;
                    result[rowNumber]++;
                } // else throw exception? (or return null)
            return result;
        }

        /// <summary>
        ///     Adjusts the in omplete indicator column row numbers.
        /// </summary>
        /// <param name="metadata">Metadata.</param>
        public int adjustIncompleteIndicatorColumnRowNumbers(BarcodeMetadata metadata)
        {
            // TODO maybe we should add missing codewords to store the correct row number to make
            // finding row numbers for other columns easier
            // use row height count to make detection of invalid row numbers more reliable

            var top = IsLeft ? Box.TopLeft : Box.TopRight;
            var bottom = IsLeft ? Box.BottomLeft : Box.BottomRight;

            var firstRow = imageRowToCodewordIndex((int)top.Y);
            var lastRow = imageRowToCodewordIndex((int)bottom.Y);

            // We need to be careful using the average row height.  
            // Barcode could be skewed so that we have smaller and taller rows
            var averageRowHeight = (lastRow - firstRow) / (float)metadata.RowCount;
            var codewords = Codewords;

            // initialize loop
            var barcodeRow = -1;
            var maxRowHeight = 1;
            var currentRowHeight = 0;

            for (var codewordRow = firstRow; codewordRow < lastRow; codewordRow++)
            {
                var codeword = codewords[codewordRow];
                if (codeword == null)
                    continue;

                codeword.setRowNumberAsRowIndicatorColumn();

                var rowDifference = codeword.RowNumber - barcodeRow;

                // TODO improve handling with case where first row indicator doesn't start with 0

                if (rowDifference == 0)
                    currentRowHeight++;
                else if (rowDifference == 1)
                {
                    maxRowHeight = Math.Max(maxRowHeight, currentRowHeight);
                    currentRowHeight = 1;
                    barcodeRow = codeword.RowNumber;
                }
                else if (codeword.RowNumber > metadata.RowCount)
                    Codewords[codewordRow] = null;
                else
                {
                    barcodeRow = codeword.RowNumber;
                    currentRowHeight = 1;
                }
            }
            return (int)(averageRowHeight + 0.5);
        }

        /// <summary>
        ///     Gets the barcode metadata.
        /// </summary>
        /// <returns>The barcode metadata.</returns>
        public BarcodeMetadata getBarcodeMetadata()
        {
            var codewords = Codewords;
            var barcodeColumnCount = new BarcodeValue();
            var barcodeRowCountUpperPart = new BarcodeValue();
            var barcodeRowCountLowerPart = new BarcodeValue();
            var barcodeECLevel = new BarcodeValue();
            foreach (var codeword in codewords)
            {
                if (codeword == null)
                    continue;
                codeword.setRowNumberAsRowIndicatorColumn();
                var rowIndicatorValue = codeword.Value % 30;
                var codewordRowNumber = codeword.RowNumber;
                if (!IsLeft)
                    codewordRowNumber += 2;
                switch (codewordRowNumber % 3)
                {
                    case 0:
                        barcodeRowCountUpperPart.setValue(rowIndicatorValue * 3 + 1);
                        break;
                    case 1:
                        barcodeECLevel.setValue(rowIndicatorValue / 3);
                        barcodeRowCountLowerPart.setValue(rowIndicatorValue % 3);
                        break;
                    case 2:
                        barcodeColumnCount.setValue(rowIndicatorValue + 1);
                        break;
                }
            }
            // Maybe we should check if we have ambiguous values?
            var barcodeColumnCountValues = barcodeColumnCount.getValue();
            var barcodeRowCountUpperPartValues = barcodeRowCountUpperPart.getValue();
            var barcodeRowCountLowerPartValues = barcodeRowCountLowerPart.getValue();
            var barcodeECLevelValues = barcodeECLevel.getValue();
            if ((barcodeColumnCountValues.Length == 0) ||
                (barcodeRowCountUpperPartValues.Length == 0) ||
                (barcodeRowCountLowerPartValues.Length == 0) ||
                (barcodeECLevelValues.Length == 0) ||
                barcodeColumnCountValues[0] < 1 ||
                barcodeRowCountUpperPartValues[0] + barcodeRowCountLowerPartValues[0] < PDF417Common.MIN_ROWS_IN_BARCODE ||
                barcodeRowCountUpperPartValues[0] + barcodeRowCountLowerPartValues[0] > PDF417Common.MAX_ROWS_IN_BARCODE)
                return null;
            var barcodeMetadata = new BarcodeMetadata(
                barcodeColumnCountValues[0],
                barcodeRowCountUpperPartValues[0],
                barcodeRowCountLowerPartValues[0],
                barcodeECLevelValues[0]);
            removeIncorrectCodewords(codewords, barcodeMetadata);
            return barcodeMetadata;
        }

        /// <summary>
        ///     Prune the codewords which do not match the metadata
        ///     TODO Maybe we should keep the incorrect codewords for the start and end positions?
        /// </summary>
        /// <param name="codewords">Codewords.</param>
        /// <param name="metadata">Metadata.</param>
        private void removeIncorrectCodewords(Codeword[] codewords, BarcodeMetadata metadata)
        {
            for (var row = 0; row < codewords.Length; row++)
            {
                var codeword = codewords[row];
                if (codeword == null)
                    continue;

                var indicatorValue = codeword.Value % 30;
                var rowNumber = codeword.RowNumber;

                // Row does not exist in the metadata
                if (rowNumber >= metadata.RowCount) // different to java rowNumber > metadata.RowCount
                {
                    codewords[row] = null; // remove this.
                    continue;
                }

                if (!IsLeft)
                    rowNumber += 2;

                switch (rowNumber % 3)
                {
                    default:
                    case 0:
                        if (indicatorValue * 3 + 1 != metadata.RowCountUpper)
                            codewords[row] = null;
                        break;

                    case 1:
                        if (indicatorValue % 3 != metadata.RowCountLower ||
                            indicatorValue / 3 != metadata.ErrorCorrectionLevel)
                            codewords[row] = null;
                        break;

                    case 2:
                        if (indicatorValue + 1 != metadata.ColumnCount)
                            codewords[row] = null;
                        break;
                }
            }
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents the current
        ///     <see cref="ZXing.PDF417.Internal.DetectionResultRowIndicatorColumn" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents the current
        ///     <see cref="ZXing.PDF417.Internal.DetectionResultRowIndicatorColumn" />.
        /// </returns>
        public override string ToString()
        {
            return "Is Left: " + IsLeft + " \n" + base.ToString();
        }
    }
}

namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     Metadata about a PDF417 Barcode
    /// </summary>
    /// <author>Guenther Grau</author>
    public sealed class BarcodeMetadata
    {
        public int ColumnCount { get; private set; }
        public int ErrorCorrectionLevel { get; private set; }
        public int RowCountUpper { get; private set; }
        public int RowCountLower { get; private set; }
        public int RowCount { get; private set; }

        public BarcodeMetadata(int columnCount, int rowCountUpperPart, int rowCountLowerPart, int errorCorrectionLevel)
        {
            ColumnCount = columnCount;
            ErrorCorrectionLevel = errorCorrectionLevel;
            RowCountUpper = rowCountUpperPart;
            RowCountLower = rowCountLowerPart;
            RowCount = rowCountLowerPart + rowCountUpperPart;
        }
    }
}

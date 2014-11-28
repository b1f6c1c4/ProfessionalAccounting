namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     A Codeword in the PDF417 barcode
    /// </summary>
    /// <author>Guenther Grau</author>
    public sealed class Codeword
    {
        /// <summary>
        ///     Default value for the RowNumber (-1 being an invalid real number)
        /// </summary>
        private static readonly int BARCODE_ROW_UNKNOWN = -1;

        public int StartX { get; private set; }
        public int EndX { get; private set; }
        public int Bucket { get; private set; }
        public int Value { get; private set; }
        public int RowNumber { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZXing.PDF417.Internal.Codeword" /> class.
        /// </summary>
        /// <param name="startX">Start x.</param>
        /// <param name="endX">End x.</param>
        /// <param name="bucket">Bucket.</param>
        /// <param name="value">Value.</param>
        public Codeword(int startX, int endX, int bucket, int value)
        {
            StartX = startX;
            EndX = endX;
            Bucket = bucket;
            Value = value;
            RowNumber = BARCODE_ROW_UNKNOWN;
        }

        /// <summary>
        ///     Gets the width.
        /// </summary>
        /// <value>The width.</value>
        public int Width { get { return EndX - StartX; } }

        /// <summary>
        ///     Gets a value indicating whether this instance has valid row number.
        /// </summary>
        /// <value><c>true</c> if this instance has valid row number; otherwise, <c>false</c>.</value>
        public bool HasValidRowNumber { get { return IsValidRowNumber(RowNumber); } }

        /// <summary>
        ///     Determines whether this instance is valid row number the specified rowNumber.
        /// </summary>
        /// <returns><c>true</c> if this instance is valid row number the specified rowNumber; otherwise, <c>false</c>.</returns>
        /// <param name="rowNumber">Row number.</param>
        public bool IsValidRowNumber(int rowNumber)
        {
            return rowNumber != BARCODE_ROW_UNKNOWN && Bucket == (rowNumber % 3) * 3;
        }

        /// <summary>
        ///     Sets the row number as the row's indicator column.
        /// </summary>
        public void setRowNumberAsRowIndicatorColumn()
        {
            RowNumber = (Value / 30) * 3 + Bucket / 3;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents the current <see cref="ZXing.PDF417.Internal.Codeword" />.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents the current <see cref="ZXing.PDF417.Internal.Codeword" />.</returns>
        public override string ToString()
        {
            return RowNumber + "|" + Value;
        }
    }
}

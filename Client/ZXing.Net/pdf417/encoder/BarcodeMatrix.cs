namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     Holds all of the information for a barcode in a format where it can be easily accessable
    ///     <author>Jacob Haynes</author>
    /// </summary>
    internal sealed class BarcodeMatrix
    {
        private readonly BarcodeRow[] matrix;
        private int currentRow;
        private readonly int height;
        private readonly int width;

        /// <summary>
        ///     <param name="height">the height of the matrix (Rows)</param>
        ///     <param name="width">the width of the matrix (Cols)</param>
        /// </summary>
        internal BarcodeMatrix(int height, int width)
        {
            matrix = new BarcodeRow[height];
            //Initializes the array to the correct width
            for (int i = 0, matrixLength = matrix.Length; i < matrixLength; i++)
                matrix[i] = new BarcodeRow((width + 4) * 17 + 1);
            this.width = width * 17;
            this.height = height;
            currentRow = -1;
        }

        internal void set(int x, int y, sbyte value) { matrix[y][x] = value; }

        /*
      internal void setMatrix(int x, int y, bool black)
      {
         set(x, y, (sbyte) (black ? 1 : 0));
      }
      */

        internal void startRow() { ++currentRow; }

        internal BarcodeRow getCurrentRow() { return matrix[currentRow]; }

        internal sbyte[][] getMatrix() { return getScaledMatrix(1, 1); }

        /*
      internal sbyte[][] getScaledMatrix(int Scale)
      {
         return getScaledMatrix(Scale, Scale);
      }
      */

        internal sbyte[][] getScaledMatrix(int xScale, int yScale)
        {
            var matrixOut = new sbyte[height * yScale][];
            for (var idx = 0; idx < height * yScale; idx++)
                matrixOut[idx] = new sbyte[width * xScale];
            var yMax = height * yScale;
            for (var ii = 0; ii < yMax; ii++)
                matrixOut[yMax - ii - 1] = matrix[ii / yScale].getScaledRow(xScale);
            return matrixOut;
        }
    }
}

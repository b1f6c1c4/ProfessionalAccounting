namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     <author>Jacob Haynes</author>
    /// </summary>
    internal sealed class BarcodeRow
    {
        private readonly sbyte[] row;
        //A tacker for position in the bar
        private int currentLocation;

        /// <summary>
        ///     Creates a Barcode row of the width
        /// </summary>
        /// <param name="width">The width.</param>
        internal BarcodeRow(int width)
        {
            row = new sbyte[width];
            currentLocation = 0;
        }

        /// <summary>
        ///     Sets a specific location in the bar
        ///     <param name="x">The location in the bar</param>
        ///     <param name="value">Black if true, white if false;</param>
        /// </summary>
        internal sbyte this[int x] { get { return row[x]; } set { row[x] = value; } }

        /// <summary>
        ///     Sets a specific location in the bar
        ///     <param name="x">The location in the bar</param>
        ///     <param name="black">Black if true, white if false;</param>
        /// </summary>
        internal void set(int x, bool black) { row[x] = (sbyte)(black ? 1 : 0); }

        /// <summary>
        ///     <param name="black">A boolean which is true if the bar black false if it is white</param>
        ///     <param name="width">How many spots wide the bar is.</param>
        /// </summary>
        internal void addBar(bool black, int width)
        {
            for (var ii = 0; ii < width; ii++)
                set(currentLocation++, black);
        }

        /*
      internal sbyte[] Row
      {
         get { return row; }
      }
      */

        /// <summary>
        ///     This function scales the row
        ///     <param name="scale">How much you want the image to be scaled, must be greater than or equal to 1.</param>
        ///     <returns>the scaled row</returns>
        /// </summary>
        internal sbyte[] getScaledRow(int scale)
        {
            var output = new sbyte[row.Length * scale];
            for (var i = 0; i < output.Length; i++)
                output[i] = row[i / scale];
            return output;
        }
    }
}

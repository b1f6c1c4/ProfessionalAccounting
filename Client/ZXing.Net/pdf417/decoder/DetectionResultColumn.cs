using System.Globalization;
using System.Text;

namespace ZXing.PDF417.Internal
{
    /// <summary>
    ///     Represents a Column in the Detection Result
    /// </summary>
    /// <author>Guenther Grau</author>
    public class DetectionResultColumn
    {
        /// <summary>
        ///     The maximum distance to search in the codeword array in both the positive and negative directions
        /// </summary>
        private const int MAX_NEARBY_DISTANCE = 5;

        /// <summary>
        ///     The Bounding Box around the column (in the BitMatrix)
        /// </summary>
        /// <value>The box.</value>
        public BoundingBox Box { get; private set; }

        /// <summary>
        ///     The Codewords the Box encodes for, offset by the Box minY.
        ///     Remember to Access this ONLY through GetCodeword(imageRow) if you're accessing it in that manner.
        /// </summary>
        /// <value>The codewords.</value>
        public Codeword[] Codewords { get; set; }

        // TODO convert this to a dictionary? Dictionary<imageRow, Codeword> ??

        /// <summary>
        ///     Initializes a new instance of the <see cref="ZXing.PDF417.Internal.DetectionResultColumn" /> class.
        /// </summary>
        /// <param name="box">The Bounding Box around the column (in the BitMatrix)</param>
        public DetectionResultColumn(BoundingBox box)
        {
            Box = BoundingBox.Create(box);
            Codewords = new Codeword[Box.MaxY - Box.MinY + 1];
        }

        /// <summary>
        ///     Converts the Image's Row to the index in the Codewords array
        /// </summary>
        /// <returns>The Codeword Index.</returns>
        /// <param name="imageRow">Image row.</param>
        public int IndexForRow(int imageRow)
        {
            return imageRow - Box.MinY;
        }

        /// <summary>
        ///     Converts the Codeword array index into a Row in the Image (BitMatrix)
        /// </summary>
        /// <returns>The Image Row.</returns>
        /// <param name="codewordIndex">Codeword index.</param>
        public int RowForIndex(int codewordIndex)
        {
            return Box.MinY + codewordIndex;
        }

        /// <summary>
        ///     Gets the codeword for a given row
        /// </summary>
        /// <returns>The codeword.</returns>
        /// <param name="imageRow">Image row.</param>
        public Codeword getCodeword(int imageRow)
        {
            return Codewords[imageRowToCodewordIndex(imageRow)];
        }

        /// <summary>
        ///     Gets the codeword closest to the specified row in the image
        /// </summary>
        /// <param name="imageRow">Image row.</param>
        public Codeword getCodewordNearby(int imageRow)
        {
            var codeword = getCodeword(imageRow);
            if (codeword != null)
                return codeword;
            for (var i = 1; i < MAX_NEARBY_DISTANCE; i++)
            {
                var nearImageRow = imageRowToCodewordIndex(imageRow) - i;
                if (nearImageRow >= 0)
                {
                    codeword = Codewords[nearImageRow];
                    if (codeword != null)
                        return codeword;
                }
                nearImageRow = imageRowToCodewordIndex(imageRow) + i;
                if (nearImageRow < Codewords.Length)
                {
                    codeword = Codewords[nearImageRow];
                    if (codeword != null)
                        return codeword;
                }
            }
            return null;
        }

        internal int imageRowToCodewordIndex(int imageRow) { return imageRow - Box.MinY; }

        /// <summary>
        ///     Sets the codeword for an image row
        /// </summary>
        /// <param name="imageRow">Image row.</param>
        /// <param name="codeword">Codeword.</param>
        public void setCodeword(int imageRow, Codeword codeword)
        {
            Codewords[IndexForRow(imageRow)] = codeword;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents the current
        ///     <see cref="ZXing.PDF417.Internal.DetectionResultColumn" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents the current
        ///     <see cref="ZXing.PDF417.Internal.DetectionResultColumn" />.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            var row = 0;
            foreach (var cw in Codewords)
                if (cw == null)
                    builder.AppendFormat(CultureInfo.InvariantCulture, "{0,3}:    |   \n", row++);
                else
                    builder.AppendFormat(
                                         CultureInfo.InvariantCulture,
                                         "{0,3}: {1,3}|{2,3}\n",
                                         row++,
                                         cw.RowNumber,
                                         cw.Value);
            return builder.ToString();
            // return "Valid Codewords: " + (from cw in Codewords where cw != null select cw).Count().ToString();
        }
    }
}

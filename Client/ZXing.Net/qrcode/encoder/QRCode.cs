using System;
using System.Text;

namespace ZXing.QrCode.Internal
{
    /// <author>satorux@google.com (Satoru Takabayashi) - creator</author>
    /// <author>dswitkin@google.com (Daniel Switkin) - ported from C++</author>
    public sealed class QRCode
    {
        /// <summary>
        /// </summary>
        public static int NUM_MASK_PATTERNS = 8;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QRCode" /> class.
        /// </summary>
        public QRCode()
        {
            MaskPattern = -1;
        }

        /// <summary>
        ///     Gets or sets the mode.
        /// </summary>
        /// <value>
        ///     The mode.
        /// </value>
        public Mode Mode { get; set; }

        /// <summary>
        ///     Gets or sets the EC level.
        /// </summary>
        /// <value>
        ///     The EC level.
        /// </value>
        public ErrorCorrectionLevel ECLevel { get; set; }

        /// <summary>
        ///     Gets or sets the version.
        /// </summary>
        /// <value>
        ///     The version.
        /// </value>
        public Version Version { get; set; }

        /// <summary>
        ///     Gets or sets the mask pattern.
        /// </summary>
        /// <value>
        ///     The mask pattern.
        /// </value>
        public int MaskPattern { get; set; }

        /// <summary>
        ///     Gets or sets the matrix.
        /// </summary>
        /// <value>
        ///     The matrix.
        /// </value>
        public ByteMatrix Matrix { get; set; }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override String ToString()
        {
            var result = new StringBuilder(200);
            result.Append("<<\n");
            result.Append(" mode: ");
            result.Append(Mode);
            result.Append("\n ecLevel: ");
            result.Append(ECLevel);
            result.Append("\n version: ");
            if (Version == null)
                result.Append("null");
            else
                result.Append(Version);
            result.Append("\n maskPattern: ");
            result.Append(MaskPattern);
            if (Matrix == null)
                result.Append("\n matrix: null\n");
            else
            {
                result.Append("\n matrix:\n");
                result.Append(Matrix);
            }
            result.Append(">>\n");
            return result.ToString();
        }

        /// <summary>
        ///     Check if "mask_pattern" is valid.
        /// </summary>
        /// <param name="maskPattern">The mask pattern.</param>
        /// <returns>
        ///     <c>true</c> if [is valid mask pattern] [the specified mask pattern]; otherwise, <c>false</c>.
        /// </returns>
        public static bool isValidMaskPattern(int maskPattern)
        {
            return maskPattern >= 0 && maskPattern < NUM_MASK_PATTERNS;
        }
    }
}

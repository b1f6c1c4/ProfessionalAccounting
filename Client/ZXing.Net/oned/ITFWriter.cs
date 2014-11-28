using System;
using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     This object renders a ITF code as a <see cref="BitMatrix" />.
    ///     <author>erik.barbara@gmail.com (Erik Barbara)</author>
    /// </summary>
    public sealed class ITFWriter : OneDimensionalCodeWriter
    {
        private static readonly int[] START_PATTERN = {1, 1, 1, 1};
        private static readonly int[] END_PATTERN = {3, 1, 1};

        /// <summary>
        ///     Encode the contents following specified format.
        ///     {@code width} and {@code height} are required size. This method may return bigger size
        ///     {@code BitMatrix} when specified size is too small. The user can set both {@code width} and
        ///     {@code height} to zero to get minimum size barcode. If negative value is set to {@code width}
        ///     or {@code height}, {@code IllegalArgumentException} is thrown.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="hints"></param>
        /// <returns></returns>
        public override BitMatrix encode(String contents,
                                         BarcodeFormat format,
                                         int width,
                                         int height,
                                         IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.ITF)
                throw new ArgumentException("Can only encode ITF, but got " + format);

            return base.encode(contents, format, width, height, hints);
        }

        /// <summary>
        ///     Encode the contents to bool array expression of one-dimensional barcode.
        ///     Start code and end code should be included in result, and side margins should not be included.
        ///     <returns>a {@code bool[]} of horizontal pixels (false = white, true = black)</returns>
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public override bool[] encode(String contents)
        {
            var length = contents.Length;
            if (length % 2 != 0)
                throw new ArgumentException("The lenght of the input should be even");
            if (length > 80)
                throw new ArgumentException(
                    "Requested contents should be less than 80 digits long, but got " + length);
            for (var i = 0; i < length; i++)
                if (!Char.IsDigit(contents[i]))
                    throw new ArgumentException(
                        "Requested contents should only contain digits, but got '" + contents[i] + "'");

            var result = new bool[9 + 9 * length];
            var pos = appendPattern(result, 0, START_PATTERN, true);
            for (var i = 0; i < length; i += 2)
            {
                var one = Convert.ToInt32(contents[i].ToString(), 10);
                var two = Convert.ToInt32(contents[i + 1].ToString(), 10);
                var encoding = new int[18];
                for (var j = 0; j < 5; j++)
                {
                    encoding[j << 1] = ITFReader.PATTERNS[one][j];
                    encoding[(j << 1) + 1] = ITFReader.PATTERNS[two][j];
                }
                pos += appendPattern(result, pos, encoding, true);
            }
            appendPattern(result, pos, END_PATTERN, true);

            return result;
        }
    }
}

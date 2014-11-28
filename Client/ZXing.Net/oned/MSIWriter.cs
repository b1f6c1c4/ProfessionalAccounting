using System;
using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     This object renders a MSI code as a <see cref="BitMatrix" />.
    /// </summary>
    public sealed class MSIWriter : OneDimensionalCodeWriter
    {
        private static readonly int[] startWidths = {2, 1};
        private static readonly int[] endWidths = {1, 2, 1};

        private static readonly int[][] numberWidths =
        {
            new[] {1, 2, 1, 2, 1, 2, 1, 2},
            new[] {1, 2, 1, 2, 1, 2, 2, 1},
            new[] {1, 2, 1, 2, 2, 1, 1, 2},
            new[] {1, 2, 1, 2, 2, 1, 2, 1},
            new[] {1, 2, 2, 1, 1, 2, 1, 2},
            new[] {1, 2, 2, 1, 1, 2, 2, 1},
            new[] {1, 2, 2, 1, 2, 1, 1, 2},
            new[] {1, 2, 2, 1, 2, 1, 2, 1},
            new[] {2, 1, 1, 2, 1, 2, 1, 2},
            new[] {2, 1, 1, 2, 1, 2, 2, 1}
        };

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
            if (format != BarcodeFormat.MSI)
                throw new ArgumentException("Can only encode MSI, but got " + format);
            return base.encode(contents, format, width, height, hints);
        }

        /// <summary>
        ///     Encode the contents to byte array expression of one-dimensional barcode.
        ///     Start code and end code should be included in result, and side margins should not be included.
        ///     <returns>a {@code boolean[]} of horizontal pixels (false = white, true = black)</returns>
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public override bool[] encode(String contents)
        {
            var length = contents.Length;
            for (var i = 0; i < length; i++)
            {
                var indexInString = MSIReader.ALPHABET_STRING.IndexOf(contents[i]);
                if (indexInString < 0)
                    throw new ArgumentException(
                        "Requested contents contains a not encodable character: '" + contents[i] + "'");
            }

            var codeWidth = 3 + length * 12 + 4;
            var result = new bool[codeWidth];
            var pos = appendPattern(result, 0, startWidths, true);
            for (var i = 0; i < length; i++)
            {
                var indexInString = MSIReader.ALPHABET_STRING.IndexOf(contents[i]);
                var widths = numberWidths[indexInString];
                pos += appendPattern(result, pos, widths, true);
            }
            appendPattern(result, pos, endWidths, true);
            return result;
        }
    }
}

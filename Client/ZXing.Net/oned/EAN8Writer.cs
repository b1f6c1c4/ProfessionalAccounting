using System;
using System.Collections.Generic;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     This object renders an EAN8 code as a <see cref="BitMatrix" />.
    ///     <author>aripollak@gmail.com (Ari Pollak)</author>
    /// </summary>
    public sealed class EAN8Writer : UPCEANWriter
    {
        private const int CODE_WIDTH = 3 + // start guard
                                       (7 * 4) + // left bars
                                       5 + // middle guard
                                       (7 * 4) + // right bars
                                       3; // end guard

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
            if (format != BarcodeFormat.EAN_8)
                throw new ArgumentException(
                    "Can only encode EAN_8, but got "
                    + format);

            return base.encode(contents, format, width, height, hints);
        }

        /// <summary>
        /// </summary>
        /// <returns>
        ///     a byte array of horizontal pixels (false = white, true = black)
        /// </returns>
        public override bool[] encode(String contents)
        {
            if (contents.Length < 7 ||
                contents.Length > 8)
                throw new ArgumentException(
                    "Requested contents should be 7 (without checksum digit) or 8 digits long, but got " +
                    contents.Length);
            foreach (var ch in contents)
                if (!Char.IsDigit(ch))
                    throw new ArgumentException("Requested contents should only contain digits, but got '" + ch + "'");
            if (contents.Length == 7)
                contents = CalculateChecksumDigitModulo10(contents);

            var result = new bool[CODE_WIDTH];
            var pos = 0;

            pos += appendPattern(result, pos, UPCEANReader.START_END_PATTERN, true);

            for (var i = 0; i <= 3; i++)
            {
                var digit = Int32.Parse(contents.Substring(i, 1));
                pos += appendPattern(result, pos, UPCEANReader.L_PATTERNS[digit], false);
            }

            pos += appendPattern(result, pos, UPCEANReader.MIDDLE_PATTERN, false);

            for (var i = 4; i <= 7; i++)
            {
                var digit = Int32.Parse(contents.Substring(i, 1));
                pos += appendPattern(result, pos, UPCEANReader.L_PATTERNS[digit], true);
            }
            appendPattern(result, pos, UPCEANReader.START_END_PATTERN, true);

            return result;
        }
    }
}

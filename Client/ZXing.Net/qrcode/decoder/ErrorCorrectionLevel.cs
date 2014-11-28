using System;

namespace ZXing.QrCode.Internal
{
    /// <summary>
    ///     <p>
    ///         See ISO 18004:2006, 6.5.1. This enum encapsulates the four error correction levels
    ///         defined by the QR code standard.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public sealed class ErrorCorrectionLevel
    {
        /// <summary> L = ~7% correction</summary>
        public static readonly ErrorCorrectionLevel L = new ErrorCorrectionLevel(0, 0x01, "L");

        /// <summary> M = ~15% correction</summary>
        public static readonly ErrorCorrectionLevel M = new ErrorCorrectionLevel(1, 0x00, "M");

        /// <summary> Q = ~25% correction</summary>
        public static readonly ErrorCorrectionLevel Q = new ErrorCorrectionLevel(2, 0x03, "Q");

        /// <summary> H = ~30% correction</summary>
        public static readonly ErrorCorrectionLevel H = new ErrorCorrectionLevel(3, 0x02, "H");

        private static readonly ErrorCorrectionLevel[] FOR_BITS = {M, L, H, Q};

        private readonly int bits;

        private ErrorCorrectionLevel(int ordinal, int bits, String name)
        {
            ordinal_Renamed_Field = ordinal;
            this.bits = bits;
            this.name = name;
        }

        /// <summary>
        ///     Gets the bits.
        /// </summary>
        public int Bits { get { return bits; } }

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public String Name { get { return name; } }

        private readonly int ordinal_Renamed_Field;
        private readonly String name;

        /// <summary>
        ///     Ordinals this instance.
        /// </summary>
        /// <returns></returns>
        public int ordinal() { return ordinal_Renamed_Field; }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override String ToString() { return name; }

        /// <summary>
        ///     Fors the bits.
        /// </summary>
        /// <param name="bits">int containing the two bits encoding a QR Code's error correction level</param>
        /// <returns>
        ///     <see cref="ErrorCorrectionLevel" /> representing the encoded error correction level
        /// </returns>
        public static ErrorCorrectionLevel forBits(int bits)
        {
            if (bits < 0 ||
                bits >= FOR_BITS.Length)
                throw new ArgumentException();
            return FOR_BITS[bits];
        }
    }
}

using System;
using ZXing.Common;

namespace ZXing.Aztec
{
    /// <summary>
    ///     The class holds the available options for the <see cref="AztecWriter" />
    /// </summary>
    [Serializable]
    public class AztecEncodingOptions : EncodingOptions
    {
        /// <summary>
        ///     Representing the minimal percentage of error correction words.
        ///     Note: an Aztec symbol should have a minimum of 25% EC words.
        /// </summary>
        public int? ErrorCorrection
        {
            get
            {
                if (Hints.ContainsKey(EncodeHintType.ERROR_CORRECTION))
                    return (int)Hints[EncodeHintType.ERROR_CORRECTION];
                return null;
            }
            set
            {
                if (value == null)
                {
                    if (Hints.ContainsKey(EncodeHintType.ERROR_CORRECTION))
                        Hints.Remove(EncodeHintType.ERROR_CORRECTION);
                }
                else
                    Hints[EncodeHintType.ERROR_CORRECTION] = value;
            }
        }

        /// <summary>
        ///     Specifies the required number of layers for an Aztec code:
        ///     a negative number (-1, -2, -3, -4) specifies a compact Aztec code
        ///     0 indicates to use the minimum number of layers (the default)
        ///     a positive number (1, 2, .. 32) specifies a normal (non-compact) Aztec code
        /// </summary>
        public int? Layers
        {
            get
            {
                if (Hints.ContainsKey(EncodeHintType.AZTEC_LAYERS))
                    return (int)Hints[EncodeHintType.AZTEC_LAYERS];
                return null;
            }
            set
            {
                if (value == null)
                {
                    if (Hints.ContainsKey(EncodeHintType.AZTEC_LAYERS))
                        Hints.Remove(EncodeHintType.AZTEC_LAYERS);
                }
                else
                    Hints[EncodeHintType.AZTEC_LAYERS] = value;
            }
        }
    }
}

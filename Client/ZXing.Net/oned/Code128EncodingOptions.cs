using System;
using ZXing.Common;

namespace ZXing.OneD
{
    /// <summary>
    ///     The class holds the available options for the QrCodeWriter
    /// </summary>
    [Serializable]
    public class Code128EncodingOptions : EncodingOptions
    {
        /// <summary>
        ///     if true, don't switch to codeset C for numbers
        /// </summary>
        public bool ForceCodesetB
        {
            get
            {
                if (Hints.ContainsKey(EncodeHintType.CODE128_FORCE_CODESET_B))
                    return (bool)Hints[EncodeHintType.CODE128_FORCE_CODESET_B];
                return false;
            }
            set { Hints[EncodeHintType.CODE128_FORCE_CODESET_B] = value; }
        }
    }
}

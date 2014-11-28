using System;

namespace ZXing.Common
{
    /// <summary>
    ///     Superclass of classes encapsulating types ECIs, according to "Extended Channel Interpretations"
    ///     5.3 of ISO 18004.
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public abstract class ECI
    {
        public virtual int Value { get { return value_Renamed; } }

        //UPGRADE_NOTE: Final was removed from the declaration of 'value '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private readonly int value_Renamed;

        internal ECI(int value_Renamed) { this.value_Renamed = value_Renamed; }

        /// <param name="value">
        ///     ECI value
        /// </param>
        /// <returns>
        ///     {@link ECI} representing ECI of given value, or null if it is legal but unsupported
        /// </returns>
        /// <throws>  IllegalArgumentException if ECI value is invalid </throws>
        public static ECI getECIByValue(int value_Renamed)
        {
            if (value_Renamed < 0 ||
                value_Renamed > 999999)
                throw new ArgumentException("Bad ECI value: " + value_Renamed);
            if (value_Renamed < 900)
                // Character set ECIs use 000000 - 000899
                return CharacterSetECI.getCharacterSetECIByValue(value_Renamed);
            return null;
        }
    }
}

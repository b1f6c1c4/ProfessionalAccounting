using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         See
    ///         <a href="http://www.nttdocomo.co.jp/english/service/imode/make/content/barcode/about/s2.html">
    ///             DoCoMo's documentation
    ///         </a>
    ///         about the result types represented by subclasses of this class.
    ///     </p>
    ///     <p>
    ///         Thanks to Jeff Griffin for proposing rewrite of these classes that relies less
    ///         on exception-based mechanisms during parsing.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal abstract class AbstractDoCoMoResultParser : ResultParser
    {
        internal static String[] matchDoCoMoPrefixedField(String prefix, String rawText, bool trim)
        {
            return matchPrefixedField(prefix, rawText, ';', trim);
        }

        internal static String matchSingleDoCoMoPrefixedField(String prefix, String rawText, bool trim)
        {
            return matchSinglePrefixedField(prefix, rawText, ';', trim);
        }
    }
}

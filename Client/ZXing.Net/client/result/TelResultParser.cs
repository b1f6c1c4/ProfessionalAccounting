namespace ZXing.Client.Result
{
    /// <summary>
    ///     Parses a "tel:" URI result, which specifies a phone number.
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class TelResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                (!rawText.StartsWith("tel:") && !rawText.StartsWith("TEL:")))
                return null;
            // Normalize "TEL:" to "tel:"
            var telURI = rawText.StartsWith("TEL:") ? "tel:" + rawText.Substring(4) : rawText;
            // Drop tel, query portion
            //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'System.String.IndexOf' which may throw an exception. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1101'"
            var queryStart = rawText.IndexOf('?', 4);
            var number = queryStart < 0 ? rawText.Substring(4) : rawText.Substring(4, (queryStart) - (4));
            return new TelParsedResult(number, telURI, null);
        }
    }
}

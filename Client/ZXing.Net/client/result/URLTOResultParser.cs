namespace ZXing.Client.Result
{
    /// <summary>
    ///     Parses the "URLTO" result format, which is of the form "URLTO:[title]:[url]".
    ///     This seems to be used sometimes, but I am not able to find documentation
    ///     on its origin or official format?
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class URLTOResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                (!rawText.StartsWith("urlto:") && !rawText.StartsWith("URLTO:")))
                return null;
            //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'System.String.IndexOf' which may throw an exception. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1101'"
            var titleEnd = rawText.IndexOf(':', 6);
            if (titleEnd < 0)
                return null;
            var title = titleEnd <= 6 ? null : rawText.Substring(6, (titleEnd) - (6));
            var uri = rawText.Substring(titleEnd + 1);
            return new URIParsedResult(uri, title);
        }
    }
}

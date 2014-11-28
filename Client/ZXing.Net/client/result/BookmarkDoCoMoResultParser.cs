namespace ZXing.Client.Result
{
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class BookmarkDoCoMoResultParser : AbstractDoCoMoResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                !rawText.StartsWith("MEBKM:"))
                return null;
            var title = matchSingleDoCoMoPrefixedField("TITLE:", rawText, true);
            var rawUri = matchDoCoMoPrefixedField("URL:", rawText, true);
            if (rawUri == null)
                return null;
            var uri = rawUri[0];
            if (!URIResultParser.isBasicallyValidURI(uri))
                return null;
            return new URIParsedResult(uri, title);
        }
    }
}

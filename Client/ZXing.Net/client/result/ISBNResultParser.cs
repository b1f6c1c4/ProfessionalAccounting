namespace ZXing.Client.Result
{
    /// <summary>
    ///     Parses strings of digits that represent a ISBN.
    /// </summary>
    /// <author>
    ///     jbreiden@google.com (Jeff Breidenbach)
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public class ISBNResultParser : ResultParser
    {
        /// <summary>
        ///     See <a href="http://www.bisg.org/isbn-13/for.dummies.html">ISBN-13 For Dummies</a>
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public override ParsedResult parse(ZXing.Result result)
        {
            var format = result.BarcodeFormat;
            if (format != BarcodeFormat.EAN_13)
                return null;
            var rawText = result.Text;
            var length = rawText.Length;
            if (length != 13)
                return null;
            if (!rawText.StartsWith("978") &&
                !rawText.StartsWith("979"))
                return null;

            return new ISBNParsedResult(rawText);
        }
    }
}

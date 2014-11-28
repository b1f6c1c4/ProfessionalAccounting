using System;

namespace ZXing.Client.Result
{
    /// <author>jbreiden@google.com (Jeff Breidenbach)</author>
    public sealed class ISBNParsedResult : ParsedResult
    {
        internal ISBNParsedResult(String isbn)
            : base(ParsedResultType.ISBN)
        {
            ISBN = isbn;
            displayResultValue = isbn;
        }

        public String ISBN { get; private set; }
    }
}

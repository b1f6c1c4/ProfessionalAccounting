using System;
using System.Text;

namespace ZXing.Client.Result
{
    /// <author>Sean Owen</author>
    public sealed class TelParsedResult : ParsedResult
    {
        public TelParsedResult(String number, String telURI, String title)
            : base(ParsedResultType.TEL)
        {
            Number = number;
            TelURI = telURI;
            Title = title;

            var result = new StringBuilder(20);
            maybeAppend(number, result);
            maybeAppend(title, result);
            displayResultValue = result.ToString();
        }

        public String Number { get; private set; }

        public String TelURI { get; private set; }

        public String Title { get; private set; }
    }
}

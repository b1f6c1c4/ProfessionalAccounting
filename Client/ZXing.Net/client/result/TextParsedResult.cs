using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     A simple result type encapsulating a string that has no further interpretation.
    /// </summary>
    /// <author>Sean Owen</author>
    public sealed class TextParsedResult : ParsedResult
    {
        public TextParsedResult(String text, String language)
            : base(ParsedResultType.TEXT)
        {
            Text = text;
            Language = language;
            displayResultValue = text;
        }

        public String Text { get; private set; }

        public String Language { get; private set; }
    }
}

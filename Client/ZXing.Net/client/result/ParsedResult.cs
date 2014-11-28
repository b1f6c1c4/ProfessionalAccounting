using System;
using System.Text;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         Abstract class representing the result of decoding a barcode, as more than
    ///         a String -- as some type of structured data. This might be a subclass which represents
    ///         a URL, or an e-mail address. {@link ResultParser#parseResult(Result)} will turn a raw
    ///         decoded string into the most appropriate type of structured representation.
    ///     </p>
    ///     <p>
    ///         Thanks to Jeff Griffin for proposing rewrite of these classes that relies less
    ///         on exception-based mechanisms during parsing.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public abstract class ParsedResult
    {
        protected string displayResultValue;

        public virtual ParsedResultType Type { get; private set; }
        public virtual String DisplayResult { get { return displayResultValue; } }

        protected ParsedResult(ParsedResultType type) { Type = type; }

        public override String ToString() { return DisplayResult; }

        public override bool Equals(object obj)
        {
            var other = obj as ParsedResult;
            if (other == null)
                return false;
            return other.Type.Equals(Type) && other.DisplayResult.Equals(DisplayResult);
        }

        public override int GetHashCode() { return Type.GetHashCode() + DisplayResult.GetHashCode(); }

        public static void maybeAppend(String value, StringBuilder result)
        {
            if (String.IsNullOrEmpty(value))
                return;

            // Don't add a newline before the first value
            if (result.Length > 0)
                result.Append('\n');
            result.Append(value);
        }

        public static void maybeAppend(String[] values, StringBuilder result)
        {
            if (values != null)
                foreach (var value in values)
                    maybeAppend(value, result);
        }
    }
}

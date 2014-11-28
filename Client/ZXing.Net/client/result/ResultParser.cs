using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         Abstract class representing the result of decoding a barcode, as more than
    ///         a String -- as some type of structured data. This might be a subclass which represents
    ///         a URL, or an e-mail address. {@link #parseResult(com.google.zxing.Result)} will turn a raw
    ///         decoded string into the most appropriate type of structured representation.
    ///     </p>
    ///     <p>
    ///         Thanks to Jeff Griffin for proposing rewrite of these classes that relies less
    ///         on exception-based mechanisms during parsing.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public abstract class ResultParser
    {
        private static readonly ResultParser[] PARSERS =
            {
                new BookmarkDoCoMoResultParser(),
                new AddressBookDoCoMoResultParser(),
                new EmailDoCoMoResultParser(),
                new AddressBookAUResultParser(),
                new VCardResultParser(),
                new BizcardResultParser(),
                new VEventResultParser(),
                new EmailAddressResultParser(),
                new SMTPResultParser(),
                new TelResultParser(),
                new SMSMMSResultParser(),
                new SMSTOMMSTOResultParser(),
                new GeoResultParser(),
                new WifiResultParser(),
                new URLTOResultParser(),
                new URIResultParser(),
                new ISBNResultParser(),
                new ProductResultParser(),
                new ExpandedProductResultParser(),
                new VINResultParser()
            };

#if SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || PORTABLE
      private static readonly Regex DIGITS = new Regex(@"\A(?:" + "\\d+" + @")\z");
      private static readonly Regex AMPERSAND = new Regex("&");
      private static readonly Regex EQUALS = new Regex("=");
#else
        private static readonly Regex DIGITS = new Regex(@"\A(?:" + "\\d+" + @")\z", RegexOptions.Compiled);
        private static readonly Regex AMPERSAND = new Regex("&", RegexOptions.Compiled);
        private static readonly Regex EQUALS = new Regex("=", RegexOptions.Compiled);
#endif

        /// <summary>
        ///     Attempts to parse the raw {@link Result}'s contents as a particular type
        ///     of information (email, URL, etc.) and return a {@link ParsedResult} encapsulating
        ///     the result of parsing.
        /// </summary>
        /// <param name="theResult">the raw <see cref="Result" /> to parse</param>
        /// <returns><see cref="ParsedResult" /> encapsulating the parsing result</returns>
        public abstract ParsedResult parse(ZXing.Result theResult);

        public static ParsedResult parseResult(ZXing.Result theResult)
        {
            foreach (var parser in PARSERS)
            {
                var result = parser.parse(theResult);
                if (result != null)
                    return result;
            }
            return new TextParsedResult(theResult.Text, null);
        }

        protected static void maybeAppend(String value, StringBuilder result)
        {
            if (value != null)
            {
                result.Append('\n');
                result.Append(value);
            }
        }

        protected static void maybeAppend(String[] value, StringBuilder result)
        {
            if (value != null)
                for (var i = 0; i < value.Length; i++)
                {
                    result.Append('\n');
                    result.Append(value[i]);
                }
        }

        protected static String[] maybeWrap(String value_Renamed)
        {
            return value_Renamed == null ? null : new[] {value_Renamed};
        }

        protected static String unescapeBackslash(String escaped)
        {
            if (escaped != null)
            {
                var backslash = escaped.IndexOf('\\');
                if (backslash >= 0)
                {
                    var max = escaped.Length;
                    var unescaped = new StringBuilder(max - 1);
                    unescaped.Append(escaped.ToCharArray(), 0, backslash);
                    var nextIsEscaped = false;
                    for (var i = backslash; i < max; i++)
                    {
                        var c = escaped[i];
                        if (nextIsEscaped || c != '\\')
                        {
                            unescaped.Append(c);
                            nextIsEscaped = false;
                        }
                        else
                            nextIsEscaped = true;
                    }
                    return unescaped.ToString();
                }
            }
            return escaped;
        }

        protected static int parseHexDigit(char c)
        {
            if (c >= 'a')
            {
                if (c <= 'f')
                    return 10 + (c - 'a');
            }
            else if (c >= 'A')
            {
                if (c <= 'F')
                    return 10 + (c - 'A');
            }
            else if (c >= '0')
                if (c <= '9')
                    return c - '0';
            return -1;
        }

        internal static bool isStringOfDigits(String value, int length)
        {
            return value != null && length > 0 && length == value.Length && DIGITS.Match(value).Success;
        }

        internal static bool isSubstringOfDigits(String value, int offset, int length)
        {
            if (value == null ||
                length <= 0)
                return false;
            var max = offset + length;
            return value.Length >= max && DIGITS.Match(value, offset, length).Success;
        }

        internal static IDictionary<string, string> parseNameValuePairs(String uri)
        {
            var paramStart = uri.IndexOf('?');
            if (paramStart < 0)
                return null;
            var result = new Dictionary<String, String>(3);
            foreach (var keyValue in AMPERSAND.Split(uri.Substring(paramStart + 1)))
                appendKeyValue(keyValue, result);
            return result;
        }

        private static void appendKeyValue(String keyValue, IDictionary<String, String> result)
        {
            var keyValueTokens = EQUALS.Split(keyValue, 2);
            if (keyValueTokens.Length == 2)
            {
                var key = keyValueTokens[0];
                var value = keyValueTokens[1];
                try
                {
                    //value = URLDecoder.decode(value, "UTF-8");
                    value = urlDecode(value);
                    result[key] = value;
                }
                catch (Exception uee)
                {
                    throw new InvalidOperationException("url decoding failed", uee); // can't happen
                }
                result[key] = value;
            }
        }

        internal static String[] matchPrefixedField(String prefix, String rawText, char endChar, bool trim)
        {
            IList<string> matches = null;
            var i = 0;
            var max = rawText.Length;
            while (i < max)
            {
                //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'System.String.IndexOf' which may throw an exception. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1101'"
                i = rawText.IndexOf(prefix, i);
                if (i < 0)
                    break;
                i += prefix.Length; // Skip past this prefix we found to start
                var start = i; // Found the start of a match here
                var done = false;
                while (!done)
                {
                    //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'System.String.IndexOf' which may throw an exception. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1101'"
                    i = rawText.IndexOf(endChar, i);
                    if (i < 0)
                    {
                        // No terminating end character? uh, done. Set i such that loop terminates and break
                        i = rawText.Length;
                        done = true;
                    }
                    else if (rawText[i - 1] == '\\')
                        // semicolon was escaped so continue
                        i++;
                    else
                    {
                        // found a match
                        if (matches == null)
                            matches = new List<string>();
                        var element = unescapeBackslash(rawText.Substring(start, (i) - (start)));
                        if (trim)
                            element = element.Trim();
                        if (!String.IsNullOrEmpty(element))
                            matches.Add(element);
                        i++;
                        done = true;
                    }
                }
            }
            if (matches == null ||
                (matches.Count == 0))
                return null;
            return SupportClass.toStringArray(matches);
        }

        internal static String matchSinglePrefixedField(String prefix, String rawText, char endChar, bool trim)
        {
            var matches = matchPrefixedField(prefix, rawText, endChar, trim);
            return matches == null ? null : matches[0];
        }

        protected static String urlDecode(String escaped)
        {
            // Should we better use HttpUtility.UrlDecode?
            // Is HttpUtility.UrlDecode available for all platforms?
            // What about encoding like UTF8?

            if (escaped == null)
                return null;
            var escapedArray = escaped.ToCharArray();

            var first = findFirstEscape(escapedArray);
            if (first < 0)
                return escaped;

            var max = escapedArray.Length;
            // final length is at most 2 less than original due to at least 1 unescaping
            var unescaped = new StringBuilder(max - 2);
            // Can append everything up to first escape character
            unescaped.Append(escapedArray, 0, first);

            for (var i = first; i < max; i++)
            {
                var c = escapedArray[i];
                if (c == '+')
                    // + is translated directly into a space
                    unescaped.Append(' ');
                else if (c == '%')
                    // Are there even two more chars? if not we will just copy the escaped sequence and be done
                    if (i >= max - 2)
                        unescaped.Append('%'); // append that % and move on
                    else
                    {
                        var firstDigitValue = parseHexDigit(escapedArray[++i]);
                        var secondDigitValue = parseHexDigit(escapedArray[++i]);
                        if (firstDigitValue < 0 ||
                            secondDigitValue < 0)
                        {
                            // bad digit, just move on
                            unescaped.Append('%');
                            unescaped.Append(escapedArray[i - 1]);
                            unescaped.Append(escapedArray[i]);
                        }
                        unescaped.Append((char)((firstDigitValue << 4) + secondDigitValue));
                    }
                else
                    unescaped.Append(c);
            }
            return unescaped.ToString();
        }

        private static int findFirstEscape(char[] escapedArray)
        {
            var max = escapedArray.Length;
            for (var i = 0; i < max; i++)
            {
                var c = escapedArray[i];
                if (c == '+' ||
                    c == '%')
                    return i;
            }
            return -1;
        }
    }
}

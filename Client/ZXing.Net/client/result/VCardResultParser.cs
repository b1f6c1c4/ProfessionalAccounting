using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Parses contact information formatted according to the VCard (2.1) format. This is not a complete
    ///     implementation but should parse information as commonly encoded in 2D barcodes.
    /// </summary>
    /// <authorSean Owen
    /// </author>
    internal sealed class VCardResultParser : ResultParser
    {
#if SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || PORTABLE
      private static readonly Regex BEGIN_VCARD = new Regex("BEGIN:VCARD", RegexOptions.IgnoreCase);
      private static readonly Regex VCARD_LIKE_DATE = new Regex(@"\A(?:" + "\\d{4}-?\\d{2}-?\\d{2}" + @")\z");
      private static readonly Regex CR_LF_SPACE_TAB = new Regex("\r\n[ \t]");
      private static readonly Regex NEWLINE_ESCAPE = new Regex("\\\\[nN]");
      private static readonly Regex VCARD_ESCAPES = new Regex("\\\\([,;\\\\])");
      private static readonly Regex EQUALS = new Regex("=");
      private static readonly Regex SEMICOLON = new Regex(";");
      private static readonly Regex UNESCAPED_SEMICOLONS = new Regex("(?<!\\\\);+");
      private static readonly Regex COMMA = new Regex(",");
      private static readonly Regex SEMICOLON_OR_COMMA = new Regex("[;,]");
#else
        private static readonly Regex BEGIN_VCARD = new Regex(
            "BEGIN:VCARD",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex VCARD_LIKE_DATE = new Regex(
            @"\A(?:" + "\\d{4}-?\\d{2}-?\\d{2}" + @")\z",
            RegexOptions.Compiled);

        private static readonly Regex CR_LF_SPACE_TAB = new Regex("\r\n[ \t]", RegexOptions.Compiled);
        private static readonly Regex NEWLINE_ESCAPE = new Regex("\\\\[nN]", RegexOptions.Compiled);
        private static readonly Regex VCARD_ESCAPES = new Regex("\\\\([,;\\\\])", RegexOptions.Compiled);
        private static readonly Regex EQUALS = new Regex("=", RegexOptions.Compiled);
        private static readonly Regex SEMICOLON = new Regex(";", RegexOptions.Compiled);
        private static readonly Regex UNESCAPED_SEMICOLONS = new Regex("(?<!\\\\);+", RegexOptions.Compiled);
        private static readonly Regex COMMA = new Regex(",", RegexOptions.Compiled);
        private static readonly Regex SEMICOLON_OR_COMMA = new Regex("[;,]", RegexOptions.Compiled);
#endif

        public override ParsedResult parse(ZXing.Result result)
        {
            // Although we should insist on the raw text ending with "END:VCARD", there's no reason
            // to throw out everything else we parsed just because this was omitted. In fact, Eclair
            // is doing just that, and we can't parse its contacts without this leniency.
            var rawText = result.Text;
            var m = BEGIN_VCARD.Match(rawText);
            if (!m.Success ||
                m.Index != 0)
                return null;
            var names = matchVCardPrefixedField("FN", rawText, true, false);
            if (names == null)
            {
                // If no display names found, look for regular name fields and format them
                names = matchVCardPrefixedField("N", rawText, true, false);
                formatNames(names);
            }
            var nicknameString = matchSingleVCardPrefixedField("NICKNAME", rawText, true, false);
            var nicknames = nicknameString == null ? null : COMMA.Split(nicknameString[0]);
            var phoneNumbers = matchVCardPrefixedField("TEL", rawText, true, false);
            var emails = matchVCardPrefixedField("EMAIL", rawText, true, false);
            var note = matchSingleVCardPrefixedField("NOTE", rawText, false, false);
            var addresses = matchVCardPrefixedField("ADR", rawText, true, true);
            var org = matchSingleVCardPrefixedField("ORG", rawText, true, true);
            var birthday = matchSingleVCardPrefixedField("BDAY", rawText, true, false);
            if (birthday != null &&
                !isLikeVCardDate(birthday[0]))
                birthday = null;
            var title = matchSingleVCardPrefixedField("TITLE", rawText, true, false);
            var urls = matchVCardPrefixedField("URL", rawText, true, false);
            var instantMessenger = matchSingleVCardPrefixedField("IMPP", rawText, true, false);
            var geoString = matchSingleVCardPrefixedField("GEO", rawText, true, false);
            var geo = geoString == null ? null : SEMICOLON_OR_COMMA.Split(geoString[0]);
            if (geo != null &&
                geo.Length != 2)
                geo = null;
            return new AddressBookParsedResult(
                toPrimaryValues(names),
                nicknames,
                null,
                toPrimaryValues(phoneNumbers),
                toTypes(phoneNumbers),
                toPrimaryValues(emails),
                toTypes(emails),
                toPrimaryValue(instantMessenger),
                toPrimaryValue(note),
                toPrimaryValues(addresses),
                toTypes(addresses),
                toPrimaryValue(org),
                toPrimaryValue(birthday),
                toPrimaryValue(title),
                toPrimaryValues(urls),
                geo);
        }

        public static List<List<String>> matchVCardPrefixedField(String prefix,
                                                                 String rawText,
                                                                 bool trim,
                                                                 bool parseFieldDivider)
        {
            List<List<String>> matches = null;
            var i = 0;
            var max = rawText.Length;

            while (i < max)
            {
                // At start or after newline, match prefix, followed by optional metadata 
                // (led by ;) ultimately ending in colon
                var matcher = new Regex("(?:^|\n)" + prefix + "(?:;([^:]*))?:", RegexOptions.IgnoreCase);

                if (i > 0)
                    i--; // Find from i-1 not i since looking at the preceding character
                var match = matcher.Match(rawText, i);
                if (!match.Success)
                    break;
                i = match.Index + match.Length;

                var metadataString = match.Groups[1].Value; // group 1 = metadata substring
                List<String> metadata = null;
                var quotedPrintable = false;
                String quotedPrintableCharset = null;
                if (metadataString != null)
                    foreach (var metadatum in SEMICOLON.Split(metadataString))
                    {
                        if (metadata == null)
                            metadata = new List<String>(1);
                        metadata.Add(metadatum);
                        var metadatumTokens = EQUALS.Split(metadatum, 2);
                        if (metadatumTokens.Length > 1)
                        {
                            var key = metadatumTokens[0];
                            var value = metadatumTokens[1];
                            if (String.Compare("ENCODING", key, StringComparison.OrdinalIgnoreCase) == 0 &&
                                String.Compare("QUOTED-PRINTABLE", value, StringComparison.OrdinalIgnoreCase) == 0)
                                quotedPrintable = true;
                            else if (String.Compare("CHARSET", key, StringComparison.OrdinalIgnoreCase) == 0)
                                quotedPrintableCharset = value;
                        }
                    }

                var matchStart = i; // Found the start of a match here

                while ((i = rawText.IndexOf('\n', i)) >= 0)
                    // Really, end in \r\n
                    if (i < rawText.Length - 1 && // But if followed by tab or space,
                        (rawText[i + 1] == ' ' || // this is only a continuation
                         rawText[i + 1] == '\t'))
                        i += 2; // Skip \n and continutation whitespace
                    else if (quotedPrintable && // If preceded by = in quoted printable
                             ((i >= 1 && rawText[i - 1] == '=') || // this is a continuation
                              (i >= 2 && rawText[i - 2] == '=')))
                        i++; // Skip \n
                    else
                        break;

                if (i < 0)
                    // No terminating end character? uh, done. Set i such that loop terminates and break
                    i = max;
                else if (i > matchStart)
                {
                    // found a match
                    if (matches == null)
                        matches = new List<List<String>>(1); // lazy init
                    if (i >= 1 &&
                        rawText[i - 1] == '\r')
                        i--; // Back up over \r, which really should be there
                    var element = rawText.Substring(matchStart, i - matchStart);
                    if (trim)
                        element = element.Trim();
                    if (quotedPrintable)
                    {
                        element = decodeQuotedPrintable(element, quotedPrintableCharset);
                        if (parseFieldDivider)
                            element = UNESCAPED_SEMICOLONS.Replace(element, "\n").Trim();
                    }
                    else
                    {
                        if (parseFieldDivider)
                            element = UNESCAPED_SEMICOLONS.Replace(element, "\n").Trim();
                        element = CR_LF_SPACE_TAB.Replace(element, "");
                        element = NEWLINE_ESCAPE.Replace(element, "\n");
                        element = VCARD_ESCAPES.Replace(element, "$1");
                    }
                    if (metadata == null)
                    {
                        var matched = new List<String>(1);
                        matched.Add(element);
                        matches.Add(matched);
                    }
                    else
                    {
                        metadata.Insert(0, element);
                        matches.Add(metadata);
                    }
                    i++;
                }
                else
                    i++;
            }

            return matches;
        }

        private static String decodeQuotedPrintable(String value, String charset)
        {
            var length = value.Length;
            var result = new StringBuilder(length);
            var fragmentBuffer = new MemoryStream();
            for (var i = 0; i < length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '\r':
                    case '\n':
                        break;
                    case '=':
                        if (i < length - 2)
                        {
                            var nextChar = value[i + 1];
                            if (nextChar == '\r' ||
                                nextChar == '\n')
                            {
                                // Ignore, it's just a continuation symbol
                            }
                            else
                            {
                                var nextNextChar = value[i + 2];
                                var firstDigit = parseHexDigit(nextChar);
                                var secondDigit = parseHexDigit(nextNextChar);
                                if (firstDigit >= 0 &&
                                    secondDigit >= 0)
                                    fragmentBuffer.WriteByte((byte)((firstDigit << 4) | secondDigit));
                                i += 2;
                            }
                        }
                        break;
                    default:
                        maybeAppendFragment(fragmentBuffer, charset, result);
                        result.Append(c);
                        break;
                }
            }
            maybeAppendFragment(fragmentBuffer, charset, result);
            return result.ToString();
        }

        private static void maybeAppendFragment(MemoryStream fragmentBuffer,
                                                String charset,
                                                StringBuilder result)
        {
            if (fragmentBuffer.Length > 0)
            {
                var fragmentBytes = fragmentBuffer.ToArray();
                String fragment;
                if (charset == null)
#if WindowsCE
               fragment = Encoding.Default.GetString(fragmentBytes, 0, fragmentBytes.Length);
#else
                    fragment = Encoding.UTF8.GetString(fragmentBytes, 0, fragmentBytes.Length);
#endif
                else
                    try
                    {
                        fragment = Encoding.GetEncoding(charset).GetString(fragmentBytes, 0, fragmentBytes.Length);
                    }
                    catch (Exception)
                    {
#if WindowsCE
    // WindowsCE doesn't support all encodings. But it is device depended.
    // So we try here the some different ones
                  if (charset == "ISO-8859-1")
                  {
                     fragment = Encoding.GetEncoding(1252).GetString(fragmentBytes, 0, fragmentBytes.Length);
                  }
                  else
                  {
                     fragment = Encoding.Default.GetString(fragmentBytes, 0, fragmentBytes.Length);
                  }
                  fragment = Encoding.Default.GetString(fragmentBytes, 0, fragmentBytes.Length);
#else
                        fragment = Encoding.UTF8.GetString(fragmentBytes, 0, fragmentBytes.Length);
#endif
                    }
                fragmentBuffer.Seek(0, SeekOrigin.Begin);
                fragmentBuffer.SetLength(0);
                result.Append(fragment);
            }
        }

        internal static List<String> matchSingleVCardPrefixedField(String prefix,
                                                                   String rawText,
                                                                   bool trim,
                                                                   bool parseFieldDivider)
        {
            var values = matchVCardPrefixedField(prefix, rawText, trim, parseFieldDivider);
            return values == null || values.Count == 0 ? null : values[0];
        }

        private static String toPrimaryValue(List<String> list)
        {
            return list == null || list.Count == 0 ? null : list[0];
        }

        private static String[] toPrimaryValues(ICollection<List<String>> lists)
        {
            if (lists == null ||
                lists.Count == 0)
                return null;
            var result = new List<String>(lists.Count);
            foreach (var list in lists)
            {
                var value = list[0];
                if (!String.IsNullOrEmpty(value))
                    result.Add(value);
            }
            return SupportClass.toStringArray(result);
        }

        private static String[] toTypes(ICollection<List<String>> lists)
        {
            if (lists == null ||
                lists.Count == 0)
                return null;
            var result = new List<String>(lists.Count);
            foreach (var list in lists)
            {
                String type = null;
                for (var i = 1; i < list.Count; i++)
                {
                    var metadatum = list[i];
                    var equals = metadatum.IndexOf('=');
                    if (equals < 0)
                    {
                        // take the whole thing as a usable label
                        type = metadatum;
                        break;
                    }
                    if (String.Compare("TYPE", metadatum.Substring(0, equals), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        type = metadatum.Substring(equals + 1);
                        break;
                    }
                }
                result.Add(type);
            }
            return SupportClass.toStringArray(result);
        }

        private static bool isLikeVCardDate(String value)
        {
            return value == null || VCARD_LIKE_DATE.Match(value).Success;
        }

        /**
       * Formats name fields of the form "Public;John;Q.;Reverend;III" into a form like
       * "Reverend John Q. Public III".
       *
       * @param names name values to format, in place
       */

        private static void formatNames(IEnumerable<List<String>> names)
        {
            if (names != null)
                foreach (var list in names)
                {
                    var name = list[0];
                    var components = new String[5];
                    var start = 0;
                    int end;
                    var componentIndex = 0;
                    while (componentIndex < components.Length - 1 &&
                           (end = name.IndexOf(';', start)) >= 0)
                    {
                        components[componentIndex] = name.Substring(start, end - start);


                        componentIndex++;
                        start = end + 1;
                    }
                    components[componentIndex] = name.Substring(start);
                    var newName = new StringBuilder(100);
                    maybeAppendComponent(components, 3, newName);
                    maybeAppendComponent(components, 1, newName);
                    maybeAppendComponent(components, 2, newName);
                    maybeAppendComponent(components, 0, newName);
                    maybeAppendComponent(components, 4, newName);
                    list.Insert(0, newName.ToString().Trim());
                }
        }

        private static void maybeAppendComponent(String[] components, int i, StringBuilder newName)
        {
            if (!String.IsNullOrEmpty(components[i]))
            {
                if (newName.Length > 0)
                    newName.Append(' ');
                newName.Append(components[i]);
            }
        }
    }
}

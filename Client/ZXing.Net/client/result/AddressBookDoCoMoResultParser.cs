using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Implements the "MECARD" address book entry format.
    ///     Supported keys: N, SOUND, TEL, EMAIL, NOTE, ADR, BDAY, URL, plus ORG
    ///     Unsupported keys: TEL-AV, NICKNAME
    ///     Except for TEL, multiple values for keys are also not supported;
    ///     the first one found takes precedence.
    ///     Our understanding of the MECARD format is based on this document:
    ///     http://www.mobicode.org.tw/files/OMIA%20Mobile%20Bar%20Code%20Standard%20v3.2.1.doc
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class AddressBookDoCoMoResultParser : AbstractDoCoMoResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                !rawText.StartsWith("MECARD:"))
                return null;
            var rawName = matchDoCoMoPrefixedField("N:", rawText, true);
            if (rawName == null)
                return null;
            var name = parseName(rawName[0]);
            var pronunciation = matchSingleDoCoMoPrefixedField("SOUND:", rawText, true);
            var phoneNumbers = matchDoCoMoPrefixedField("TEL:", rawText, true);
            var emails = matchDoCoMoPrefixedField("EMAIL:", rawText, true);
            var note = matchSingleDoCoMoPrefixedField("NOTE:", rawText, false);
            var addresses = matchDoCoMoPrefixedField("ADR:", rawText, true);
            var birthday = matchSingleDoCoMoPrefixedField("BDAY:", rawText, true);
            if (!isStringOfDigits(birthday, 8))
                // No reason to throw out the whole card because the birthday is formatted wrong.
                birthday = null;
            var urls = matchDoCoMoPrefixedField("URL:", rawText, true);

            // Although ORG may not be strictly legal in MECARD, it does exist in VCARD and we might as well
            // honor it when found in the wild.
            var org = matchSingleDoCoMoPrefixedField("ORG:", rawText, true);

            return new AddressBookParsedResult(
                maybeWrap(name),
                null,
                pronunciation,
                phoneNumbers,
                null,
                emails,
                null,
                null,
                note,
                addresses,
                null,
                org,
                birthday,
                null,
                urls,
                null);
        }

        private static String parseName(String name)
        {
            var comma = name.IndexOf(',');
            if (comma >= 0)
                // Format may be last,first; switch it around
                return name.Substring(comma + 1) + ' ' + name.Substring(0, comma);
            return name;
        }
    }
}

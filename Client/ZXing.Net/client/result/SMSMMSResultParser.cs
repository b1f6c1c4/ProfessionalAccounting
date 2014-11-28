using System;
using System.Collections.Generic;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         Parses an "sms:" URI result, which specifies a number to SMS and optional
    ///         "via" number. See
    ///         <a href="http://gbiv.com/protocols/uri/drafts/draft-antti-gsm-sms-url-04.txt">
    ///             the IETF draft
    ///         </a>
    ///         on this.
    ///     </p>
    ///     <p>
    ///         This actually also parses URIs starting with "mms:", "smsto:", "mmsto:", "SMSTO:", and
    ///         "MMSTO:", and treats them all the same way, and effectively converts them to an "sms:" URI
    ///         for purposes of forwarding to the platform.
    ///     </p>
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class SMSMMSResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null ||
                !(rawText.StartsWith("sms:") || rawText.StartsWith("SMS:") ||
                  rawText.StartsWith("mms:") || rawText.StartsWith("MMS:")))
                return null;

            // Check up front if this is a URI syntax string with query arguments
            var nameValuePairs = parseNameValuePairs(rawText);
            String subject = null;
            String body = null;
            var querySyntax = false;
            if (nameValuePairs != null &&
                nameValuePairs.Count != 0)
            {
                subject = nameValuePairs["subject"];
                body = nameValuePairs["body"];
                querySyntax = true;
            }

            // Drop sms, query portion
            //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'System.String.IndexOf' which may throw an exception. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1101'"
            var queryStart = rawText.IndexOf('?', 4);
            String smsURIWithoutQuery;
            // If it's not query syntax, the question mark is part of the subject or message
            if (queryStart < 0 ||
                !querySyntax)
                smsURIWithoutQuery = rawText.Substring(4);
            else
                smsURIWithoutQuery = rawText.Substring(4, (queryStart) - (4));

            var lastComma = -1;
            int comma;
            var numbers = new List<String>(1);
            var vias = new List<String>(1);
            while ((comma = smsURIWithoutQuery.IndexOf(',', lastComma + 1)) > lastComma)
            {
                var numberPart = smsURIWithoutQuery.Substring(lastComma + 1, comma);
                addNumberVia(numbers, vias, numberPart);
                lastComma = comma;
            }
            addNumberVia(numbers, vias, smsURIWithoutQuery.Substring(lastComma + 1));

            return new SMSParsedResult(
                SupportClass.toStringArray(numbers),
                SupportClass.toStringArray(vias),
                subject,
                body);
        }

        private static void addNumberVia(ICollection<String> numbers,
                                         ICollection<String> vias,
                                         String numberPart)
        {
            var numberEnd = numberPart.IndexOf(';');
            if (numberEnd < 0)
            {
                numbers.Add(numberPart);
                vias.Add(null);
            }
            else
            {
                numbers.Add(numberPart.Substring(0, numberEnd));
                var maybeVia = numberPart.Substring(numberEnd + 1);
                String via;
                if (maybeVia.StartsWith("via="))
                    via = maybeVia.Substring(4);
                else
                    via = null;
                vias.Add(via);
            }
        }
    }
}

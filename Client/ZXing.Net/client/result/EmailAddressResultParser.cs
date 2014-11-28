using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Represents a result that encodes an e-mail address, either as a plain address
    ///     like "joe@example.org" or a mailto: URL like "mailto:joe@example.org".
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class EmailAddressResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null)
                return null;
            String emailAddress;
            if (rawText.ToLower().StartsWith("mailto:"))
            {
                // If it starts with mailto:, assume it is definitely trying to be an email address
                emailAddress = rawText.Substring(7);
                var queryStart = emailAddress.IndexOf('?');
                if (queryStart >= 0)
                    emailAddress = emailAddress.Substring(0, queryStart);
                emailAddress = urlDecode(emailAddress);
                var nameValues = parseNameValuePairs(rawText);
                String subject = null;
                String body = null;
                if (nameValues != null)
                {
                    if (String.IsNullOrEmpty(emailAddress))
                        emailAddress = nameValues["to"];
                    subject = nameValues["subject"];
                    body = nameValues["body"];
                }
                return new EmailAddressParsedResult(emailAddress, subject, body, rawText);
            }

            if (!EmailDoCoMoResultParser.isBasicallyValidEmailAddress(rawText))
                return null;
            emailAddress = rawText;
            return new EmailAddressParsedResult(emailAddress, null, null, "mailto:" + emailAddress);
        }
    }
}

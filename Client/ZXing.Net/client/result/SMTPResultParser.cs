using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         Parses an "smtp:" URI result, whose format is not standardized but appears to be like:
    ///         <code>smtp[:subject[:body]]}</code>.
    ///     </p>
    ///     <p>See http://code.google.com/p/zxing/issues/detail?id=536</p>
    /// </summary>
    /// <author>Sean Owen</author>
    public class SMTPResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (!(rawText.StartsWith("smtp:") || rawText.StartsWith("SMTP:")))
                return null;
            var emailAddress = rawText.Substring(5);
            String subject = null;
            String body = null;
            var colon = emailAddress.IndexOf(':');
            if (colon >= 0)
            {
                subject = emailAddress.Substring(colon + 1);
                emailAddress = emailAddress.Substring(0, colon);
                colon = subject.IndexOf(':');
                if (colon >= 0)
                {
                    body = subject.Substring(colon + 1);
                    subject = subject.Substring(0, colon);
                }
            }
            var mailtoURI = "mailto:" + emailAddress;
            return new EmailAddressParsedResult(emailAddress, subject, body, mailtoURI);
        }
    }
}

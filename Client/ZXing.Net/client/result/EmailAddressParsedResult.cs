using System;
using System.Text;

namespace ZXing.Client.Result
{
    /// <author>Sean Owen</author>
    public sealed class EmailAddressParsedResult : ParsedResult
    {
        public String EmailAddress { get; private set; }
        public String Subject { get; private set; }
        public String Body { get; private set; }
        public String MailtoURI { get; private set; }

        internal EmailAddressParsedResult(String emailAddress, String subject, String body, String mailtoURI)
            : base(ParsedResultType.EMAIL_ADDRESS)
        {
            EmailAddress = emailAddress;
            Subject = subject;
            Body = body;
            MailtoURI = mailtoURI;

            var result = new StringBuilder(30);
            maybeAppend(EmailAddress, result);
            maybeAppend(Subject, result);
            maybeAppend(Body, result);
            displayResultValue = result.ToString();
        }
    }
}

using System;
using System.Text;

namespace ZXing.Client.Result
{
    /// <author>Sean Owen</author>
    public sealed class SMSParsedResult : ParsedResult
    {
        public SMSParsedResult(String number,
                               String via,
                               String subject,
                               String body)
            : this(new[] {number}, new[] {via}, subject, body) { }

        public SMSParsedResult(String[] numbers,
                               String[] vias,
                               String subject,
                               String body)
            : base(ParsedResultType.SMS)
        {
            Numbers = numbers;
            Vias = vias;
            Subject = subject;
            Body = body;
            SMSURI = getSMSURI();

            var result = new StringBuilder(100);
            maybeAppend(Numbers, result);
            maybeAppend(Subject, result);
            maybeAppend(Body, result);
            displayResultValue = result.ToString();
        }

        private String getSMSURI()
        {
            var result = new StringBuilder();
            result.Append("sms:");
            var first = true;
            for (var i = 0; i < Numbers.Length; i++)
            {
                if (first)
                    first = false;
                else
                    result.Append(',');
                result.Append(Numbers[i]);
                if (Vias != null &&
                    Vias[i] != null)
                {
                    result.Append(";via=");
                    result.Append(Vias[i]);
                }
            }
            var hasBody = Body != null;
            var hasSubject = Subject != null;
            if (hasBody || hasSubject)
            {
                result.Append('?');
                if (hasBody)
                {
                    result.Append("body=");
                    result.Append(Body);
                }
                if (hasSubject)
                {
                    if (hasBody)
                        result.Append('&');
                    result.Append("subject=");
                    result.Append(Subject);
                }
            }
            return result.ToString();
        }

        public String[] Numbers { get; private set; }

        public String[] Vias { get; private set; }

        public String Subject { get; private set; }

        public String Body { get; private set; }

        public String SMSURI { get; private set; }
    }
}

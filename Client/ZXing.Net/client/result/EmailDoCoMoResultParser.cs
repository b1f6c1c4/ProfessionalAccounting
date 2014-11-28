using System;
using System.Text.RegularExpressions;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Implements the "MATMSG" email message entry format.
    ///     Supported keys: TO, SUB, BODY
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class EmailDoCoMoResultParser : AbstractDoCoMoResultParser
    {
        private static readonly Regex ATEXT_ALPHANUMERIC =
            new Regex(
                @"\A(?:" + "[a-zA-Z0-9@.!#$%&'*+\\-/=?^_`{|}~]+" + @")\z"
#if !(SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || PORTABLE)
                ,
                RegexOptions.Compiled);
#else
);
#endif

        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (!rawText.StartsWith("MATMSG:"))
                return null;
            var rawTo = matchDoCoMoPrefixedField("TO:", rawText, true);
            if (rawTo == null)
                return null;
            var to = rawTo[0];
            if (!isBasicallyValidEmailAddress(to))
                return null;
            var subject = matchSingleDoCoMoPrefixedField("SUB:", rawText, false);
            var body = matchSingleDoCoMoPrefixedField("BODY:", rawText, false);

            return new EmailAddressParsedResult(to, subject, body, "mailto:" + to);
        }

        /**
       * This implements only the most basic checking for an email address's validity -- that it contains
       * an '@' and contains no characters disallowed by RFC 2822. This is an overly lenient definition of
       * validity. We want to generally be lenient here since this class is only intended to encapsulate what's
       * in a barcode, not "judge" it.
       */

        internal static bool isBasicallyValidEmailAddress(String email)
        {
            return email != null && ATEXT_ALPHANUMERIC.Match(email).Success && email.IndexOf('@') >= 0;
        }
    }
}

using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>
    ///         Parses an "smsto:" URI result, whose format is not standardized but appears to be like:
    ///         {@code smsto:number(:body)}.
    ///     </p>
    ///     <p>
    ///         This actually also parses URIs starting with "smsto:", "mmsto:", "SMSTO:", and
    ///         "MMSTO:", and treats them all the same way, and effectively converts them to an "sms:" URI
    ///         for purposes of forwarding to the platform.
    ///     </p>
    /// </summary>
    /// <author>Sean Owen</author>
    public class SMSTOMMSTOResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (!(rawText.StartsWith("smsto:") || rawText.StartsWith("SMSTO:") ||
                  rawText.StartsWith("mmsto:") || rawText.StartsWith("MMSTO:")))
                return null;
            // Thanks to dominik.wild for suggesting this enhancement to support
            // smsto:number:body URIs
            var number = rawText.Substring(6);
            String body = null;
            var bodyStart = number.IndexOf(':');
            if (bodyStart >= 0)
            {
                body = number.Substring(bodyStart + 1);
                number = number.Substring(0, bodyStart);
            }
            return new SMSParsedResult(number, null, null, body);
        }
    }
}

using System;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     <p>Parses a WIFI configuration string. Strings will be of the form:</p>
    ///     <p>{@code WIFI:T:[network type];S:[network SSID];P:[network password];H:[hidden?];;}</p>
    ///     <p>The fields can appear in any order. Only "S:" is required.</p>
    /// </summary>
    /// <author>Vikram Aggarwal</author>
    /// <author>Sean Owen</author>
    public class WifiResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (!rawText.StartsWith("WIFI:"))
                return null;
            var ssid = matchSinglePrefixedField("S:", rawText, ';', false);
            if (string.IsNullOrEmpty(ssid))
                return null;
            var pass = matchSinglePrefixedField("P:", rawText, ';', false);
            var type = matchSinglePrefixedField("T:", rawText, ';', false) ?? "nopass";

            var hidden = false;
#if WindowsCE
         try { hidden = Boolean.Parse(matchSinglePrefixedField("H:", rawText, ';', false)); } catch { }
#else
            Boolean.TryParse(matchSinglePrefixedField("H:", rawText, ';', false), out hidden);
#endif

            return new WifiParsedResult(type, ssid, pass, hidden);
        }
    }
}

using System;
using System.Text;

namespace ZXing.Client.Result
{
    /// <summary>
    /// </summary>
    /// <author>Vikram Aggarwal</author>
    public class WifiParsedResult : ParsedResult
    {
        public WifiParsedResult(String networkEncryption, String ssid, String password)
            : this(networkEncryption, ssid, password, false) { }

        public WifiParsedResult(String networkEncryption, String ssid, String password, bool hidden)
            : base(ParsedResultType.WIFI)
        {
            Ssid = ssid;
            NetworkEncryption = networkEncryption;
            Password = password;
            Hidden = hidden;

            var result = new StringBuilder(80);
            maybeAppend(Ssid, result);
            maybeAppend(NetworkEncryption, result);
            maybeAppend(Password, result);
            maybeAppend(hidden.ToString(), result);
            displayResultValue = result.ToString();
        }

        public String Ssid { get; private set; }

        public String NetworkEncryption { get; private set; }

        public String Password { get; private set; }

        public bool Hidden { get; private set; }
    }
}

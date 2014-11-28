using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ZXing.Client.Result
{
    /// <author>Sean Owen</author>
    public sealed class URIParsedResult : ParsedResult
    {
        private static readonly Regex USER_IN_HOST = new Regex(
            ":/*([^/@]+)@[^/]+"
#if !(SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || PORTABLE)
            ,
            RegexOptions.Compiled);
#else
);
#endif

        public String URI { get; private set; }

        public String Title { get; private set; }

        /// <returns>
        ///     true if the URI contains suspicious patterns that may suggest it intends to
        ///     mislead the user about its true nature. At the moment this looks for the presence
        ///     of user/password syntax in the host/authority portion of a URI which may be used
        ///     in attempts to make the URI's host appear to be other than it is. Example:
        ///     http://yourbank.com@phisher.com  This URI connects to phisher.com but may appear
        ///     to connect to yourbank.com at first glance.
        /// </returns>
        public bool PossiblyMaliciousURI { get; private set; }

        public URIParsedResult(String uri, String title)
            : base(ParsedResultType.URI)
        {
            URI = massageURI(uri);
            Title = title;
            PossiblyMaliciousURI = USER_IN_HOST.Match(URI).Success;

            var result = new StringBuilder(30);
            maybeAppend(Title, result);
            maybeAppend(URI, result);
            displayResultValue = result.ToString();
        }

        /// <summary>
        ///     Transforms a string that represents a URI into something more proper, by adding or canonicalizing
        ///     the protocol.
        /// </summary>
        private static String massageURI(String uri)
        {
            var protocolEnd = uri.IndexOf(':');
            if (protocolEnd < 0)
                // No protocol, assume http
                uri = "http://" + uri;
            else if (isColonFollowedByPortNumber(uri, protocolEnd))
                // Found a colon, but it looks like it is after the host, so the protocol is still missing
                uri = "http://" + uri;
            return uri;
        }

        private static bool isColonFollowedByPortNumber(String uri, int protocolEnd)
        {
            var start = protocolEnd + 1;
            var nextSlash = uri.IndexOf('/', start);
            if (nextSlash < 0)
                nextSlash = uri.Length;
            return ResultParser.isSubstringOfDigits(uri, start, nextSlash - start);
        }
    }
}

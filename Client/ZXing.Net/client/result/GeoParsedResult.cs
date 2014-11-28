using System;
using System.Globalization;
using System.Text;

namespace ZXing.Client.Result
{
    /// <author>Sean Owen</author>
    public sealed class GeoParsedResult : ParsedResult
    {
        internal GeoParsedResult(double latitude, double longitude, double altitude, String query)
            : base(ParsedResultType.GEO)
        {
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Query = query;
            GeoURI = getGeoURI();
            GoogleMapsURI = getGoogleMapsURI();
            displayResultValue = getDisplayResult();
        }

        /// <returns>
        ///     latitude in degrees
        /// </returns>
        public double Latitude { get; private set; }

        /// <returns>
        ///     longitude in degrees
        /// </returns>
        public double Longitude { get; private set; }

        /// <returns>
        ///     altitude in meters. If not specified, in the geo URI, returns 0.0
        /// </returns>
        public double Altitude { get; private set; }

        /// <return> query string associated with geo URI or null if none exists</return>
        public String Query { get; private set; }

        public String GeoURI { get; private set; }

        /// <returns>
        ///     a URI link to Google Maps which display the point on the Earth described
        ///     by this instance, and sets the zoom level in a way that roughly reflects the
        ///     altitude, if specified
        /// </returns>
        public String GoogleMapsURI { get; private set; }

        private String getDisplayResult()
        {
            var result = new StringBuilder(20);
            result.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0###########}", Latitude);
            result.Append(", ");
            result.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0###########}", Longitude);
            if (Altitude > 0.0)
            {
                result.Append(", ");
                result.AppendFormat(CultureInfo.InvariantCulture, "{0:0.0###########}", Altitude);
                result.Append('m');
            }
            if (Query != null)
            {
                result.Append(" (");
                result.Append(Query);
                result.Append(')');
            }
            return result.ToString();
        }

        private String getGeoURI()
        {
            var result = new StringBuilder();
            result.Append("geo:");
            result.Append(Latitude);
            result.Append(',');
            result.Append(Longitude);
            if (Altitude > 0)
            {
                result.Append(',');
                result.Append(Altitude);
            }
            if (Query != null)
            {
                result.Append('?');
                result.Append(Query);
            }
            return result.ToString();
        }

        private String getGoogleMapsURI()
        {
            var result = new StringBuilder(50);
            result.Append("http://maps.google.com/?ll=");
            result.Append(Latitude);
            result.Append(',');
            result.Append(Longitude);
            if (Altitude > 0.0f)
            {
                // Map altitude to zoom level, cleverly. Roughly, zoom level 19 is like a
                // view from 1000ft, 18 is like 2000ft, 17 like 4000ft, and so on.
                var altitudeInFeet = Altitude * 3.28;
                var altitudeInKFeet = (int)(altitudeInFeet / 1000.0);
                // No Math.log() available here, so compute log base 2 the old fashioned way
                // Here logBaseTwo will take on a value between 0 and 18 actually
                var logBaseTwo = 0;
                while (altitudeInKFeet > 1 &&
                       logBaseTwo < 18)
                {
                    altitudeInKFeet >>= 1;
                    logBaseTwo++;
                }
                var zoom = 19 - logBaseTwo;
                result.Append("&z=");
                result.Append(zoom);
            }
            return result.ToString();
        }
    }
}

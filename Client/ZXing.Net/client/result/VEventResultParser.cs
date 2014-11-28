using System;
using System.Globalization;

namespace ZXing.Client.Result
{
    /// <summary>
    ///     Partially implements the iCalendar format's "VEVENT" format for specifying a
    ///     calendar event. See RFC 2445. This supports SUMMARY, DTSTART and DTEND fields.
    /// </summary>
    /// <author>
    ///     Sean Owen
    /// </author>
    /// <author>
    ///     www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    internal sealed class VEventResultParser : ResultParser
    {
        public override ParsedResult parse(ZXing.Result result)
        {
            var rawText = result.Text;
            if (rawText == null)
                return null;
            var vEventStart = rawText.IndexOf("BEGIN:VEVENT");
            if (vEventStart < 0)
                return null;

            var summary = matchSingleVCardPrefixedField("SUMMARY", rawText, true);
            var start = matchSingleVCardPrefixedField("DTSTART", rawText, true);
            if (start == null)
                return null;
            var end = matchSingleVCardPrefixedField("DTEND", rawText, true);
            var duration = matchSingleVCardPrefixedField("DURATION", rawText, true);
            var location = matchSingleVCardPrefixedField("LOCATION", rawText, true);
            var organizer = stripMailto(matchSingleVCardPrefixedField("ORGANIZER", rawText, true));

            var attendees = matchVCardPrefixedField("ATTENDEE", rawText, true);
            if (attendees != null)
                for (var i = 0; i < attendees.Length; i++)
                    attendees[i] = stripMailto(attendees[i]);
            var description = matchSingleVCardPrefixedField("DESCRIPTION", rawText, true);

            var geoString = matchSingleVCardPrefixedField("GEO", rawText, true);
            double latitude;
            double longitude;
            if (geoString == null)
            {
                latitude = Double.NaN;
                longitude = Double.NaN;
            }
            else
            {
                var semicolon = geoString.IndexOf(';');
                if (semicolon < 0)
                    return null;
#if WindowsCE
            try { latitude = Double.Parse(geoString.Substring(0, semicolon), NumberStyles.Float, CultureInfo.InvariantCulture); }
            catch { return null; }
            try { longitude = Double.Parse(geoString.Substring(semicolon + 1), NumberStyles.Float, CultureInfo.InvariantCulture); }
            catch { return null; }
#else
                if (
                    !Double.TryParse(
                                     geoString.Substring(0, semicolon),
                                     NumberStyles.Float,
                                     CultureInfo.InvariantCulture,
                                     out latitude))
                    return null;
                if (
                    !Double.TryParse(
                                     geoString.Substring(semicolon + 1),
                                     NumberStyles.Float,
                                     CultureInfo.InvariantCulture,
                                     out longitude))
                    return null;
#endif
            }

            try
            {
                return new CalendarParsedResult(
                    summary,
                    start,
                    end,
                    duration,
                    location,
                    organizer,
                    attendees,
                    description,
                    latitude,
                    longitude);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static String matchSingleVCardPrefixedField(String prefix,
                                                            String rawText,
                                                            bool trim)
        {
            var values = VCardResultParser.matchSingleVCardPrefixedField(prefix, rawText, trim, false);
            return values == null || values.Count == 0 ? null : values[0];
        }

        private static String[] matchVCardPrefixedField(String prefix, String rawText, bool trim)
        {
            var values = VCardResultParser.matchVCardPrefixedField(prefix, rawText, trim, false);
            if (values == null ||
                values.Count == 0)
                return null;
            var size = values.Count;
            var result = new String[size];
            for (var i = 0; i < size; i++)
                result[i] = values[i][0];
            return result;
        }

        private static String stripMailto(String s)
        {
            if (s != null &&
                (s.StartsWith("mailto:") || s.StartsWith("MAILTO:")))
                s = s.Substring(7);
            return s;
        }
    }
}

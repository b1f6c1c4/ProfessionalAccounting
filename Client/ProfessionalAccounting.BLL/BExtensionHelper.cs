using System;
using System.Globalization;
using ProfessionalAccounting.Entities;

namespace ProfessionalAccounting.BLL
{
    public static class BExtensionHelper
    {
        public static string AsCurrency(this decimal value) { return String.Format("£¤{0:0.0000}", value); }

        public static string AsCurrency(this decimal? value)
        {
            return value.HasValue ? AsCurrency(value.Value) : String.Empty;
        }

        public static string AsPureCurrency(this decimal value) { return String.Format("{0:0.0000}", value); }

        public static string AsPureCurrency(this decimal? value)
        {
            return value.HasValue ? AsPureCurrency(value.Value) : String.Empty;
        }

        public static string AsID(this int value) { return String.Format("{0}", value); }
        public static string AsID(this int? value) { return value.HasValue ? AsID(value.Value) : String.Empty; }
        public static string AsTitle(this decimal value) { return String.Format("{0:0000.00}", value); }

        public static string AsTitle(this decimal? value)
        {
            return value.HasValue ? AsTitle(value.Value) : String.Empty;
        }

        public static string AsDT(this DateTime value) { return String.Format("{0:yyyyMMdd}", value); }
        public static string AsDT(this DateTime? value) { return value.HasValue ? AsDT(value.Value) : "ÎÞÈÕÆÚ"; }

        public static DateTime? AsDT(this string value)
        {
            DateTime val;
            if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.AllowWhiteSpaces, out val))
                return val;
            return null;
        }

        public static int SubtractMonth(this DateTime dt1, DateTime dt2)
        {
            return 12 * (dt2.Year - dt1.Year) + dt2.Month - dt1.Month;
        }

        public static string Restriction(this string s, int maxLength, bool appendix = false)
        {
            if (s.Length <= maxLength)
                return s;
            s = s.Substring(0, maxLength);
            if (appendix)
                return s + "..";
            return s;
        }

        public static PatternData GetDefaultData(this PatternUI pattern)
        {
            var data = new PatternData(pattern);
            data.ResetDefaultData();
            return data;
        }

        public static void ResetDefaultData(this PatternData data)
        {
            var sp = data.Pattern.UI.Split(',');
            for (var i = 0; i < sp.Length; i++)
                if (sp[i].StartsWith("E"))
                    data[i] = sp[i].Split(';')[2];
                else if (sp[i].StartsWith("O", StringComparison.InvariantCultureIgnoreCase))
                    data[i] = "0";
                else if (sp[i].StartsWith("DT"))
                    data[i] = DateTime.Now.AsDT();
        }
    }
}

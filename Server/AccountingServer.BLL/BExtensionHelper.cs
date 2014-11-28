using System;
using System.Collections.Generic;
using System.Globalization;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public static class BExtensionHelper
    {
        public static IEnumerable<DbDetail> SelectDetails(this IDbHelper db, DbItem entity)
        {
            return db.SelectDetails(new DbDetail {Item = entity.ID});
        }

        public static int DeleteDetails(this IDbHelper db, DbItem entity)
        {
            return db.DeleteDetails(new DbDetail {Item = entity.ID});
        }

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

        public static Decimal? AsCurrencyOrTitle(this string value)
        {
            Decimal val;
            if (Decimal.TryParse(value, out val))
                return val;
            return null;
        }

        public static void SetIDText(this DbItem entity, string value)
        {
            int val;
            if (Int32.TryParse(value, out val))
                entity.ID = val;
        }

        public static void SetDTText(this DbItem entity, string value) { entity.DT = value.AsDT(); }

        public static void SetRemarkText(this DbItem entity, string value)
        {
            if (String.IsNullOrEmpty(value))
                entity.Remark = null;
            else if (value == "\"\"")
                entity.Remark = String.Empty;
            else
                entity.Remark = value;
        }


        public static void SetItemText(this DbDetail entity, string value)
        {
            int val;
            if (Int32.TryParse(value, out val))
                entity.Item = val;
        }

        public static void SetTitleText(this DbDetail entity, string value)
        {
            Decimal val;
            if (Decimal.TryParse(value, out val))
                entity.Title = val;
        }

        public static void SetFundText(this DbDetail entity, string value)
        {
            Decimal val;
            if (Decimal.TryParse(value, out val))
                entity.Fund = val;
        }

        public static void SetRemarkText(this DbDetail entity, string value)
        {
            if (String.IsNullOrEmpty(value))
                entity.Remark = null;
            else if (value == "\"\"")
                entity.Remark = String.Empty;
            else
                entity.Remark = value;
        }

        public static string GetSemiFullText(this DbDetail entity)
        {
            return String.Format("{0} {1} {2}", entity.Title.AsTitle(), entity.Remark, entity.Fund.AsCurrency());
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
    }
}

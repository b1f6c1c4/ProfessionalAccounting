using System;
using System.Collections.Generic;
using System.Globalization;
using AccountingServer.DAL;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public static class BExtensionHelper
    {
        public static IEnumerable<VoucherDetail> SelectDetails(this IDbHelper db, Voucher entity)
        {
            return db.SelectDetails(new VoucherDetail {Item = entity.ID});
        }

        public static int DeleteDetails(this IDbHelper db, Voucher entity)
        {
            return db.DeleteDetails(new VoucherDetail {Item = entity.ID});
        }

        public static string AsCurrency(this double value) { return String.Format("£¤{0:0.0000}", value); }

        public static string AsCurrency(this double? value)
        {
            return value.HasValue ? AsCurrency(value.Value) : String.Empty;
        }

        public static string AsPureCurrency(this double value) { return String.Format("{0:0.0000}", value); }

        public static string AsPureCurrency(this double? value)
        {
            return value.HasValue ? AsPureCurrency(value.Value) : String.Empty;
        }

        public static string AsID(this int value) { return String.Format("{0}", value); }
        public static string AsID(this int? value) { return value.HasValue ? AsID(value.Value) : String.Empty; }
        public static string AsTitle(this int value) { return String.Format("{0:0000}", value); }
        public static string AsSubTitle(this int value) { return String.Format("{0:00}", value); }

        public static string AsTitle(this int? value)
        {
            return value.HasValue ? AsTitle(value.Value) : String.Empty;
        }
        public static string AsSubTitle(this int? value)
        {
            return value.HasValue ? AsSubTitle(value.Value) : String.Empty;
        }

        public static string AsDate(this DateTime value) { return String.Format("{0:yyyyMMdd}", value); }
        public static string AsDate(this DateTime? value) { return value.HasValue ? AsDate(value.Value) : "ÎÞÈÕÆÚ"; }

        public static DateTime? AsDate(this string value)
        {
            DateTime val;
            if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.AllowWhiteSpaces, out val))
                return val;
            return null;
        }

        public static double? AsCurrency(this string value)
        {
            double val;
            if (double.TryParse(value, out val))
                return val;
            return null;
        }
        public static int? AsTitleOrSubTitle(this string value)
        {
            int val;
            if (Int32.TryParse(value, out val))
                return val;
            return null;
        }

        //public static void SetIDText(this Voucher entity, string value)
        //{
        //    int val;
        //    if (Int32.TryParse(value, out val))
        //        entity.ID = val;
        //}

        public static void SetDTText(this Voucher entity, string value) { entity.Date = value.AsDate(); }

        //public static void SetRemarkText(this Voucher entity, string value)
        //{
        //    if (String.IsNullOrEmpty(value))
        //        entity.Content = null;
        //    else if (value == "\"\"")
        //        entity.Content = String.Empty;
        //    else
        //        entity.Content = value;
        //}


        //public static void SetItemText(this VoucherDetail entity, string value)
        //{
        //    int val;
        //    if (Int32.TryParse(value, out val))
        //        entity.Item = val;
        //}

        public static void SetTitleText(this VoucherDetail entity, string value)
        {
            Int32 val;
            if (Int32.TryParse(value, out val))
                entity.Title = val;
        }
        public static void SetSubTitleText(this VoucherDetail entity, string value)
        {
            Int32 val;
            if (Int32.TryParse(value, out val))
                entity.SubTitle = val;
        }

        public static void SetFundText(this VoucherDetail entity, string value)
        {
            double val;
            if (double.TryParse(value, out val))
                entity.Fund = val;
        }

        //public static void SetRemarkText(this VoucherDetail entity, string value)
        //{
        //    if (String.IsNullOrEmpty(value))
        //        entity.Content = null;
        //    else if (value == "\"\"")
        //        entity.Content = String.Empty;
        //    else
        //        entity.Content = value;
        //}

        public static string GetSemiFullText(this VoucherDetail entity)
        {
            return String.Format(
                                 "{0}{1}-{2} {3}{4}",
                                 entity.Title.AsTitle(),
                                 entity.SubTitle.AsSubTitle(),
                                 entity.Content,
                                 entity.Fund.AsCurrency(),
                                 entity.Remark == null ? String.Empty : " (" + entity.Content + ")");
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

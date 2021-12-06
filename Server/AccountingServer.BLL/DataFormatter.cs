using System;
using System.Globalization;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     ��ʽ������
    /// </summary>
    public static class DataFormatter
    {
        /// <summary>
        ///     ��ʽ�����ÿո����ĩβ����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsCurrency(this double value)
        {
            var s = $"{value:C4}";
            return s.TrimEnd('0').CPadRight(s.Length);
        }

        /// <summary>
        ///     ��ʽ�����ÿո����ĩβ����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsCurrency(this double? value) => value.HasValue ? AsCurrency(value.Value) : string.Empty;

        /// <summary>
        ///     ��ʽ��һ����Ŀ���
        /// </summary>
        /// <param name="value">һ����Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsTitle(this int value) => $"{value:0000}";

        /// <summary>
        ///     ��ʽ��һ����Ŀ���
        /// </summary>
        /// <param name="value">һ����Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsTitle(this int? value) => value.HasValue ? AsTitle(value.Value) : string.Empty;

        /// <summary>
        ///     ��ʽ��������Ŀ���
        /// </summary>
        /// <param name="value">������Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsSubTitle(this int value) => $"{value:00}";

        /// <summary>
        ///     ��ʽ��������Ŀ���
        /// </summary>
        /// <param name="value">������Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsSubTitle(this int? value) => value.HasValue ? AsSubTitle(value.Value) : string.Empty;


        /// <summary>
        ///     ��ʽ������
        /// </summary>
        /// <param name="value">����</param>
        /// <returns>��ʽ���������</returns>
        public static string AsDate(this DateTime value) => $"{value:yyyyMMdd}";

        /// <summary>
        ///     ��ʽ������
        /// </summary>
        /// <param name="value">����</param>
        /// <returns>��ʽ���������</returns>
        public static string AsDate(this DateTime? value) => value.HasValue ? AsDate(value.Value) : "[null]";

        /// <summary>
        ///     ��ʽ������
        /// </summary>
        /// <param name="value">����</param>
        /// <param name="level">������</param>
        /// <returns>��ʽ���������</returns>
        public static string AsDate(this DateTime? value, SubtotalLevel level)
        {
            if (!value.HasValue)
                return "[null]";

            switch (level)
            {
                case SubtotalLevel.Day:
                case SubtotalLevel.Week:
                    return value.AsDate();
                case SubtotalLevel.Month:
                    return $"@{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.FinancialMonth:
                    return $"{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.BillingMonth:
                    return $"#{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.Year:
                    return $"{value.Value.Year:D4}";
                default:
                    throw new ArgumentException("�����β��ǻ�������", nameof(level));
            }
        }

        /// <summary>
        ///     ��������
        /// </summary>
        /// <param name="value">��ʽ���������</param>
        /// <returns>����</returns>
        public static DateTime? AsDate(this string value)
        {
            DateTime val;
            if (DateTime.TryParseExact(value, "yyyyMMdd", null, DateTimeStyles.AllowWhiteSpaces, out val))
                return val;
            return null;
        }

        /// <summary>
        ///     �������
        /// </summary>
        /// <param name="value">��ʽ����Ľ��</param>
        /// <returns>���</returns>
        public static double? AsCurrency(this string value)
        {
            double val;
            if (double.TryParse(value, out val))
                return val;
            return null;
        }

        /// <summary>
        ///     ������Ŀ���
        /// </summary>
        /// <param name="value">��ʽ����ı��</param>
        /// <returns>���</returns>
        public static int? AsTitleOrSubTitle(this string value)
        {
            int val;
            if (int.TryParse(value, out val))
                return val;
            return null;
        }
    }
}

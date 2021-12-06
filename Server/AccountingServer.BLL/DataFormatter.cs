using System;
using System.Globalization;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     ��ʽ������
    /// </summary>
    public static class DataFormatter
    {
        /// <summary>
        ///     ��ʽ�����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsCurrency(this double value)
        {
            var s = String.Format("��{0:0.0000}", value);
            return s.TrimEnd('0').PadRight(s.Length);
        }

        /// <summary>
        ///     ��ʽ�����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsFullCurrency(this double value)
        {
            return String.Format("��{0:0.0000}", value);
        }

        /// <summary>
        ///     ��ʽ�����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsCurrency(this double? value)
        {
            return value.HasValue ? AsCurrency(value.Value) : String.Empty;
        }

        /// <summary>
        ///     ��ʽ�����
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsFullCurrency(this double? value)
        {
            return value.HasValue ? AsFullCurrency(value.Value) : String.Empty;
        }

        /// <summary>
        ///     ��ʽ�����������ţ�
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsPureCurrency(this double value)
        {
            return String.Format("{0:0.0000}", value);
        }

        /// <summary>
        ///     ��ʽ�����������ţ�
        /// </summary>
        /// <param name="value">���</param>
        /// <returns>��ʽ����Ľ��</returns>
        public static string AsPureCurrency(this double? value)
        {
            return value.HasValue ? AsPureCurrency(value.Value) : String.Empty;
        }


        /// <summary>
        ///     ��ʽ��һ����Ŀ���
        /// </summary>
        /// <param name="value">һ����Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsTitle(this int value)
        {
            return String.Format("{0:0000}", value);
        }

        /// <summary>
        ///     ��ʽ��һ����Ŀ���
        /// </summary>
        /// <param name="value">һ����Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsTitle(this int? value)
        {
            return value.HasValue ? AsTitle(value.Value) : String.Empty;
        }

        /// <summary>
        ///     ��ʽ��������Ŀ���
        /// </summary>
        /// <param name="value">������Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsSubTitle(this int? value)
        {
            return value.HasValue ? AsSubTitle(value.Value) : String.Empty;
        }

        /// <summary>
        ///     ��ʽ��������Ŀ���
        /// </summary>
        /// <param name="value">������Ŀ���</param>
        /// <returns>��ʽ����ı��</returns>
        public static string AsSubTitle(this int value)
        {
            return String.Format("{0:00}", value);
        }


        /// <summary>
        ///     ��ʽ������
        /// </summary>
        /// <param name="value">����</param>
        /// <returns>��ʽ���������</returns>
        public static string AsDate(this DateTime value)
        {
            return String.Format("{0:yyyyMMdd}", value);
        }

        /// <summary>
        ///     ��ʽ������
        /// </summary>
        /// <param name="value">����</param>
        /// <returns>��ʽ���������</returns>
        public static string AsDate(this DateTime? value)
        {
            return value.HasValue ? AsDate(value.Value) : "������";
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
            if (Int32.TryParse(value, out val))
                return val;
            return null;
        }
    }
}

using System;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Util
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
                    return $"{value.Value.Year:D4}{value.Value.Month:D2}";
                case SubtotalLevel.Year:
                    return $"{value.Value.Year:D4}";
                default:
                    throw new ArgumentException("�����β��ǻ�������", nameof(level));
            }
        }

        /// <summary>
        ///     ��ʽ�����ڹ�����
        /// </summary>
        /// <param name="value">���ڹ�����</param>
        /// <returns>��ʽ��������ڹ�����</returns>
        public static string AsDateRange(this DateFilter value)
        {
            if (value.NullOnly)
                return "[null]";

            if (value.EndDate.HasValue)
            {
                if (value.StartDate.HasValue)
                    return value.Nullable
                        ? $"[{value.StartDate.AsDate()}={value.EndDate.AsDate()}]"
                        : $"[{value.StartDate.AsDate()}~{value.EndDate.AsDate()}]";

                return value.Nullable
                    ? $"[~{value.EndDate.AsDate()}]"
                    : $"[={value.EndDate.AsDate()}]";
            }

            if (value.StartDate.HasValue)
                return value.Nullable
                    ? $"[{value.StartDate.AsDate()}=]"
                    : $"[{value.StartDate.AsDate()}~]";

            return value.Nullable ? "[]" : "[~null]";
        }
    }
}

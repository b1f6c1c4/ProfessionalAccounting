using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     ����ƾ֤�����
    /// </summary>
    public enum VoucherType
    {
        /// <summary>
        ///     ��ͨ����ƾ֤
        /// </summary>
        Ordinal,

        /// <summary>
        ///     ��ĩ��ת����ƾ֤
        /// </summary>
        Carry,

        /// <summary>
        ///     ̯������ƾ֤
        /// </summary>
        Amortization,

        /// <summary>
        ///     �۾ɼ���ƾ֤
        /// </summary>
        Depreciation,

        /// <summary>
        ///     ��ֵ����ƾ֤
        /// </summary>
        Devalue,

        /// <summary>
        ///     ��Ƚ�ת����ƾ֤
        /// </summary>
        AnnualCarry,

        /// <summary>
        ///     ��׼ȷ����ƾ֤
        /// </summary>
        Uncertain
    }

    /// <summary>
    ///     ����ƾ֤
    /// </summary>
    public class Voucher
    {
        /// <summary>
        ///     ���˺��Ա�־
        /// </summary>
        public const string ReconciliationMark = "reconciliation";

        /// <summary>
        ///     ���
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        ///     ���ڣ���Ϊ<c>null</c>��ʾ������
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        ///     ϸĿ
        /// </summary>
        public VoucherDetail[] Details { get; set; }

        /// <summary>
        ///     ���
        /// </summary>
        public VoucherType? Type { get; set; }
    }

    /// <summary>
    ///     ϸĿ
    /// </summary>
    public class VoucherDetail
    {
        /// <summary>
        ///     ��������ƾ֤���
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///     ��ƿ�Ŀһ����Ŀ����
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     ��ƿ�Ŀ������Ŀ���룬��Ϊ<c>null</c>��ʾ�޶�����Ŀ
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     ����
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     ���
        /// </summary>
        public double? Fund { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     �ʲ��۾ɼ������Ŀ
    /// </summary>
    public abstract class AssetItem
    {
        /// <summary>
        ///     �淶���Ա�־
        /// </summary>
        public const string IgnoranceMark = "reconciliation";

        /// <summary>
        ///     ����ƾ֤���
        /// </summary>
        public string VoucherID { get; set; }

        /// <summary>
        ///     ��������
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     �����ֵ
        ///     <para>���洢�����ݿ���</para>
        /// </summary>
        public double BookValue { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     ȡ���ʲ�
    /// </summary>
    public class AcquisationItem : AssetItem
    {
        /// <summary>
        ///     ԭֵ
        /// </summary>
        public double OrigValue { get; set; }
    }

    /// <summary>
    ///     �����۾ɷ���
    /// </summary>
    public enum DepreciationMethod
    {
        /// <summary>
        ///     �������۾�
        /// </summary>
        None,

        /// <summary>
        ///     ����ƽ����
        /// </summary>
        StraightLine,

        /// <summary>
        ///     �����ܺͷ�
        /// </summary>
        SumOfTheYear,

        /// <summary>
        ///     ˫�����ݼ���
        /// </summary>
        DoubleDeclineMethod
    }

    /// <summary>
    ///     �����ʲ��۾�
    /// </summary>
    public class DepreciateItem : AssetItem
    {
        /// <summary>
        ///     �۾ɶ�
        /// </summary>
        public double Amount { get; set; }
    }

    /// <summary>
    ///     �����ʲ���ֵ׼��
    /// </summary>
    public class DevalueItem : AssetItem
    {
        /// <summary>
        ///     ���ʼ�ֵ
        /// </summary>
        public double FairValue { get; set; }
    }

    /// <summary>
    ///     �����ʲ�
    /// </summary>
    public class DispositionItem : AssetItem
    {
        /// <summary>
        ///     ԭ���澻ֵ
        /// </summary>
        public double NetValue { get; set; }
    }

    /// <summary>
    ///     �ʲ�
    /// </summary>
    public class Asset
    {
        /// <summary>
        ///     �淶���Ա�־
        /// </summary>
        public const string IgnoranceMark = "reconciliation";

        /// <summary>
        ///     ���
        /// </summary>
        public Guid? ID { get; set; }

        /// <summary>
        ///     ����
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     ��������
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     �ʲ�ԭֵ
        /// </summary>
        public double? Value { get; set; }

        /// <summary>
        ///     Ԥ�ƾ���ֵ
        /// </summary>
        public double? Salvge { get; set; }

        /// <summary>
        ///     ʹ������
        /// </summary>
        public int? Life { get; set; }

        /// <summary>
        ///     ԭֵ���ڿ�Ŀ���
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     �۾����ڿ�Ŀ���
        /// </summary>
        public int? DepreciationTitle { get; set; }

        /// <summary>
        ///     ��ֵ׼�����ڿ�Ŀ���
        /// </summary>
        public int? DevaluationTitle { get; set; }

        /// <summary>
        ///     �������ڿ�Ŀ���
        /// </summary>
        public int? ExpenseTitle { get; set; }

        /// <summary>
        ///     �������ڶ�����Ŀ���
        /// </summary>
        public int? ExpenseSubTitle { get; set; }

        /// <summary>
        ///     �۾ɷ���
        /// </summary>
        public DepreciationMethod? Method { get; set; }

        /// <summary>
        ///     �ʲ��۾ɼ����
        /// </summary>
        public AssetItem[] Schedule { get; set; }

        /// <summary>
        ///     ��ע
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     ������Ŀ
    /// </summary>
    public class Balance
    {
        /// <summary>
        ///     ����
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     һ����Ŀ���
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     ������Ŀ���
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     ����
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     ���
        /// </summary>
        public double Fund { get; set; }
    }

    public class AssetItemComparer : IComparer<AssetItem>
    {
        public int Compare(AssetItem x, AssetItem y)
        {
            var res = BalanceComparer.CompareDate(x.Date, y.Date);
            if (res != 0)
                return res;

            Func<AssetItem, int> getType = t =>
                                            {
                                                if (t is AcquisationItem)
                                                    return 0;
                                                if (t is DispositionItem)
                                                    return 1;
                                                if (t is DepreciateItem)
                                                    return 2;
                                                if (t is DevalueItem)
                                                    return 3;
                                                throw new InvalidOperationException();
                                            };
            return getType(x).CompareTo(getType(y));
        }
    }

    public class BalanceEqualityComparer : EqualityComparer<Balance>
    {
        public override bool Equals(Balance x, Balance y)
        {
            return x.Title == y.Title && x.SubTitle == y.SubTitle && x.Content == y.Content;
        }

        public override int GetHashCode(Balance obj)
        {
            var t = obj.Title ?? Int32.MinValue;
            var s = obj.SubTitle ?? Int32.MaxValue;
            var c = obj.Content == null ? Int32.MinValue : obj.Content.GetHashCode();
            return t ^ (s << 3) ^ c;
        }
    }

    public class BalanceComparer : Comparer<Balance>
    {
        /// <summary>
        ///     �Ƚ������ڣ�����Ϊ�����ڣ�������
        /// </summary>
        /// <param name="b1Date">��һ������</param>
        /// <param name="b2Date">�ڶ�������</param>
        /// <returns>���Ϊ0����һ����Ϊ-1���ڶ�����Ϊ1�������ڰ����ʱ����ǰ���ǣ�</returns>
        public static int CompareDate(DateTime? b1Date, DateTime? b2Date)
        {
            if (b1Date.HasValue &&
                b2Date.HasValue)
                return b1Date.Value.CompareTo(b2Date.Value);
            if (b1Date.HasValue)
                return 1;
            if (b2Date.HasValue)
                return -1;
            return 0;
        }

        public override int Compare(Balance x, Balance y)
        {
            if (x != null &&
                y != null)
            {
                switch (CompareDate(x.Date, y.Date))
                {
                    case 1:
                        return 1;
                    case -1:
                        return -1;
                }

                if (x.Title.HasValue &&
                    y.Title.HasValue)
                {
                    if (x.Title < y.Title)
                        return -1;
                    if (x.Title > y.Title)
                        return 1;
                }
                else if (x.Title.HasValue)
                    return 1;
                else if (y.Title.HasValue)
                    return -1;

                if (x.SubTitle.HasValue &&
                    y.SubTitle.HasValue)
                {
                    if (x.SubTitle < y.SubTitle)
                        return -1;
                    if (x.SubTitle > y.SubTitle)
                        return 1;
                }
                else if (x.SubTitle.HasValue)
                    return 1;
                else if (y.SubTitle.HasValue)
                    return -1;

                if (x.Content != null &&
                    y.Content != null)
                    return String.Compare(x.Content, y.Content, StringComparison.Ordinal);
                if (x.Content != null)
                    return 1;
                if (y.Content != null)
                    return -1;
            }
            if (x != null)
                return -1;
            if (y != null)
                return 1;
            return 0;
        }
    }
}

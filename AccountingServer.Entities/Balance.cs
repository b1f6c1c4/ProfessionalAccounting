using System;
using System.Collections.Generic;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     余额表条目
    /// </summary>
    public class Balance
    {
        /// <summary>
        ///     日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     一级科目编号
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     二级科目编号
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        ///     币种
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        ///     余额
        /// </summary>
        public double Fund { get; set; }

        public Balance() { }

        /// <summary>
        ///     深拷贝
        /// </summary>
        /// <param name="orig">源</param>
        public Balance(Balance orig)
        {
            Date = orig.Date;
            Title = orig.Title;
            SubTitle = orig.SubTitle;
            Content = orig.Content;
            Remark = orig.Remark;
            Currency = orig.Currency;
            Fund = orig.Fund;
        }
    }

    public class BalanceComparer : IEqualityComparer<Balance>
    {
        public bool Equals(Balance x, Balance y)
        {
            if (x.Date != y.Date)
                return false;
            if (x.Title != y.Title)
                return false;
            if (x.SubTitle != y.SubTitle)
                return false;
            if (x.Content != y.Content)
                return false;
            if (x.Remark != y.Remark)
                return false;
            if (x.Currency != y.Currency)
                return false;
            if (Math.Abs(x.Fund - y.Fund) > 1E-8)
                return false;

            return true;
        }

        public int GetHashCode(Balance obj)
        {
            var aggr = 0;
            if (obj.Title.HasValue)
                aggr += obj.Title.Value * 100;
            if (obj.SubTitle.HasValue)
                aggr += obj.SubTitle.Value;
            if (obj.Content != null)
                aggr ^= obj.Content.GetHashCode();
            if (obj.Remark != null)
                aggr ^= obj.Remark.GetHashCode() << 1;
            if (obj.Currency != null)
                aggr ^= obj.Currency.GetHashCode() << 2;
            return aggr;
        }
    }
}

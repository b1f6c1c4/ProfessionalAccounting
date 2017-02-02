using System;

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
}

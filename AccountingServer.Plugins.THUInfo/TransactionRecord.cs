using System;
using System.Globalization;
using AccountingServer.BLL;

namespace AccountingServer.Plugins.THUInfo
{
    /// <summary>
    ///     交易记录
    /// </summary>
    internal class TransactionRecord
    {
        /// <summary>
        ///     序号
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///     交易地点
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        ///     交易类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     终端编号
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///     交易时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        ///     交易日期
        /// </summary>
        public DateTime Date => Time.Date;

        /// <summary>
        ///     交易金额
        /// </summary>
        public double Fund { get; set; }

        /// <inheritdoc />
        public override string ToString() => string.Format(
            "@ {4:s}: #{0}{1}{2}{3} {5}",
            Index.ToString(CultureInfo.InvariantCulture).CPadRight(4),
            Location.CPadRight(17),
            Type.CPadRight(23),
            Endpoint.CPadLeft(9),
            Time,
            Fund.AsCurrency().CPadLeft(11));
    }
}

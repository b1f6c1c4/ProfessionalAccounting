using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     所有者权益币种转换表达式解释器
    /// </summary>
    internal class BaseCurrencyShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public BaseCurrencyShell(Accountant helper) => m_Accountant = helper;

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            expr = expr.Rest();
            switch (expr?.Initial())
            {
                case "lst":
                    return ListHistory(expr.Rest());
                case "ap":
                    return DoConversion(expr.Rest());
                case "rst":
                    return ResetConversion(expr.Rest());
                default:
                    throw new InvalidOperationException("表达式无效");
            }
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initial() == "bc";

        /// <summary>
        ///     列出记账本位币变更历史
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private static IQueryResult ListHistory(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var sb = new StringBuilder();

            foreach (var info in BaseCurrency.History)
                if (info.Date.Within(rng))
                    sb.AppendLine($"{info.Date.AsDate().PadLeft(8)} @{info.Currency}");

            return new PlainText(sb.ToString());
        }

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoConversion(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var cnt = 0L;
            foreach (var info in BaseCurrency.History)
            {
                if (!info.Date.HasValue)
                    continue;

                if (!info.Date.Within(rng))
                    continue;

                cnt += ConvertEquity(info.Date.Value, info.Currency);
            }

            return new NumberAffected(cnt);
        }

        /// <summary>
        ///     取消摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ResetConversion(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} %equity conversion%");
            return new NumberAffected(cnt);
        }


        /// <summary>
        ///     所有者权益币种转换
        /// </summary>
        /// <param name="dt">日期</param>
        /// <param name="to">目标币种</param>
        /// <returns>记账凭证数</returns>
        private long ConvertEquity(DateTime dt, string to)
        {
            var rst = m_Accountant.RunGroupedQuery($"T4101+T4103-@{to} [~{dt.AsDate()}]`Cts");

            var cnt = 0L;

            foreach (var grpC in rst.Items.Cast<ISubtotalCurrency>())
            foreach (var grpT in grpC.Items.Cast<ISubtotalTitle>())
            foreach (var grpS in grpT.Items.Cast<ISubtotalSubTitle>())
            {
                var oldb = grpS.Fund;
                var newb = m_Accountant.From(dt, grpC.Currency)
                    * m_Accountant.To(dt, to) * oldb;
                m_Accountant.Upsert(
                    new Voucher
                        {
                            Date = dt,
                            Type = VoucherType.Ordinary,
                            Remark = "equity conversion",
                            Details =
                                new List<VoucherDetail>
                                    {
                                        new VoucherDetail
                                            {
                                                Title = grpT.Title,
                                                SubTitle = grpS.SubTitle,
                                                Currency = grpC.Currency,
                                                Fund = -oldb,
                                            },
                                        new VoucherDetail
                                            {
                                                Title = grpT.Title,
                                                SubTitle = grpS.SubTitle,
                                                Currency = to,
                                                Fund = newb,
                                            },
                                        new VoucherDetail { Title = 3999, Currency = grpC.Currency, Fund = oldb },
                                        new VoucherDetail { Title = 3999, Currency = to, Fund = -newb },
                                    },
                        });
                cnt++;
            }

            return cnt;
        }
    }
}

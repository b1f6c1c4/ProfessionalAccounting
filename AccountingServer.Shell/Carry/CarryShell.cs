using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     结转表达式解释器
    /// </summary>
    internal class CarryShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            expr = expr.Rest();
            switch (expr?.Initital())
            {
                case "ap":
                    return DoCarry(expr.Rest());
                case "rst":
                    return ResetCarry(expr.Rest());
                default:
                    throw new InvalidOperationException("表达式无效");
            }
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "ca";

        private static readonly ConfigManager<CarrySettings> CarrySettings =
            new ConfigManager<CarrySettings>("Carry.xml");

        /// <summary>
        ///     汇率查询
        /// </summary>
        private static readonly IExchange Exchange = ExchangeFactory.Create();

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = BLL.Parsing.Facade.Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            BLL.Parsing.Facade.Parsing.Eof(expr);

            if (rng.NullOnly)
            {
                Carry(null);
                return new Succeed();
            }

            if (!rng.StartDate.HasValue ||
                !rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无界", nameof(expr));

            var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1);

            while (dt <= rng.EndDate.Value)
            {
                Carry(dt);
                dt = dt.AddMonths(1);
            }

            if (rng.Nullable)
                Carry(null);

            return new Succeed();
        }

        /// <summary>
        ///     取消摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ResetCarry(string expr)
        {
            var rng = BLL.Parsing.Facade.Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            BLL.Parsing.Facade.Parsing.Eof(expr);

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} Carry");
            return new NumberAffected(cnt);
        }


        /// <summary>
        ///     月末结转
        /// </summary>
        /// <param name="dt">月，若为<c>null</c>则表示对无日期进行结转</param>
        private void Carry(DateTime? dt)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, dt.Value.Month, 1);
                ed = AccountantHelper.LastDayOfMonth(dt.Value.Year, dt.Value.Month);
                rng = new DateFilter(sd, ed);
            }
            else
            {
                ed = null;
                rng = DateFilter.TheNullOnly;
            }

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.ExpensesQuery}) - @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                true);

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.Revenue1Query}) - @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                false);

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.Revenue2Query}) - @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                true);

            if (ed.HasValue)
            {
                var total =
                    m_Accountant.RunGroupedQuery($"T3999 [~{ed.AsDate()}]`C")
                        .Sum(bal => Exchange.From(ed.Value, bal.Currency) * bal.Fund);
                if (!total.IsZero())
                    m_Accountant.Upsert(new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.Carry,
                            Remark = "currency carry",
                            Details = new List<VoucherDetail>
                                {
                                    new VoucherDetail
                                        {
                                            Title = 3999,
                                            Fund = -total
                                        },
                                    new VoucherDetail
                                        {
                                            Title = 6603,
                                            SubTitle = 03,
                                            Fund = total
                                        }
                                }
                        });
            }

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.ExpensesQuery}) * @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                true);

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.Revenue1Query}) * @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                false);

            PartialCarry(
                m_Accountant.RunGroupedQuery($"({CarrySettings.Config.Revenue2Query}) * @@ {rng.AsDateRange()}`Ctsc"),
                ed,
                true);
        }

        /// <summary>
        ///     部分月末结转
        /// </summary>
        /// <param name="res">检索结果</param>
        /// <param name="ed">日期</param>
        /// <param name="target">是否专用</param>
        private void PartialCarry(IEnumerable<Balance> res, DateTime? ed, bool target)
        {
            foreach (var grpCurrency in res.GroupByCurrency())
            {
                var b = 0D;
                var voucher =
                    new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.Carry,
                            Details = new List<VoucherDetail>()
                        };
                foreach (var balance in grpCurrency)
                {
                    b += balance.Fund;
                    voucher.Details.Add(
                        new VoucherDetail
                            {
                                Currency = grpCurrency.Key,
                                Title = balance.Title,
                                SubTitle = balance.SubTitle,
                                Content = balance.Content,
                                Fund = -balance.Fund
                            });
                }

                if (b.IsZero())
                    continue;

                if (grpCurrency.Key == VoucherDetail.BaseCurrency)
                {
                    voucher.Details.Add(
                        new VoucherDetail
                            {
                                Title = 4103,
                                SubTitle = target ? 01 : (int?)null,
                                Fund = b
                            });
                    m_Accountant.Upsert(voucher);
                    continue;
                }

                if (!ed.HasValue)
                    throw new InvalidOperationException("无穷长时间以前不存在汇率");

                var cob = Exchange.From(ed.Value, grpCurrency.Key) * b;

                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Currency = grpCurrency.Key,
                            Title = 3999,
                            SubTitle = 01,
                            Fund = b
                        });
                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Title = 3999,
                            Remark = voucher.ID,
                            Fund = -cob
                        });
                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Title = 4103,
                            SubTitle = target ? 01 : (int?)null,
                            Fund = cob
                        });
                m_Accountant.Upsert(voucher);
            }
        }
    }

    [Serializable]
    [XmlRoot("CarrySettings")]
    public class CarrySettings
    {
        [XmlElement("Expenses")] public string ExpensesQuery;

        [XmlElement("Revenue1")] public string Revenue1Query;

        [XmlElement("Revenue2")] public string Revenue2Query;
    }
}

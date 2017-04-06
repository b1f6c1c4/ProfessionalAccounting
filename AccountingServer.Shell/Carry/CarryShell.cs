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
using static AccountingServer.BLL.Parsing.Facade;

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
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

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
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

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

            var targets = GetTargets(ed).ToList();
            foreach (var target in targets)
                PartialCarry(target, rng, false);

            if (ed.HasValue)
            {
                var total =
                    m_Accountant.RunGroupedQuery($"T3999 [~{ed.AsDate()}]`C")
                        .Select(
                            bal => new Balance
                                {
                                    Currency = bal.Currency,
                                    Fund = bal.Fund +
                                        // ReSharper disable once PossibleInvalidOperationException
                                        targets.SelectMany(t => t.Voucher.Details)
                                            .Where(d => d.Currency == bal.Currency && d.Title == 3999)
                                            .Sum(d => d.Fund.Value)
                                })
                        .Sum(bal => ExchangeFactory.Instance.From(ed.Value, bal.Currency) * bal.Fund);
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

            foreach (var target in targets)
                PartialCarry(target, rng, true);

            foreach (var target in targets)
            {
                target.Voucher.Details.Add(
                    new VoucherDetail
                        {
                            Title = 4103,
                            SubTitle = target.IsSpecial ? 01 : (int?)null,
                            Fund = target.Value
                        });
                m_Accountant.Upsert(target.Voucher);
            }
        }

        /// <summary>
        ///     按目标月末结转
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="rng">范围</param>
        /// <param name="baseCurrency">是否为基准</param>
        /// <returns>结转记账凭证</returns>
        private void PartialCarry(CarryTarget target, DateFilter rng, bool baseCurrency)
        {
            var total = 0D;
            var ed = rng.NullOnly ? null : rng.EndDate;
            var voucher = target.Voucher;
            var res =
                m_Accountant.RunGroupedQuery(
                    $"({target.Query}) {(baseCurrency ? '*' : '-')}@@ {rng.AsDateRange()}`Ctsc");
            foreach (var grpCurrency in res.GroupByCurrency())
            {
                var b = 0D;
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
                    total += b;
                    continue;
                }

                var cob = ExchangeFactory.Instance.From(ed ?? throw new InvalidOperationException("无穷长时间以前不存在汇率"), grpCurrency.Key) * b;

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
                            Currency = VoucherDetail.BaseCurrency,
                            Title = 3999,
                            Remark = voucher.ID,
                            Fund = -cob
                        });
                total += cob;
            }

            target.Value += total;
        }

        private static IEnumerable<CarryTarget> GetTargets(DateTime? ed)
        {
            yield return new CarryTarget
                {
                    Query = CarrySettings.Config.ExpensesQuery,
                    Value = 0D,
                    Voucher = new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.Carry,
                            Details = new List<VoucherDetail>()
                        },
                    IsSpecial = true
                };
            yield return new CarryTarget
                {
                    Query = CarrySettings.Config.Revenue1Query,
                    Value = 0D,
                    Voucher = new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.Carry,
                            Details = new List<VoucherDetail>()
                        },
                    IsSpecial = false
                };
            yield return new CarryTarget
                {
                    Query = CarrySettings.Config.Revenue2Query,
                    Value = 0D,
                    Voucher = new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.Carry,
                            Details = new List<VoucherDetail>()
                        },
                    IsSpecial = true
                };
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

    internal class CarryTarget
    {
        public Voucher Voucher { get; set; }

        public string Query { get; set; }

        public double Value { get; set; }

        public bool IsSpecial { get; set; }
    }
}

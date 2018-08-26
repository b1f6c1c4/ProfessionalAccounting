using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
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

        public CarryShell(Accountant helper) => m_Accountant = helper;

        public static IConfigManager<CarrySettings> CarrySettings { private get; set; } =
            new ConfigManager<CarrySettings>("Carry.xml");

        /// <inheritdoc />
        public IQueryResult Execute(string expr, IEntitiesSerializer serializer)
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

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            Parsing.Eof(expr);

            var cnt = 0L;

            if (rng.NullOnly)
            {
                cnt += Carry(null);
                return new NumberAffected(cnt);
            }

            if (!rng.StartDate.HasValue ||
                !rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无界", nameof(expr));

            var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (dt <= rng.EndDate.Value)
            {
                cnt += Carry(dt);
                dt = dt.AddMonths(1);
            }

            if (rng.Nullable)
                cnt += Carry(null);

            return new NumberAffected(cnt);
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
        /// <returns>记账凭证数</returns>
        private long Carry(DateTime? dt)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, dt.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
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

            var cnt = 0L;
            if (ed.HasValue)
            {
                var baseCur = BaseCurrency.At(ed);
                var total =
                    m_Accountant.RunGroupedQuery($"T3999 [~{ed.AsDate()}]`C")
                        .Items.Cast<ISubtotalCurrency>().Select(
                            bal => new Balance
                                {
                                    Currency = bal.Currency,
                                    Fund = bal.Fund +
                                        // ReSharper disable once PossibleInvalidOperationException
                                        targets.SelectMany(t => t.Voucher.Details)
                                            .Where(d => d.Currency == bal.Currency && d.Title == 3999)
                                            .Sum(d => d.Fund.Value)
                                })
                        .Sum(
                            bal => ExchangeFactory.Instance.From(ed.Value, bal.Currency)
                                / ExchangeFactory.Instance.To(ed.Value, baseCur)
                                * bal.Fund);
                if (!total.IsZero())
                {
                    m_Accountant.Upsert(
                        new Voucher
                            {
                                Date = ed,
                                Type = VoucherType.Carry,
                                Remark = "currency carry",
                                Details = new List<VoucherDetail>
                                    {
                                        new VoucherDetail
                                            {
                                                Currency = baseCur,
                                                Title = 3999,
                                                Fund = -total
                                            },
                                        new VoucherDetail
                                            {
                                                Currency = baseCur,
                                                Title = 6603,
                                                SubTitle = 03,
                                                Fund = total
                                            }
                                    }
                            });
                    cnt++;
                }
            }

            foreach (var target in targets)
                PartialCarry(target, rng, true);

            foreach (var target in targets)
            {
                if (!target.Value.IsZero())
                    target.Voucher.Details.Add(
                        new VoucherDetail
                            {
                                Currency = BaseCurrency.At(target.Voucher.Date),
                                Title = 4103,
                                SubTitle = target.IsSpecial ? 01 : (int?)null,
                                Fund = target.Value
                            });

                if (target.Voucher.Details.Any())
                {
                    m_Accountant.Upsert(target.Voucher);
                    cnt++;
                }
            }

            return cnt;
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
            var baseCur = BaseCurrency.At(ed);
            var res =
                m_Accountant.RunGroupedQuery(
                    $"({target.Query}) {(baseCurrency ? '*' : '-')}@{baseCur} {rng.AsDateRange()}`Ctsc");
            foreach (var grpC in res.Items.Cast<ISubtotalCurrency>())
            {
                var b = grpC.Fund;
                foreach (var grpt in grpC.Items.Cast<ISubtotalTitle>())
                foreach (var grps in grpt.Items.Cast<ISubtotalSubTitle>())
                foreach (var grpc in grps.Items.Cast<ISubtotalContent>())
                    voucher.Details.Add(
                        new VoucherDetail
                            {
                                Currency = grpC.Currency,
                                Title = grpt.Title,
                                SubTitle = grps.SubTitle,
                                Content = grpc.Content,
                                Fund = -grpc.Fund
                            });

                if (b.IsZero())
                    continue;

                if (grpC.Currency == baseCur)
                {
                    total += b;
                    continue;
                }

                if (!ed.HasValue)
                    throw new InvalidOperationException("无穷长时间以前不存在汇率");

                var cob = ExchangeFactory.Instance.From(ed.Value, grpC.Currency) *
                    ExchangeFactory.Instance.To(ed.Value, baseCur) * b;

                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Currency = grpC.Currency,
                            Title = 3999,
                            Fund = b
                        });
                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Currency = baseCur,
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
                    IsSpecial = false
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

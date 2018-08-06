using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     年度结转表达式解释器
    /// </summary>
    internal class CarryYearShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryYearShell(Accountant helper) => m_Accountant = helper;

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
        public bool IsExecutable(string expr) => expr.Initital() == "caa";

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
                CarryYear(null);
                return new DirtySucceed();
            }

            var ed = rng.EndDate ?? throw new ArgumentException("时间范围无后界", nameof(expr));

            var dt = new DateTime((rng.StartDate ?? ed).Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            while (dt <= ed)
            {
                CarryYear(dt, !rng.StartDate.HasValue);
                dt = dt.AddYears(1);
            }

            return new DirtySucceed();
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

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} AnnualCarry");
            return new NumberAffected(cnt);
        }


        /// <summary>
        ///     年末结转
        /// </summary>
        /// <param name="dt">年，若为<c>null</c>则表示对无日期进行结转</param>
        /// <param name="includeNull">是否计入无日期</param>
        private void CarryYear(DateTime? dt, bool includeNull = false)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                ed = sd.AddYears(1).AddDays(-1);
                rng = new DateFilter(includeNull ? (DateTime?)null : sd, ed);
            }
            else
            {
                ed = null;
                rng = DateFilter.TheNullOnly;
            }

            var b00s = m_Accountant.RunGroupedQuery($"T410300 {rng.AsDateRange()}`C");
            var b01s = m_Accountant.RunGroupedQuery($"T410301 {rng.AsDateRange()}`C");

            foreach (var grpC in b00s.Items.Cast<ISubtotalCurrency>())
            {
                var b00 = grpC.Fund;
                m_Accountant.Upsert(
                    new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.AnnualCarry,
                            Details =
                                new List<VoucherDetail>
                                    {
                                        new VoucherDetail { Title = 4101, Currency = grpC.Currency, Fund = b00 },
                                        new VoucherDetail { Title = 4103, Currency = grpC.Currency, Fund = -b00 }
                                    }
                        });
            }

            foreach (var grpC in b01s.Items.Cast<ISubtotalCurrency>())
            {
                var b01 = grpC.Fund;
                m_Accountant.Upsert(
                    new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.AnnualCarry,
                            Details =
                                new List<VoucherDetail>
                                    {
                                        new VoucherDetail
                                            {
                                                Title = 4101,
                                                SubTitle = 01,
                                                Currency = grpC.Currency,
                                                Fund = b01
                                            },
                                        new VoucherDetail
                                            {
                                                Title = 4103,
                                                SubTitle = 01,
                                                Currency = grpC.Currency,
                                                Fund = -b01
                                            }
                                    }
                        });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
                return new Succeed();
            }

            var ed = rng.EndDate ?? throw new ArgumentException("时间范围无后界", nameof(expr));

            var dt = new DateTime((rng.StartDate ?? ed).Year, 1, 1).CastUtc();

            while (dt <= ed)
            {
                CarryYear(dt, !rng.StartDate.HasValue);
                dt = dt.AddYears(1);
            }

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

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} AnnualCarry");
            return new NumberAffected(cnt);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "caa";


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
                var sd = new DateTime(dt.Value.Year, 1, 1).CastUtc();
                ed = sd.AddYears(1).AddDays(-1);
                rng = new DateFilter(includeNull ? (DateTime?)null : sd, ed);
            }
            else
            {
                ed = null;
                rng = DateFilter.TheNullOnly;
            }

            var b00 = m_Accountant.RunGroupedQuery($"T410300 {rng.AsDateRange()}`v").Single().Fund;
            var b01 = m_Accountant.RunGroupedQuery($"T410301 {rng.AsDateRange()}`v").Single().Fund;

            if (!b00.IsZero())
                m_Accountant.Upsert(
                    new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.AnnualCarry,
                            Details =
                                new List<VoucherDetail>
                                    {
                                        new VoucherDetail { Title = 4101, Fund = b00 },
                                        new VoucherDetail { Title = 4103, Fund = -b00 }
                                    }
                        });

            if (!b01.IsZero())
                m_Accountant.Upsert(
                    new Voucher
                        {
                            Date = ed,
                            Type = VoucherType.AnnualCarry,
                            Details =
                                new List<VoucherDetail>
                                    {
                                        new VoucherDetail { Title = 4101, SubTitle = 01, Fund = b01 },
                                        new VoucherDetail { Title = 4103, SubTitle = 01, Fund = -b01 }
                                    }
                        });
        }
    }
}

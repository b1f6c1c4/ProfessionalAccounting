using System;
using AccountingServer.BLL;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
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
            if (expr?.Initital() == "ap")
                return DoCarry(expr.Rest());
            if (expr?.Initital() == "rst")
                return ResetCarry(expr.Rest());

            throw new InvalidOperationException("表达式无效");
        }

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));

            if (rng.NullOnly)
            {
                m_Accountant.Carry(null);
                return new Succeed();
            }

            if (!rng.StartDate.HasValue ||
                !rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无界", nameof(expr));

            var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1);

            while (dt <= rng.EndDate.Value)
            {
                m_Accountant.Carry(dt);
                dt = dt.AddMonths(1);
            }

            if (rng.Nullable)
                m_Accountant.Carry(null);

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
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));

            if (rng.NullOnly)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.Carry },
                                                          filter: null,
                                                          rng: rng));
                return new NumberAffected(cnt);
            }

            if (!rng.StartDate.HasValue ||
                !rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无界", nameof(expr));

            var count = 0L;
            var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1);

            while (dt <= rng.EndDate.Value)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.Carry },
                                                          filter: null,
                                                          rng: new DateFilter(dt, dt.AddMonths(1).AddDays(-1))));
                count += cnt;
                dt = dt.AddMonths(1);
            }

            if (rng.Nullable)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.Carry },
                                                          filter: null,
                                                          rng: DateFilter.TheNullOnly));
                count += cnt;
            }

            return new NumberAffected(count);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "ca";
    }

    /// <summary>
    ///     年度结转表达式解释器
    /// </summary>
    internal class CarryYearShell : IShellComponent
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryYearShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            expr = expr.Rest();
            if (expr?.Initital() == "ap")
                return DoCarry(expr);
            if (expr?.Initital() == "rst")
                return ResetCarry(expr);

            throw new InvalidOperationException("表达式无效");
        }

        /// <summary>
        ///     执行摊销
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));

            if (rng.NullOnly)
            {
                m_Accountant.CarryYear(null);
                return new Succeed();
            }

            if (!rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无后界", nameof(expr));

            var dt = new DateTime((rng.StartDate ?? rng.EndDate.Value).Year, 1, 1);

            while (dt <= rng.EndDate.Value)
            {
                m_Accountant.CarryYear(dt, !rng.StartDate.HasValue);
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
            if (!string.IsNullOrWhiteSpace(expr))
                throw new ArgumentException("语法错误", nameof(expr));

            if (rng.NullOnly)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.AnnualCarry },
                                                          filter: null,
                                                          rng: rng));
                return new NumberAffected(cnt);
            }

            if (!rng.EndDate.HasValue)
                throw new ArgumentException("时间范围无后界", nameof(expr));

            var count = 0L;
            var dt = new DateTime((rng.StartDate ?? rng.EndDate.Value).Year, 1, 1);

            while (dt <= rng.EndDate.Value)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.AnnualCarry },
                                                          filter: null,
                                                          rng: new DateFilter(dt, dt.AddYears(1).AddDays(-1))));
                count += cnt;
                dt = dt.AddYears(1);
            }

            if (rng.Nullable)
            {
                var cnt = m_Accountant.DeleteVouchers(
                                                      new VoucherQueryAtomBase(
                                                          new Voucher { Type = VoucherType.AnnualCarry },
                                                          filter: null,
                                                          rng: DateFilter.TheNullOnly));
                count += cnt;
            }

            return new NumberAffected(count);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "caa";
    }
}

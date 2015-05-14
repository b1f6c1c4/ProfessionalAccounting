using System;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     执行结转表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        private IQueryResult ExecuteCarry(ConsoleParser.CarryContext expr)
        {
            if (expr.carryMonth() != null)
            {
                var rng = expr.carryMonth().range() != null
                              ? expr.carryMonth().range().Range
                              : DateFilter.Unconstrained;

                if (rng.NullOnly)
                {
                    m_Accountant.Carry(null);
                    return new Suceed();
                }

                if (!rng.StartDate.HasValue ||
                    !rng.EndDate.HasValue)
                    throw new InvalidOperationException();

                var dt = new DateTime(rng.StartDate.Value.Year, rng.StartDate.Value.Month, 1);

                while (dt < rng.EndDate.Value)
                {
                    m_Accountant.Carry(dt);
                    dt = dt.AddMonths(1);
                }

                if (rng.Nullable)
                    m_Accountant.Carry(null);

                return new Suceed();
            }
            if (expr.carryMonthResetHard() != null)
            {
                var rng = expr.carryMonthResetHard().range() != null
                              ? expr.carryMonthResetHard().range().Range
                              : DateFilter.Unconstrained;

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
                    throw new InvalidOperationException();

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
            if (expr.carryYear() != null)
            {
                var rng = expr.carryYear().range() != null
                              ? expr.carryYear().range().Range
                              : DateFilter.Unconstrained;

                if (rng.NullOnly)
                {
                    m_Accountant.CarryYear(null);
                    return new Suceed();
                }

                if (!rng.EndDate.HasValue)
                    throw new InvalidOperationException();

                var dt = new DateTime((rng.StartDate ?? rng.EndDate.Value).Year, 1, 1);

                while (dt <= rng.EndDate.Value)
                {
                    m_Accountant.CarryYear(dt, !rng.StartDate.HasValue);
                    dt = dt.AddYears(1);
                }

                return new Suceed();
            }
            if (expr.carryYearResetHard() != null)
            {
                var rng = expr.carryYearResetHard().range() != null
                              ? expr.carryYearResetHard().range().Range
                              : DateFilter.Unconstrained;

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
                    throw new InvalidOperationException();

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
            throw new InvalidOperationException();
        }
    }
}

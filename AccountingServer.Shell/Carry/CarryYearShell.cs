using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     ��Ƚ�ת���ʽ������
    /// </summary>
    internal class CarryYearShell : IShellComponent
    {
        /// <summary>
        ///     �������ҵ������
        /// </summary>
        private readonly Accountant m_Accountant;

        public CarryYearShell(Accountant helper) { m_Accountant = helper; }

        /// <inheritdoc />
        public IQueryResult Execute(string expr)
        {
            expr = expr.Rest();
            if (expr?.Initital() == "ap")
                return DoCarry(expr.Rest());
            if (expr?.Initital() == "rst")
                return ResetCarry(expr.Rest());

            throw new InvalidOperationException("���ʽ��Ч");
        }

        /// <summary>
        ///     ִ��̯��
        /// </summary>
        /// <param name="expr">���ʽ</param>
        /// <returns>ִ�н��</returns>
        private IQueryResult DoCarry(string expr)
        {
            var rng = BLL.Parsing.Facade.Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            BLL.Parsing.Facade.Parsing.Eof(expr);

            if (rng.NullOnly)
            {
                CarryYear(null);
                return new Succeed();
            }

            if (!rng.EndDate.HasValue)
                throw new ArgumentException("ʱ�䷶Χ�޺��", nameof(expr));

            var dt = new DateTime((rng.StartDate ?? rng.EndDate.Value).Year, 1, 1);

            while (dt <= rng.EndDate.Value)
            {
                CarryYear(dt, !rng.StartDate.HasValue);
                dt = dt.AddYears(1);
            }

            return new Succeed();
        }

        /// <summary>
        ///     ȡ��̯��
        /// </summary>
        /// <param name="expr">���ʽ</param>
        /// <returns>ִ�н��</returns>
        private IQueryResult ResetCarry(string expr)
        {
            var rng = BLL.Parsing.Facade.Parsing.Range(ref expr) ?? DateFilter.Unconstrained;
            BLL.Parsing.Facade.Parsing.Eof(expr);

            var cnt = m_Accountant.DeleteVouchers($"{rng.AsDateRange()} AnnualCarry");
            return new NumberAffected(cnt);
        }

        /// <inheritdoc />
        public bool IsExecutable(string expr) => expr.Initital() == "caa";


        /// <summary>
        ///     ��ĩ��ת
        /// </summary>
        /// <param name="dt">�꣬��Ϊ<c>null</c>���ʾ�������ڽ��н�ת</param>
        /// <param name="includeNull">�Ƿ����������</param>
        private void CarryYear(DateTime? dt, bool includeNull = false)
        {
            DateTime? ed;
            DateFilter rng;
            if (dt.HasValue)
            {
                var sd = new DateTime(dt.Value.Year, 1, 1);
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
                            Currency = Voucher.BaseCurrency,
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
                            Currency = Voucher.BaseCurrency,
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

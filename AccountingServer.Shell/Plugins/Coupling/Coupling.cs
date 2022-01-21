/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Coupling;

internal class Coupling : PluginBase
{
    public static IConfigManager<CoupleTemplate> Templates { private get; set; } =
        new ConfigManager<CoupleTemplate>("Coupling.xml");

    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
        var sub = Parsing.Subtotal(ref expr, session.Client);
        Parsing.Eof(expr);
        var level = sub.PreprocessDetail();

        var sas = Templates.Config.SpecialAccounts
            .Select(sa => (Account: sa, Query: Parsing.PureDetailQuery(sa.Query, session.Client))).ToList();
        var couples = GetCouples(session, rng);
        var dic = new Dictionary<(string, string), double>();
        await foreach (var couple in couples)
        {
            var sac = sas.FirstOrDefault(sa => couple.Creditor.IsMatch(sa.Query)).Account;
            var sad = sas.FirstOrDefault(sa => couple.Debitor.IsMatch(sa.Query)).Account;
            if (sac == null || sad == null)
                continue;

            var ac = GetSpecializedAccount(couple.Creditor, sac);
            var ad = GetSpecializedAccount(couple.Debitor, sad);
            var fund = couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now);
            if (string.Compare(ac, ad, StringComparison.Ordinal) > 0)
            {
                fund = -fund;
                (ac, ad) = (ad, ac);
            }

            if (!dic.ContainsKey((ac, ad)))
                dic.Add((ac, ad), 0D);
            dic[(ac, ad)] += fund;
        }

        foreach (var ((ac, ad), fund) in dic)
            yield return $"{ac} -> {ad}: {fund.AsCurrency(BaseCurrency.Now)}\n";
    }

    private static string GetSpecializedAccount(VoucherDetail detail, SpecialAccount sa)
    {
        var a = $"U{detail.User.AsUser()}-{sa.Name}";
        if (sa.ByContent)
            a += $"-{detail.Content}";
        if (sa.ByRemark)
            a += $"-{detail.Remark}";
        return a;
    }

    private static IAsyncEnumerable<Couple> GetCouples(Session session, DateFilter rng)
        => session.Accountant.SelectVouchersAsync(Parsing.VoucherQuery($"U T3998 {rng.AsDateRange()}", session.Client))
            .SelectMany(static v => Decouple(v).ToAsyncEnumerable());

    private sealed class VoucherStub : IVoucherQueryAtom
    {
        public bool IsDangerous() => DetailFilter.IsDangerous();

        public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);

        public bool ForAll => false;
        public Voucher VoucherFilter => null;
        public DateFilter Range => DateFilter.Unconstrained;
        public IQueryCompounded<IDetailQueryAtom> DetailFilter { get; init; }
    }

    private sealed class IntersectStub<TAtom> : IQueryAry<TAtom> where TAtom : class
    {
        public bool IsDangerous() => Filter1.IsDangerous() && Filter2.IsDangerous();

        public T Accept<T>(IQueryVisitor<TAtom, T> visitor) => visitor.Visit(this);

        public OperatorType Operator => OperatorType.Intersect;
        public IQueryCompounded<TAtom> Filter1 { get; init; }
        public IQueryCompounded<TAtom> Filter2 { get; init; }
    }

    private struct Couple
    {
        public Voucher Voucher;
        public VoucherDetail Creditor;
        public VoucherDetail Debitor;
        public string Currency;
        public double Fund;

        public override string ToString()
        {
            var c =
                $"U{Creditor.User.AsUser()} T{Creditor.Title.AsTitle()}{Creditor.SubTitle.AsSubTitle()} {Creditor.Content.Quotation('\'')} {Creditor.Remark.Quotation('"')}";
            var d =
                $"U{Debitor.User.AsUser()} T{Debitor.Title.AsTitle()}{Debitor.SubTitle.AsSubTitle()} {Debitor.Content.Quotation('\'')} {Debitor.Remark.Quotation('"')}";
            return
                $"^{Voucher.ID}^ {Voucher.Date.AsDate()} {c.CPadRight(60)} -> {d.CPadRight(60)} @{Currency} {Fund.AsCurrency(Currency)}";
        }
    }

    private static IEnumerable<Couple> Decouple(Voucher voucher)
    {
        foreach (var grpC in voucher.Details.GroupBy(static d => d.Currency))
        {
            // U1 c* -> U1 d* + U1 T3998 >
            var totalCredits = 0D;
            var creditors = new Dictionary<string, List<VoucherDetail>>();
            // U2 d* <- U2 c* + U2 T3998 <
            var totalDebits = 0D;
            var debitors = new Dictionary<string, List<VoucherDetail>>();
            foreach (var grpU in voucher.Details.GroupBy(static d => d.User))
            {
                if (!grpU.Sum(static d => d.Fund!.Value).IsZero())
                    throw new ApplicationException(
                        $"Unbalanced Voucher ^{voucher.ID}^ U{grpU.Key.AsUser()} @{grpC.Key}, run chk 1 first");

                var t3998 = grpU.Where(static d => d.Title == 3998).Sum(static d => d.Fund!.Value);
                if (t3998.IsZero())
                    continue;

                if (t3998 > 0)
                {
                    totalCredits += -t3998;
                    creditors.Add(grpU.Key, grpU.Where(static d => !d.Fund!.Value.IsNonNegative()).ToList());
                }
                else
                {
                    totalDebits += -t3998;
                    debitors.Add(grpU.Key, grpU.Where(static d => !d.Fund!.Value.IsNonPositive()).ToList());
                }
            }

            if (!(totalCredits + totalDebits).IsZero())
                throw new ApplicationException(
                    $"Unbalanced Voucher ^{voucher.ID}^ @{grpC.Key}, run chk 1 first");

            if (totalCredits.IsZero())
                continue;

            foreach (var (_, credits) in creditors)
            foreach (var c in credits)
            foreach (var (_, debits) in debitors)
            foreach (var d in debits)
                yield return new()
                    {
                        Voucher = voucher,
                        Creditor = c,
                        Debitor = d,
                        Currency = grpC.Key,
                        Fund = -d.Fund!.Value * c.Fund!.Value / totalDebits,
                    };
        }
    }
}

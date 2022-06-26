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
        var len = expr.Length;
        var aux = Parsing.PureDetailQuery(ref expr, session.Client);
        if (len == expr.Length)
            aux = null;
        var rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
        Parsing.Eof(expr);

        foreach (var configCouple in Templates.Config.Couples)
        await foreach (var ret in AnalyzeCouple(session, configCouple, rng, aux))
            yield return ret;
    }

    private static async IAsyncEnumerable<string> AnalyzeCouple(Session session, ConfigCouple configCouple,
        DateFilter rng, IQueryCompounded<IDetailQueryAtom> aux)
    {
        var spendEntries = new SortedDictionary<string, (List<Couple>, List<Couple>, List<Couple>)>();
        var gatherEntries = new SortedDictionary<string, List<Couple>>();
        var errEntries = new List<Couple>();
        await foreach (var couple in GetCouples(session, configCouple.User, rng))
        {
            if (aux != null && !couple.Creditor.IsMatch(aux) && !couple.Debitor.IsMatch(aux))
                continue;

            var lc = configCouple.Liabilities.SingleOrDefault(l => l.User == couple.Creditor.User);
            var cc = lc?.IsCash(couple.Creditor) ?? false;
            var ld = configCouple.Liabilities.SingleOrDefault(l => l.User == couple.Debitor.User);
            var cd = ld?.IsCash(couple.Debitor) ?? false;
            if (cc && configCouple.IsCash(couple.Debitor)) // U1 cash -> U1&2 cash
            {
                var a = couple.Creditor.User.AsUser();
                if (!gatherEntries.ContainsKey(a))
                    gatherEntries.Add(a, new());
                gatherEntries[a].Add(couple);
            }
            else if (cc && configCouple.IsNonCash(couple.Debitor)) // U1 cash -> U1&2 non-cash
            {
                var a = lc.NameOf(couple.Creditor);
                if (!spendEntries.ContainsKey(a))
                    spendEntries.Add(a, (new(), new(), new()));
                spendEntries[a].Item1.Add(couple);
            }
            else if (configCouple.IsCash(couple.Creditor) && cd) // U1&2 cash -> U1 cash
            {
                var a = ld.NameOf(couple.Debitor);
                if (!spendEntries.ContainsKey(a))
                    spendEntries.Add(a, (new(), new(), new()));
                spendEntries[a].Item2.Add(couple);
            }
            else if (configCouple.IsNonCash(couple.Creditor) && cd) // U1&2 non-cash -> U1 cash
            {
                var a = ld.NameOf(couple.Debitor);
                if (!spendEntries.ContainsKey(a))
                    spendEntries.Add(a, (new(), new(), new()));
                spendEntries[a].Item3.Add(couple);
            }
            else if (configCouple.User == couple.Creditor.User ||
                     configCouple.User == couple.Debitor.User) // otherwise
                errEntries.Add(couple);
        }

        foreach (var (a, lst) in gatherEntries)
        {
            var fund = await lst.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now));
            yield return
                $"// {configCouple.User.AsUser()} *** cash by *** {a} {fund.AsCurrency(BaseCurrency.Now)}\n";
            foreach (var couple in lst)
                yield return $"{couple}\n";
        }

        foreach (var (a, (paidBy, paidInCashTo, paidTo)) in spendEntries)
        {
            var fund1 = await paidBy.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now));
            var fund2 = await paidInCashTo.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now));
            var fund3 = await paidTo.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now));
            if (paidInCashTo.Count != 0)
            {
                yield return
                    $"// {configCouple.User.AsUser()} *** cash to *** {a} {fund2.AsCurrency(BaseCurrency.Now)}\n";
                foreach (var couple in paidInCashTo)
                    yield return $"{couple}\n";
            }

            if (paidTo.Count != 0)
            {
                yield return
                    $"// {configCouple.User.AsUser()} +++ paid to +++ {a} {fund3.AsCurrency(BaseCurrency.Now)}\n";
                if (aux != null)
                    foreach (var couple in paidTo)
                        yield return $"{couple}\n";
            }

            yield return
                $"// {configCouple.User.AsUser()} --- paid by --- {a} {fund1.AsCurrency(BaseCurrency.Now)}\n";
            if (aux != null)
                foreach (var couple in paidBy)
                    yield return $"{couple}\n";
        }

        if (errEntries.Count != 0)
            yield return $"// ignored {errEntries.Count} items\n";
        if (aux != null)
            foreach (var couple in errEntries)
                yield return $"// ignored: {couple}\n";
    }

    private static IAsyncEnumerable<Couple> GetCouples(Session session, string user, DateFilter rng)
        => session.Accountant
            .SelectVouchersAsync(Parsing.VoucherQuery($"{user.AsUser()} T3998 {rng.AsDateRange()}", session.Client))
            .SelectMany(static v => Decouple(v).ToAsyncEnumerable());

    private struct Couple
    {
        public Voucher Voucher;
        public VoucherDetail Creditor;
        public VoucherDetail Debitor;
        public string Currency;
        public double Fund;

        public override string ToString()
        {
            var c = $"{Creditor.User.AsUser()} T{Creditor.Title.AsTitle()}{Creditor.SubTitle.AsSubTitle()}";
            if (Creditor.Content != null)
                c += $" {Creditor.Content.Quotation('\'')}";
            if (Creditor.Remark != null)
                c += $" {Creditor.Remark.Quotation('"')}";
            var d = $"{Debitor.User.AsUser()} T{Debitor.Title.AsTitle()}{Debitor.SubTitle.AsSubTitle()}";
            if (Debitor.Content != null)
                d += $" {Debitor.Content.Quotation('\'')}";
            if (Debitor.Remark != null)
                d += $" {Debitor.Remark.Quotation('"')}";
            return
                $"^{Voucher.ID}^ {Voucher.Date.AsDate()} {c.CPadRight(18)} -> {d.CPadRight(51)} @{Currency} {Fund.AsCurrency(Currency)}";
        }
    }

    private static IEnumerable<Couple> Decouple(Voucher voucher)
    {
        foreach (var grpC in voucher.Details.GroupBy(static d => d.Currency))
        {
            var ratio = new Dictionary<string, double>();
            // U1 c* -> U1 d* + U1 T3998 >
            // ratio = T3998 / (T3998 + debits)
            var totalCredits = 0D;
            var creditors = new List<VoucherDetail>();
            // U2 d* <- U2 c* + U2 T3998 <
            // ratio = T3998 / (T3998 + credits)
            var totalDebits = 0D;
            var debitors = new List<VoucherDetail>();
            foreach (var grpU in grpC.GroupBy(static d => d.User))
            {
                if (!grpU.Sum(static d => d.Fund!.Value).IsZero())
                    throw new ApplicationException(
                        $"Unbalanced Voucher ^{voucher.ID}^ {grpU.Key.AsUser()} @{grpC.Key}, run chk 1 first");

                double t3998;
                try
                {
                    t3998 = grpU.SingleOrDefault(static d => d.Title == 3998)?.Fund ?? 0D;
                }
                catch (Exception e)
                {
                    throw new ApplicationException(
                        $"Error during Decoupling Voucher ^{voucher.ID}^ {grpU.Key.AsUser()} @{grpC.Key}", e);
                }

                if (t3998.IsZero())
                    continue;

                if (t3998 > 0)
                {
                    totalCredits += -t3998;
                    ratio.Add(grpU.Key, t3998 / grpU.Sum(static d => Math.Max(0D, d.Fund!.Value)));
                    creditors.AddRange(grpU.Where(static d =>
                        !(d.Title == 6603 && d.SubTitle == null) &&
                        !d.Fund!.Value.IsNonNegative()));
                }
                else
                {
                    totalDebits += -t3998;
                    ratio.Add(grpU.Key, t3998 / grpU.Sum(static d => Math.Min(0D, d.Fund!.Value)));
                    debitors.AddRange(grpU.Where(static d =>
                        !(d.Title == 6603 && d.SubTitle == null) &&
                        !d.Fund!.Value.IsNonPositive()));
                }
            }

            if (!(totalCredits + totalDebits).IsZero())
                throw new ApplicationException(
                    $"Unbalanced Voucher ^{voucher.ID}^ @{grpC.Key}, run chk 1 first");

            if (totalCredits.IsZero())
                continue;

            foreach (var c in creditors)
            foreach (var d in debitors)
                yield return new()
                    {
                        Voucher = voucher,
                        Creditor = c,
                        Debitor = d,
                        Currency = grpC.Key,
                        Fund = -d.Fund!.Value * ratio[d.User]
                            * c.Fund!.Value * ratio[c.User] / totalDebits,
                    };
        }
    }
}

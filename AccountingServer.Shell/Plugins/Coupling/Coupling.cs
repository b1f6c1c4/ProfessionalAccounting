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
    static Coupling()
        => Cfg.RegisterType<CoupleTemplate>("Coupling");

    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var len = expr.Length;
        var aux = Parsing.PureDetailQuery(ref expr, session.Client);
        if (len == expr.Length)
            aux = null;
        var rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
        Parsing.Eof(expr);

        foreach (var configCouple in Cfg.Get<CoupleTemplate>().Couples)
        await foreach (var ret in AnalyzeCouple(session, configCouple, rng, aux))
            yield return ret;
    }

    private static async IAsyncEnumerable<string> ShowEntries(Session session,
        SortedDictionary<string, List<Couple>> dic, Func<string, string> func, bool hideDetail = false)
    {
        foreach (var (a, lst) in dic)
        {
            var fund = await lst.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now));
            yield return $"// {func(a)} {fund.AsCurrency(BaseCurrency.Now)}\n";
            if (!hideDetail || string.IsNullOrEmpty(a))
                foreach (var couple in lst)
                    yield return $"{couple}\n";
        }
    }

    private static async IAsyncEnumerable<string> AnalyzeCouple(Session session, ConfigCouple configCouple,
        DateFilter rng, IQueryCompounded<IDetailQueryAtom> aux)
    {
        var gatherEntries = new SortedDictionary<string, List<Couple>>();
        var scatterEntries = new SortedDictionary<string, List<Couple>>();
        var reimEntries = new SortedDictionary<string, List<Couple>>();
        var misaEntries = new SortedDictionary<string, List<Couple>>();
        var spendEntries = new SortedDictionary<string, List<Couple>>();
        var errEntries = new List<Couple>();

        void Add(SortedDictionary<string, List<Couple>> dic, Couple couple, string key)
        {
            if (!dic.ContainsKey(key))
                dic.Add(key, new());
            dic[key].Add(couple);
        }

        await foreach (var couple in GetCouples(session, configCouple.User, rng))
        {
            var (typeC, nameC) = configCouple.Parse(couple.Creditor);
            var (typeD, nameD) = configCouple.Parse(couple.Debitor);
            switch (typeC, typeD)
            {
                // individual give cash to couple's account
                case (CashCategory.Cash, CashCategory.Cash | CashCategory.IsCouple):
                    if (couple.Creditor.IsMatch(aux))
                        Add(gatherEntries, couple, nameC);
                    break;
                // couple give cash to individual's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.Cash):
                    if (couple.Debitor.IsMatch(aux))
                        Add(scatterEntries, couple, nameD);
                    break;
                // couple's revenue goes to to individual's account
                case (CashCategory.ExtraCash | CashCategory.IsCouple, CashCategory.Cash):
                case (CashCategory.NonCash | CashCategory.IsCouple, CashCategory.Cash):
                    if (couple.Debitor.IsMatch(aux))
                        Add(reimEntries, couple, nameD);
                    break;
                // individuals' spending from couple's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.ExtraCash):
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.NonCash):
                    if (couple.Debitor.IsMatch(aux))
                        Add(misaEntries, couple, nameD);
                    break;
                // couple spending from individuals' account
                case (CashCategory.Cash, CashCategory.ExtraCash | CashCategory.IsCouple):
                case (CashCategory.Cash, CashCategory.NonCash | CashCategory.IsCouple):
                    if (couple.Creditor.IsMatch(aux))
                        Add(spendEntries, couple, nameC);
                    break;
                // couple spending from couple's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.NonCash | CashCategory.IsCouple):
                case (CashCategory.ExtraCash | CashCategory.IsCouple, CashCategory.NonCash | CashCategory.IsCouple):
                    if (couple.Creditor.IsMatch(aux))
                        Add(spendEntries, couple, nameC);
                    break;
                default:
                    errEntries.Add(couple);
                    break;
            }
        }

        yield return $"/* {configCouple.User.AsUser()} Account Summary */\n";
        await foreach (var s in ShowEntries(session, gatherEntries, static a => $"[GATHER] from {a}"))
            yield return s;
        await foreach (var s in ShowEntries(session, scatterEntries, static a => $"[SCATTER] to {a}"))
            yield return s;
        await foreach (var s in ShowEntries(session, reimEntries, static a => $"[REIM] to {a}"))
            yield return s;
        await foreach (var s in ShowEntries(session, misaEntries, static a => $"[MISA] by {a}"))
            yield return s;
        await foreach (var s in ShowEntries(session, spendEntries, static a => $"[SPEND] using {a}", aux == null))
            yield return s;

        if (errEntries.Count != 0)
            yield return $"// ignored {errEntries.Count} items\n";
        if (aux != null)
            foreach (var couple in errEntries)
                yield return $"// ignored: {couple}\n";
    }

    private static IAsyncEnumerable<Couple> GetCouples(Session session, string user, DateFilter rng)
        => session.Accountant
            .SelectVouchersAsync(Parsing.VoucherQuery($"{user.AsUser()} {rng.AsDateRange()}", session.Client))
            .SelectMany(v => Decouple(v, user).ToAsyncEnumerable());

    private static IEnumerable<Couple> Decouple(Voucher voucher, string primaryUser)
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

                if (grpU.Key == primaryUser)
                {
                    // my cash sources
                    var primaryCredits = grpU.Sum(static d => Math.Min(0D, d.Fund!.Value));
                    var primaryCreditRatio = 1 - t3998 / primaryCredits;
                    var primaryCreditors = grpU.Where(static d =>
                        d.Title != 3998 &&
                        !(d.Title == 6603 && d.SubTitle == null) &&
                        !d.Fund!.Value.IsNonNegative());
                    // my spent destinations
                    var primaryDebits = grpU.Sum(static d => Math.Max(0D, d.Fund!.Value));
                    var primaryDebitRatio = 1 - t3998 / primaryDebits;
                    var primaryDebitors = grpU.Where(static d =>
                        d.Title != 3998 &&
                        !(d.Title == 6603 && d.SubTitle == null) &&
                        !d.Fund!.Value.IsNonPositive()).ToList();

                    if (!(primaryCredits + primaryDebits).IsZero())
                        throw new ApplicationException(
                            $"Unbalanced Voucher ^{voucher.ID}^ @{grpC.Key}, run chk 1 first");

                    if (!primaryCredits.IsZero())
                        foreach (var c in primaryCreditors) // all my cash sources
                        foreach (var d in primaryDebitors) // all my spent destinations
                            yield return new()
                                {
                                    Voucher = voucher,
                                    Creditor = c,
                                    Debitor = d,
                                    Currency = grpC.Key,
                                    Fund = -d.Fund!.Value * primaryDebitRatio
                                        * c.Fund!.Value * primaryCreditRatio / primaryDebits,
                                };
                }

                if (t3998.IsZero())
                    continue;

                if (t3998 > 0) // this user sent cash to other users
                {
                    totalCredits += -t3998; // amount of cash sent
                    var userDebits = grpU.Sum(static d => Math.Max(0D, d.Fund!.Value));
                    ratio.Add(grpU.Key, t3998 / userDebits); // proportion of cash sent among cash used
                    // note all cash sources for late use
                    creditors.AddRange(grpU.Where(static d =>
                        !(d.Title == 6603 && d.SubTitle == null) &&
                        !d.Fund!.Value.IsNonNegative()));
                }
                else // this user received cash from other users
                {
                    totalDebits += -t3998; // amount of cash received
                    var userCredits = grpU.Sum(static d => Math.Min(0D, d.Fund!.Value));
                    ratio.Add(grpU.Key, t3998 / userCredits); // proportion of cash received among money spent
                    // note all spent destinations for late use
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

            foreach (var c in creditors) // all cash sources
            foreach (var d in debitors) // all spent destinations
                yield return new()
                    {
                        Voucher = voucher,
                        Creditor = c,
                        Debitor = d,
                        Currency = grpC.Key,
                        // not all cash of c.User are for others, so {*= ratio[c.User]}
                        // not all spent of d.User are from others, so {*= ratio[d.User]}
                        // this destination ows {d.Fund / totalDebits} percent of total money transferred
                        Fund = -d.Fund!.Value * ratio[d.User]
                            * c.Fund!.Value * ratio[c.User] / totalDebits,
                    };
        }
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
            var c = $"{Creditor.User.AsUser()} {Creditor.Title.AsTitle()}{Creditor.SubTitle.AsSubTitle()}";
            if (Creditor.Content != null)
                c += $" {Creditor.Content.Quotation('\'')}";
            if (Creditor.Remark != null)
                c += $" {Creditor.Remark.Quotation('"')}";
            var d = $"{Debitor.User.AsUser()} {Debitor.Title.AsTitle()}{Debitor.SubTitle.AsSubTitle()}";
            if (Debitor.Content != null)
                d += $" {Debitor.Content.Quotation('\'')}";
            if (Debitor.Remark != null)
                d += $" {Debitor.Remark.Quotation('"')}";
            return
                $"^{Voucher.ID}^ {Voucher.Date.AsDate()} {c.CPadRight(18)} -> {d.CPadRight(51)} @{Currency} {Fund.AsCurrency(Currency)}";
        }
    }
}

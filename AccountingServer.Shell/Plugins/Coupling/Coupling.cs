/* Copyright (C) 2020-2024 b1f6c1c4
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
using System.Reflection;
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

    [AttributeUsage(AttributeTargets.Field)]
    private class PropAttribute : Attribute
    {
        public string Proposition { get; }
        public string Verb { get; }
        public bool Collapse { get; }

        public PropAttribute(string p, string v = null, bool collapse = false)
        {
            Proposition = p;
            Verb = v;
            Collapse = collapse;
        }
    }

    private enum Semantics
    {
        [Prop("from", "planned gather")]
        PGather,

        [Prop("from", "gather")]
        UGather,

        [Prop("to", "planned scatter", collapse: true)]
        PScatter,

        [Prop("to", "scatter")]
        UScatter,

        [Prop("against")]
        Reimburse,

        [Prop("by")]
        Misappropriation,

        [Prop("using", collapse: true)]
        Spend,

        [Prop("", collapse: true)]
        Ignored,
    }

    private bool ParseEnum<T>(string s, ref T? v) where T : struct
    {
        var nm = Enum.GetNames(typeof(T))
            .SingleOrDefault(m => string.Equals(s, m, StringComparison.InvariantCultureIgnoreCase));
        if (nm == null)
            return false;

        v = Enum.Parse<T>(nm);
        return true;
    }

    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        Semantics? sem = null;
        Parsing.Token(ref expr, false, s => ParseEnum(s, ref sem));
        var len = expr.Length;
        var aux = Parsing.PureDetailQuery(ref expr, session.Client);
        if (len == expr.Length)
            aux = null;
        var rng = Parsing.Range(ref expr, session.Client) ?? DateFilter.Unconstrained;
        Parsing.Eof(expr);

        foreach (var configCouple in Cfg.Get<CoupleTemplate>().Couples)
        await foreach (var ret in AnalyzeCouple(session, configCouple, rng, aux, sem))
            yield return ret;
    }

    private static async IAsyncEnumerable<string> ShowEntries(Session session, string verb, string prop,
        SortedDictionary<string, List<Couple>> dic, bool hideDetail = false)
    {
        var items = dic.Select(kvp =>
                (Name: kvp.Key, Detail: kvp.Value, FundPromise:
                    kvp.Value.ToAsyncEnumerable().SumAwaitAsync(async couple => couple.Fund *
                        await session.Accountant.Query(couple.Voucher.Date, couple.Currency, BaseCurrency.Now))))
            .ToList();
        var total = await items.ToAsyncEnumerable().SumAwaitAsync(static tuple => tuple.FundPromise);
        var str = $"Total [{verb.ToUpperInvariant()}]: {total.AsFund(BaseCurrency.Now)}";
        yield return $"/**{new string('*', str.Length)}**/\n";
        yield return $"/* {str} */\n";
        yield return $"/**{new string('*', str.Length)}**/\n";
        await foreach (var tuple in items.ToAsyncEnumerable())
        {
            var fund = await tuple.FundPromise;
            yield return
                $"// {fund / total,5:#0.0%} [{verb.ToUpperInvariant()}] {prop} {tuple.Name,-15} {fund.AsFund(BaseCurrency.Now)}\n";
            if (!hideDetail || string.IsNullOrEmpty(tuple.Name))
                foreach (var couple in tuple.Detail)
                    yield return $"{couple}\n";
        }
    }

    private static async IAsyncEnumerable<string> AnalyzeCouple(Session session, ConfigCouple configCouple,
        DateFilter rng, IQueryCompounded<IDetailQueryAtom> aux, Semantics? semantics)
    {
        var sets = Enum.GetValues<Semantics>()
            .ToDictionary(static n => n, static _ => new SortedDictionary<string, List<Couple>>());

        await foreach (var couple in GetCouples(session, configCouple.User, rng, aux))
        {
            var (typeC, nameC) = configCouple.Parse(couple.Creditor);
            var (typeD, nameD) = configCouple.Parse(couple.Debitor);
            Semantics ty;
            string nm;
            switch (typeC, typeD)
            {
                // individual give cash to couple's account
                case (CashCategory.Cash, CashCategory.Cash | CashCategory.IsCouple):
                    if (!couple.Creditor.IsMatch(aux))
                        continue;

                    ty = couple.Voucher.Remark == @"planned" ? Semantics.PGather : Semantics.UGather;
                    nm = couple.Creditor.User.AsUser();
                    break;

                // couple give cash to individual's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.Cash):
                    if (!couple.Debitor.IsMatch(aux))
                        continue;

                    ty = couple.Voucher.Remark == @"planned" ? Semantics.PScatter : Semantics.UScatter;
                    nm = couple.Debitor.User.AsUser();
                    break;

                // couple's revenue goes to to individual's account
                case (CashCategory.ExtraCash | CashCategory.IsCouple, CashCategory.Cash):
                case (CashCategory.NonCash | CashCategory.IsCouple, CashCategory.Cash):
                    if (!couple.Debitor.IsMatch(aux))
                        continue;

                    ty = Semantics.Reimburse;
                    nm = couple.Debitor.User.AsUser();
                    break;

                // individuals' spending from couple's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.ExtraCash):
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.NonCash):
                    if (!couple.Debitor.IsMatch(aux))
                        continue;

                    ty = Semantics.Misappropriation;
                    nm = couple.Debitor.User.AsUser();
                    break;

                // couple spending from individuals' account
                case (CashCategory.Cash, CashCategory.ExtraCash | CashCategory.IsCouple):
                case (CashCategory.Cash, CashCategory.NonCash | CashCategory.IsCouple):
                    if (!couple.Creditor.IsMatch(aux))
                        continue;

                    ty = Semantics.Spend;
                    nm = nameC;
                    break;

                // couple spending from couple's account
                case (CashCategory.Cash | CashCategory.IsCouple, CashCategory.NonCash | CashCategory.IsCouple):
                case (CashCategory.ExtraCash | CashCategory.IsCouple, CashCategory.NonCash | CashCategory.IsCouple):
                    if (!couple.Creditor.IsMatch(aux))
                        continue;

                    ty = Semantics.Spend;
                    nm = nameC;
                    break;
                default:
                    ty = Semantics.Ignored;
                    nm = @"items";
                    break;
            }

            var dic = sets[ty];
            if (!dic.ContainsKey(nm))
                dic.Add(nm, new());
            dic[nm].Add(couple);
        }

        yield return $"/* {configCouple.User.AsUser()} Account Summary during {rng.AsDateRange()} */\n";
        foreach (var (k, v) in sets)
        {
            if (semantics != null && k != semantics)
                continue;

            var prop = k.GetType().GetMember(k.ToString())[0]
                .GetCustomAttribute(typeof(PropAttribute), false) as PropAttribute;
            var verb = prop?.Verb ?? k.ToString();
            var hideDetail = ((prop?.Collapse ?? false) && aux == null)
                || (k == Semantics.Ignored && semantics != k);
            await foreach (var s in ShowEntries(session, verb, prop?.Proposition, v, hideDetail))
                yield return s;
        }
    }

    private static IAsyncEnumerable<Couple> GetCouples(Session session, string user, DateFilter rng,
        IQueryCompounded<IDetailQueryAtom> dq)
    {
        var vq0 = Parsing.VoucherQuery($"{user.AsUser()} {rng.AsDateRange()}", session.Client);
        var vq = dq == null
            ? vq0
            : new IntersectQueries<IVoucherQueryAtom>(vq0, new SimpleVoucherQuery { DetailFilter = dq, Range = rng });
        return session.Accountant
            .SelectVouchersAsync(vq)
            .SelectMany(v => Decouple(v, user).ToAsyncEnumerable());
    }

    private static IEnumerable<Couple> Decouple(Voucher voucher, string primaryUser)
    {
        var lst = new Dictionary<Couple, double>(new CoupleComparer());
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
            {
                var cpl = new Couple
                    {
                        Voucher = voucher, Creditor = c, Debitor = d, Currency = grpC.Key,
                    };
                // not all cash of c.User are for others, so {*= ratio[c.User]}
                // not all spent of d.User are from others, so {*= ratio[d.User]}
                // this destination ows {d.Fund / totalDebits} percent of total money transferred
                var fund = -d.Fund!.Value * ratio[d.User]
                    * c.Fund!.Value * ratio[c.User] / totalDebits;
                if (lst.ContainsKey(cpl))
                    lst[cpl] += fund;
                else
                    lst.Add(cpl, fund);
            }
        }

        foreach (var (k, v) in lst)
        {
            var cpl = k;
            cpl.Fund = v;
            yield return cpl;
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
                $"^{Voucher.ID}^ {Voucher.Date.AsDate()} {c.CPadRight(30)} -> {d.CPadRight(51)} @{Currency} {Fund.AsFund(Currency)}";
        }
    }

    private class DetailEqualityComparer : IEqualityComparer<VoucherDetail>
    {
        public bool Equals(VoucherDetail x, VoucherDetail y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.User == y.User && x.Currency == y.Currency && x.Title == y.Title && x.SubTitle == y.SubTitle &&
                x.Content == y.Content && x.Remark == y.Remark;
        }

        public int GetHashCode(VoucherDetail obj)
            => HashCode.Combine(obj.User, obj.Currency, obj.Title, obj.SubTitle, obj.Content, obj.Remark);
    }

    private class CoupleComparer : IEqualityComparer<Couple>
    {
        private readonly DetailEqualityComparer m_Comparer = new();

        public bool Equals(Couple x, Couple y)
            => Equals(x.Voucher.ID, y.Voucher.ID) && x.Currency == y.Currency
                && m_Comparer.Equals(x.Creditor, y.Creditor)
                && m_Comparer.Equals(x.Debitor, y.Debitor);

        public int GetHashCode(Couple obj)
            => HashCode.Combine(obj.Voucher.ID, obj.Currency,
                m_Comparer.GetHashCode(obj.Creditor), m_Comparer.GetHashCode(obj.Debitor));
    }
}

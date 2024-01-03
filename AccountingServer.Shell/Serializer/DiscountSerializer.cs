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
using System.Text.RegularExpressions;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Serializer;

public class DiscountSerializer : IClientDependable, IEntitySerializer
{
    private const string TheToken = "new Voucher {";

    public Client Client { private get; set; }

    /// <inheritdoc />
    public string PresentVoucher(Voucher voucher) => throw new NotImplementedException();

    /// <inheritdoc />
    public string PresentVoucher(Voucher voucher, string inject) => throw new NotImplementedException();

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetail detail) => throw new NotImplementedException();

    /// <inheritdoc />
    public string PresentVoucherDetail(VoucherDetailR detail) => throw new NotImplementedException();

    /// <inheritdoc />
    public Voucher ParseVoucher(string expr)
    {
        if (!expr.StartsWith(TheToken, StringComparison.Ordinal))
            throw new FormatException("格式错误");

        expr = expr[TheToken.Length..];
        if (ParsingF.Token(ref expr, false, static s => s == "!") == null)
            throw new NotImplementedException();

        var v = GetVoucher(ref expr);
        Parsing.TrimStartComment(ref expr);
        if (Parsing.Token(ref expr, false) != "}")
            throw new FormatException("格式错误" + expr);

        Parsing.Eof(expr);
        return v;
    }

    /// <inheritdoc />
    public VoucherDetail ParseVoucherDetail(string expr) => throw new NotImplementedException();

    public string PresentAsset(Asset asset) => throw new NotImplementedException();
    public Asset ParseAsset(string str) => throw new NotImplementedException();
    public string PresentAmort(Amortization amort) => throw new NotImplementedException();
    public Amortization ParseAmort(string str) => throw new NotImplementedException();

    /// <summary>
    ///     解析记账凭证表达式
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <returns>记账凭证</returns>
    private Voucher GetVoucher(ref string expr)
    {
        Parsing.TrimStartComment(ref expr);
        DateTime? date = Client.Today;
        try
        {
            date = ParsingF.UniqueTime(ref expr, Client);
        }
        catch (Exception)
        {
            // ignore
        }

        var currency = Parsing.Token(ref expr, false, static s => s.StartsWith("@", StringComparison.Ordinal))?[1..]
                .ToUpperInvariant()
            ?? BaseCurrency.Now;

        var lst = new List<Item>();
        List<Item> ds;
        while ((ds = ParseItem(currency, ref expr))?.Any() == true)
            lst.AddRange(ds);

        var d = (double?)0D;
        var t = (double?)0D;
        var reg = new Regex(@"(?<dt>[dt])(?<num>[0-9]+(?:\.[0-9]{1,2})?|null)");
        while (true)
        {
            var res = Parsing.Token(ref expr, false, reg.IsMatch);
            if (res == null)
                break;

            var m = reg.Match(res);
            var num = m.Groups["num"].Value == "null" ? (double?)null : Convert.ToDouble(m.Groups["num"].Value);
            if (m.Groups["dt"].Value == "d")
                d += num;
            else // if (m.Groups["dt"].Value == "t")
                t += num;
        }

        if (!d.HasValue && !t.HasValue)
            throw new ApplicationException("不定项过多");

        var resLst = new List<VoucherDetail>();
        VoucherDetail vd;
        var exprS = new AbbrSerializer { Client = Client };
        while ((vd = exprS.ParseVoucherDetail(ref expr)) != null)
            resLst.Add(vd);

        // Don't use Enumerable.Sum, it ignores null values
        var actualVals = resLst.Aggregate((double?)0D, static (s, dd) => s + dd.Fund);

        if (!d.HasValue || !t.HasValue)
        {
            if (!actualVals.HasValue)
                throw new ApplicationException("不定项过多");

            var sum = lst.Sum(static item => item.Fund - item.DiscountFund);
            if (!d.HasValue)
                d = sum + t + actualVals;
            else // if (!t.HasValue)
                t = -(sum - d + actualVals);
        }


        var total = lst.Sum(static it => it.Fund!.Value);
        foreach (var item in lst)
        {
            item.DiscountFund += d!.Value / total * item.Fund!.Value;
            item.Fund += t.Value / total * item.Fund;
        }

        foreach (var item in lst)
        {
            if (!item.UseActualFund)
                continue;

            item.Fund -= item.DiscountFund;
            item.DiscountFund = 0D;
        }

        foreach (var grp in lst.GroupBy(static it => new()
                         {
                             User = it.User,
                             Currency = it.Currency,
                             Title = it.Title,
                             SubTitle = it.SubTitle,
                             Content = it.Content,
                             Remark = it.Remark,
                         },
                     new DetailEqualityComparer()))
        {
            grp.Key.Fund = grp.Sum(static it => it.Fund!.Value);
            resLst.Add(grp.Key);
        }

        var totalDs = new Dictionary<string, double>();
        foreach (var it in lst)
            if (totalDs.ContainsKey(it.User))
                totalDs[it.User] += it.DiscountFund;
            else
                totalDs[it.User] = it.DiscountFund;

        foreach (var kvp in totalDs)
            if (!kvp.Value.IsZero())
                resLst.Add(
                    new() { User = kvp.Key, Currency = currency, Title = 6603, Fund = -kvp.Value });

        return new() { Type = VoucherType.Ordinary, Date = date, Details = resLst };
    }

    private VoucherDetail ParseVoucherDetail(string currency, ref string expr)
    {
        var lst = new List<string>();

        Parsing.TrimStartComment(ref expr);
        var user = Parsing.Token(ref expr, false, static t => t.StartsWith("U", StringComparison.Ordinal))
            .ParseUserSpec(Client);
        var title = Parsing.Title(ref expr);
        if (title == null)
            if (!AlternativeTitle(ref expr, lst, ref title))
                return null;

        while (true)
        {
            Parsing.TrimStartComment(ref expr);
            if (Parsing.Optional(ref expr, "+"))
                break;

            if (Parsing.Optional(ref expr, ":"))
            {
                expr = $": {expr}";
                break;
            }

            if (lst.Count > 2)
                throw new ArgumentException("语法错误", nameof(expr));

            Parsing.TrimStartComment(ref expr);
            lst.Add(Parsing.Token(ref expr));
        }

        var content = lst.Count >= 1 ? lst[0] : null;
        var remark = lst.Count >= 2 ? lst[1] : null;

        if (content == "G()")
            content = Guid.NewGuid().ToString().ToUpperInvariant();

        if (remark == "G()")
            remark = Guid.NewGuid().ToString().ToUpperInvariant();


        return new()
            {
                User = user,
                Currency = currency,
                Title = title.Title,
                SubTitle = title.SubTitle,
                Content = string.IsNullOrEmpty(content) ? null : content,
                Remark = string.IsNullOrEmpty(remark) ? null : remark,
            };
    }

    private List<Item> ParseItem(string currency, ref string expr)
    {
        var lst = new List<(VoucherDetail Detail, bool Actual)>();

        while (true)
        {
            var actual = Parsing.Optional(ref expr, "!");
            var vd = ParseVoucherDetail(currency, ref expr);
            if (vd == null)
                break;

            lst.Add((vd, actual));
        }

        if (ParsingF.Token(ref expr, false, static s => s == ":") == null)
            return null;

        var resLst = new List<Item>();

        var reg = new Regex(
            @"(?<num>[0-9]+(?:\.[0-9]+)?)(?:(?<equals>=[0-9]+(?:\.[0-9]+)?)|(?<plus>(?:\+[0-9]+(?:\.[0-9]+)?)+)|(?<minus>(?:-[0-9]+(?:\.[0-9]+)?)+))?(?<times>\*[0-9]+(?:\.[0-9]+)?)?");
        while (true)
        {
            var res = Parsing.Token(ref expr, false, reg.IsMatch);
            if (res == null)
                break;

            var m = reg.Match(res);
            var fund0 = Convert.ToDouble(m.Groups["num"].Value);
            var fundd = 0D;
            if (m.Groups["equals"].Success)
                fundd = fund0 - Convert.ToDouble(m.Groups["equals"].Value[1..]);
            else if (m.Groups["plus"].Success)
            {
                var sreg = new Regex(@"\+[0-9]+(?:\.[0-9]+)?");
                foreach (Match sm in sreg.Matches(m.Groups["plus"].Value))
                    fundd += Convert.ToDouble(sm.Value);
                fund0 += fundd;
            }
            else if (m.Groups["minus"].Success)
            {
                var sreg = new Regex(@"-[0-9]+(?:\.[0-9]+)?");
                foreach (Match sm in sreg.Matches(m.Groups["minus"].Value))
                    fundd -= Convert.ToDouble(sm.Value);
            }

            if (m.Groups["times"].Success)
            {
                var mult = Convert.ToDouble(m.Groups["times"].Value[1..]);
                fund0 *= mult;
                fundd *= mult;
            }

            resLst.AddRange(
                lst.Select(
                    d => new Item
                        {
                            User = d.Detail.User,
                            Currency = d.Detail.Currency,
                            Title = d.Detail.Title,
                            SubTitle = d.Detail.SubTitle,
                            Content = d.Detail.Content,
                            Fund = fund0 / lst.Count,
                            DiscountFund = fundd / lst.Count,
                            Remark = d.Detail.Remark,
                            UseActualFund = d.Actual,
                        }));
        }

        ParsingF.Optional(ref expr, ";");

        return resLst;
    }

    private static bool AlternativeTitle(ref string expr, ICollection<string> lst, ref ITitle title)
        => AbbrSerializer.GetAlternativeTitle(ref expr, lst, ref title);

    private sealed class Item : VoucherDetail
    {
        public double DiscountFund { get; set; }

        public bool UseActualFund { get; init; }
    }

    private sealed class DetailEqualityComparer : IEqualityComparer<VoucherDetail>
    {
        public bool Equals(VoucherDetail x, VoucherDetail y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.User != y.User)
                return false;
            if (x.Currency != y.Currency)
                return false;
            if (x.Title != y.Title)
                return false;
            if (x.SubTitle != y.SubTitle)
                return false;
            if (x.Content != y.Content)
                return false;
            if (x.Fund.HasValue != y.Fund.HasValue)
                return false;
            if (x.Fund.HasValue &&
                y.Fund.HasValue)
                if (!(x.Fund.Value - y.Fund.Value).IsZero())
                    return false;

            return x.Remark == y.Remark;
        }

        public int GetHashCode(VoucherDetail obj) => obj.Currency?.GetHashCode() | obj.Title?.GetHashCode() |
            obj.SubTitle?.GetHashCode() | obj.Content?.GetHashCode() | obj.Fund?.GetHashCode() |
            obj.Remark?.GetHashCode() ?? 0;
    }
}

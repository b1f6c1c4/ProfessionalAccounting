/* Copyright (C) 2022 b1f6c1c4
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
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Cheque;

internal class Cheque : PluginBase
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        if (string.IsNullOrWhiteSpace(expr))
        {
            await foreach (var s in PresentSummary(session))
                yield return s;

            yield break;
        }

        var date = Parsing.UniqueTime(ref expr, session.Client) ?? session.Client.Today;
        var tok1 = Parsing.Token(ref expr);
        Voucher voucher;
        if (tok1.StartsWith(@"U", StringComparison.Ordinal))
        {
            var usr1 = tok1[1..];
            if (Parsing.Token(ref expr) != "pay")
                goto fmt;

            var usr2 = Parsing.Token(ref expr, false);
            if (!usr2.StartsWith(@"U", StringComparison.Ordinal))
                goto fmt;

            usr2 = usr2[1..];
            var curr = Parsing.Token(ref expr, false, static s => s.StartsWith(@"@", StringComparison.Ordinal))?[1..] ??
                BaseCurrency.Now;

            var fund = Parsing.DoubleF(ref expr);
            var chq = Parsing.Token(ref expr);
            Parsing.Eof(expr);
            voucher = new() { Date = date, Details = Payment(usr1, usr2, curr, fund, chq) };
        }
        else
        {
            var tok2 = Parsing.Token(ref expr, false);
            var chq = Parsing.Token(ref expr);
            Parsing.Eof(expr);
            switch (tok2)
            {
                case "deposit":
                case "dep":
                case "d":
                    voucher = new() { Date = date, Details = await Bank(session, tok1, chq, 1121) };
                    break;
                case "withdraw":
                case "wd":
                case "w":
                    voucher = new() { Date = date, Details = await Bank(session, tok1, chq, 2201) };
                    break;
                default:
                    goto fmt;
            }
        }

        await session.Accountant.UpsertAsync(voucher);
        yield return session.Serializer.PresentVoucher(voucher).Wrap();

        yield break;

        fmt:
        throw new FormatException("Unknown input expr format");
    }

    private static async IAsyncEnumerable<string> PresentSummary(Session session)
    {
        var dic = new Dictionary<string, ChequeDescriptor>();
        await foreach (var voucher in session.Accountant.RunVoucherQueryAsync("U T112101 + U T220101"))
        foreach (var detail in voucher.Details.Where(static detail
                     => detail.SubTitle == 01 && detail.Title is 1121 or 2201))
        {
            if (detail.Content == null)
            {
                yield return $"!!!!!! empty content found for ^{voucher.ID}^\n";

                continue;
            }

            if (!dic.ContainsKey(detail.Content))
                dic.Add(detail.Content, new() { Creation = DateTime.Now, Surprises = new() });
            var cd = dic[detail.Content];

            void Put(ref VoucherDetailR v)
            {
                if (v != null)
                    cd.Surprises.Add(v);
                v = new(voucher, detail);
            }

            switch (detail.Title)
            {
                default:
                    continue;
                case 1121:
                    if (detail.Fund > 0)
                        Put(ref cd.Payee);
                    else
                        Put(ref cd.PayeeDeposit);
                    break;
                case 2201:
                    if (detail.Fund > 0)
                        Put(ref cd.PayerWithdraw);
                    else
                        Put(ref cd.Payer);
                    break;
            }

            if (cd.Creation > voucher.Date)
                cd.Creation = voucher.Date.Value;
        }

        foreach (var (chq, cd) in dic.OrderByDescending(static kvp => kvp.Value.Importance)
                     .ThenByDescending(static kvp => kvp.Value.Creation))
            if (cd.Surprises.Any())
                yield return $"!!! {chq} with surprise of {cd.Surprises.Count}\n";
            else if (cd.PayerWithdraw != null && cd.Payer == null)
                yield return $"!!! {chq} with withdraw but no payer\n";
            else if (cd.PayeeDeposit != null && cd.Payee == null)
                yield return $"!!! {chq} with deposit but no payee\n";
            else if (cd.Payer == null && cd.Payee == null)
                yield return $"!!! {chq} has no payer nor payee";
            else
            {
                var obj = cd.Payer ?? cd.Payee;
                yield return $"{obj.Voucher.Date.AsDate()} {chq.CPadRight(9)} "
                    + (cd.Payer?.User.AsUser() ?? "someone").CPadRight(7)
                    + " paid "
                    + (cd.Payee?.User.AsUser() ?? "someone").CPadLeft(7)
                    + " "
                    + (cd.Payee?.Fund ?? -cd.Payer!.Fund).AsCurrency(obj.Currency).CPadLeft(14);

                if (cd.Payer != null)
                    if (cd.PayerWithdraw == null)
                        yield return " w/PENDING ";
                    else
                        yield return $" w/{cd.PayerWithdraw.Voucher.Date.AsDate()}";
                else
                    yield return "           ";

                if (cd.Payee != null)
                    if (cd.PayeeDeposit == null)
                        yield return " d/PENDING ";
                    else
                        yield return $" d/{cd.PayeeDeposit.Voucher.Date.AsDate()}";
                else
                    yield return "           ";

                yield return "\n";
            }
    }

    private static List<VoucherDetail> Payment(string user1, string user2, string currency, double fund, string cheque)
        => new()
            {
                new()
                    {
                        User = user1,
                        Currency = currency,
                        Title = 2201,
                        SubTitle = 01,
                        Content = cheque,
                        Fund = -fund,
                    },
                new()
                    {
                        User = user1, Currency = currency, Title = 3998, Fund = fund,
                    },
                new()
                    {
                        User = user2, Currency = currency, Title = 3998, Fund = -fund,
                    },
                new()
                    {
                        User = user2,
                        Currency = currency,
                        Title = 1121,
                        SubTitle = 01,
                        Content = cheque,
                        Fund = fund,
                    },
            };

    private static async ValueTask<string> ResolveAccountUser(Session session, string acct)
    {
        var res = await session.Accountant.RunGroupedQueryAsync($"U T1002 {acct.Quotation('\'')} !U");
        return res.Items.Count() switch
            {
                0 => throw new ApplicationException($"Cannot find bank account {acct}"),
                > 1 => throw new ApplicationException($"Multiple users own the bank account {acct}"),
                _ => (res.Items.Single() as ISubtotalUser)!.User,
            };
    }

    private static async ValueTask<List<VoucherDetail>> Bank(Session session, string acct, string cheque, int title)
    {
        var user = await ResolveAccountUser(session, acct);
        try
        {
            var detail = (await session.Accountant
                    .RunVoucherQueryAsync($"{user.AsUser()} {title.AsTitle()}01 {cheque.Quotation('\'')}")
                    .SingleAsync()).Details
                .Single(d => d.User == user && d.Title == title && d.SubTitle == 01 && d.Content == cheque);
            return new()
                {
                    new()
                        {
                            User = user,
                            Currency = detail.Currency,
                            Title = 1002,
                            Content = acct,
                            Fund = detail.Fund,
                        },
                    new()
                        {
                            User = user,
                            Currency = detail.Currency,
                            Title = title,
                            SubTitle = 01,
                            Content = cheque,
                            Fund = -detail.Fund,
                        },
                };
        }
        catch (InvalidOperationException e)
        {
            throw new ApplicationException("This cheque might have been deposited/withdrawn", e);
        }
    }

    private class ChequeDescriptor
    {
        public DateTime Creation;
        public VoucherDetailR Payee;
        public VoucherDetailR PayeeDeposit;
        public VoucherDetailR Payer;
        public VoucherDetailR PayerWithdraw;
        public List<VoucherDetailR> Surprises;

        public int Importance => Surprises.Count * 1000
            + ImportanceScore(Payer, PayerWithdraw)
            + ImportanceScore(Payee, PayeeDeposit);

        private static int ImportanceScore(VoucherDetailR vd1, VoucherDetailR vd2)
            => vd1 == null ? vd2 == null ? 0 : 100 : vd2 == null ? 10 : 0;
    }
}

/* Copyright (C) 2020-2025 b1f6c1c4
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
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Shell;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Shell.Util;

public static class VoucherHelper
{
    public static void Normalize(this Voucher voucher, Context ctx)
    {
        var details = voucher.Details.Where(static d => !d.Currency.EndsWith('#')).ToList();
        var grpCs = details.GroupBy(static d => d.Currency ?? BaseCurrency.Now).ToList();
        var grpUs = details.GroupBy(d => d.User ?? ctx.Client.User).ToList();
        foreach (var grpC in grpCs)
        {
            var cnt = grpC.Count(static d => !d.Fund.HasValue);
            if (cnt == 1)
                grpC.Single(static d => !d.Fund.HasValue).Fund = -grpC.Sum(static d => d.Fund ?? 0D);
            else if (cnt > 1)
                foreach (var grpU in grpC.GroupBy(static d => d.User))
                {
                    var uunc = grpU.SingleOrDefault(static d => !d.Fund.HasValue);
                    if (uunc != null)
                        uunc.Fund = -grpU.Sum(static d => d.Fund ?? 0D);
                }
        }

        // Automatically append T3998 and T3999 entries
        if (grpCs.Count == 1 && grpUs.Count == 1) // single currency, single users: nothing
        {
            // do nothing
        }
        else if (grpCs.Count == 1 && grpUs.Count > 1) // single currency, multiple users: T3998
            foreach (var grpU in grpUs)
            {
                var sum = grpU.Sum(static d => d.Fund!.Value);
                if (sum.IsZero())
                    continue;

                voucher.Details.Add(
                    new() { User = grpU.Key, Currency = grpCs.First().Key, Title = 3998, Fund = -sum });
            }
        else if (grpCs.Count > 1 && grpUs.Count == 1) // single user, multiple currencies: T3999
            foreach (var grpC in grpCs)
            {
                var sum = grpC.Sum(static d => d.Fund!.Value);
                if (sum.IsZero())
                    continue;

                voucher.Details.Add(
                    new() { User = grpUs.First().Key, Currency = grpC.Key, Title = 3999, Fund = -sum });
            }
        else // multiple user, multiple currencies: T3998 on payer, T3998/T3999 on payee
        {
            var grpUCs = details.GroupBy(d => (d.User ?? ctx.Client.User, d.Currency ?? BaseCurrency.Now))
                .ToList();
            // list of payees (debit)
            var ps = grpUCs.Where(static grp => !grp.Sum(static d => d.Fund!.Value).IsNonPositive()).ToList();
            // list of payers (credit)
            var qs = grpUCs.Where(static grp => !grp.Sum(static d => d.Fund!.Value).IsNonNegative()).ToList();
            // total money received by payees (note: not w.r.t. to currency)
            var pss = ps.Sum(static grp => grp.Sum(static d => d.Fund!.Value));
            // total money sent by payers (note: not w.r.t. to currency)
            var qss = qs.Sum(static grp => grp.Sum(static d => d.Fund!.Value));
            var lst = new List<VoucherDetail>();
            if (ps.Count == 1 && qs.Count >= 1) // one payee, multiple payers
                foreach (var q in qs)
                    QuadratureNormalization(lst, ps[0], pss, q, qss);
            else if (ps.Count >= 1 && qs.Count == 1) // multiple payees, one payer
                foreach (var p in ps)
                    QuadratureNormalization(lst, p, pss, qs[0], qss);
            // For multiple payee + multiple payer case, simply give up
            voucher.Details.AddRange(lst.GroupBy(static d => (d.User, d.Currency, d.Title), static (key, res)
                => new VoucherDetail
                    {
                        User = key.User, Currency = key.Currency, Title = key.Title, Fund = res.Sum(static d => d.Fund),
                    }).Where(static d => !d.Fund!.Value.IsZero()));
        }
    }

    /// <summary>
    ///     When a user (q) pays another user (p) in different money
    /// </summary>
    /// <param name="details">Target</param>
    /// <param name="p">The payee, user and currency</param>
    /// <param name="pss">Total money received by payees</param>
    /// <param name="q">The payer, user and currency</param>
    /// <param name="qss">Total money sent by payers</param>
    private static void QuadratureNormalization(ICollection<VoucherDetail> details,
        IGrouping<(string, string), VoucherDetail> p, double pss,
        IGrouping<(string, string), VoucherDetail> q, double qss)
    {
        var ps0 = p.Sum(static d => d.Fund!.Value); // money received by the payee
        var qs0 = q.Sum(static d => d.Fund!.Value); // money sent by the payer
        var ps = ps0 * qs0 / qss; // money received by the payee from that payer, in payee's currency
        var qs = qs0 * ps0 / pss; // money sent by the payer to that payee, in payer's currency
        // UpCp <- UpCq <- UqCq
        details.Add(new() { User = p.Key.Item1, Currency = p.Key.Item2, Title = 3999, Fund = -ps });
        details.Add(new() { User = p.Key.Item1, Currency = q.Key.Item2, Title = 3999, Fund = -qs });
        details.Add(new() { User = p.Key.Item1, Currency = q.Key.Item2, Title = 3998, Fund = +qs });
        details.Add(new() { User = q.Key.Item1, Currency = q.Key.Item2, Title = 3998, Fund = -qs });
    }
}

/* Copyright (C) 2023 b1f6c1c4
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
using System.Text;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.SpreadSheet;

/// <summary>
///     表格式对账
/// </summary>
internal class SpreadSheet : PluginBase
{
    static SpreadSheet()
        => Cfg.RegisterType<SheetTemplates>("Sheet");

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Session session)
    {
        var abbr = Parsing.Token(ref expr);
        var filter = Parsing.VoucherQuery(ref expr, session.Client);
        Parsing.Eof(expr);

        var cols = Cfg.Get<SheetTemplates>().Templates.Single(t => t.Name == abbr).Columns;
        var queries = cols.Select(col => ParsingF.PureDetailQuery(col.Query, session.Client)).ToList();
        var merged = Enumerable.Zip(cols, queries)
            .Where(static cq => !cq.First.IsComplex)
            .Select(static cq => cq.Second)
            .Aggregate(static (q1, q2) => new UnionQueries<IDetailQueryAtom>(q1, q2));
        var voucherF = new IntersectQueries<IVoucherQueryAtom>(filter, new SimpleVoucherQuery { DetailFilter = merged });
        var sb = new StringBuilder();

        sb.Append("Date\tCurrency");
        foreach (var col in cols)
            sb.Append($"\t{col.Name}");
        sb.Append("\n");
        yield return sb.ToString();

        await foreach (var v in session.Accountant.SelectVouchersAsync(voucherF))
        {
            var isFirst = true;
            foreach (var grpC in v.Details.GroupBy(static d => d.Currency))
            {
                sb.Clear();
                if (isFirst)
                    sb.Append($"{v.Date.AsDate()}\t");
                else
                    sb.Append("\t");
                sb.Append(grpC.Key);
                var hasColumn = false;
                for (var i = 0; i < cols.Count; i++)
                {
                    sb.Append("\t");
                    var flt = grpC.Where(d => d.IsMatch(queries[i]));
                    if (cols[i].IsComplex)
                    {
                        var flag = true;
                        foreach (var d in flt)
                        {
                            if (!flag)
                                sb.Append("; ");
                            flag = false;
                            if (!string.IsNullOrEmpty(d.Content))
                                sb.Append(d.Content);
                            else
                                sb.Append(TitleManager.GetTitleName(d.Title, d.SubTitle));
                            hasColumn = true;
                        }
                    }
                    else if (flt.Any())
                    {
                        var value = flt.Sum(static d => d.Fund);
                        sb.Append(value.AsCurrency(grpC.Key));
                        hasColumn = true;
                    }
                }
                if (!hasColumn)
                    break;
                isFirst = false;
                sb.Append("\n");
                yield return sb.ToString();
            }
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListHelp()
    {
        await foreach (var s in base.ListHelp())
            yield return s;

        foreach (var util in Cfg.Get<SheetTemplates>().Templates)
            yield return $"{util.Name,20}{util.Description}\n";
    }
}

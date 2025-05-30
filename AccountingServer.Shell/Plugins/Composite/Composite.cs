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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Subtotal;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Composite;

/// <summary>
///     复合查询
/// </summary>
internal class Composite : PluginBase
{
    static Composite()
        => Cfg.RegisterType<CompositeTemplates>("Composite");

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> Execute(string expr, Context ctx)
    {
        Template temp = null;
        if (ParsingF.Token(ref expr, true, t => (temp = GetTemplate(t)) != null) == null)
            throw new ArgumentException("找不到模板", nameof(expr));

        var regex = new Regex(@"^[A-Za-z]{3}$");
        var currency =
            Parsing.Token(ref expr, false, regex.IsMatch)?.ToUpperInvariant()
            ?? BaseCurrency.Now;

        DateFilter the;
        if (string.IsNullOrWhiteSpace(expr))
            the = DateRange(temp.Day, ctx.Client.Today);
        else
        {
            the = ParsingF.Range(ref expr, ctx.Client) ?? throw new ArgumentException("语法错误", nameof(expr));
            ParsingF.Eof(expr);
        }

        yield return (await DoInquiry(the, temp, currency, ctx)).Item1;
    }

    public static Template GetTemplate(string name) => Cfg.Get<CompositeTemplates>().Templates.SingleOrDefault(
        p => p.Name == name);

    public static DateFilter DateRange(int day, DateTime now)
    {
        DateFilter the;
        if (day == 0)
            return new(
                new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddDays(-1));

        if (now.Day > day)
            the = new(
                new(now.Year, now.Month, day + 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, day, 0, 0, 0, DateTimeKind.Utc).AddMonths(1));
        else
            the = new(
                new DateTime(now.Year, now.Month, day + 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1),
                new(now.Year, now.Month, day, 0, 0, 0, DateTimeKind.Utc));
        return the;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<string> ListHelp()
    {
        await foreach (var s in base.ListHelp())
            yield return s;
        foreach (var tmp in Cfg.Get<CompositeTemplates>().Templates)
            yield return $"{tmp.Name}\n";
    }

    /// <summary>
    ///     执行查询
    /// </summary>
    /// <param name="rng">日期过滤器</param>
    /// <param name="inq">查询</param>
    /// <param name="baseCurrency">记账本位币</param>
    /// <param name="ctx">客户端上下文</param>
    /// <returns>执行结果</returns>
    public async ValueTask<(string, double)> DoInquiry(DateFilter rng, BaseInquiry inq, string baseCurrency,
        Context ctx)
    {
        var visitor = new InquiriesVisitor(ctx, rng, baseCurrency);
        var val = await inq.Accept(visitor);
        return (visitor.Result, val);
    }

    /// <summary>
    ///     使用分隔符连接字符串
    /// </summary>
    /// <param name="path">原字符串</param>
    /// <param name="token">要连接上的字符串</param>
    /// <param name="interval">分隔符</param>
    /// <returns>新字符串</returns>
    public static string Merge(string path, string token, string interval = "/")
    {
        if (path.Length == 0)
            return token;

        return path + interval + token;
    }
}

internal class InquiriesVisitor : IInquiryVisitor<ValueTask<double>>
{
    private readonly string m_BaseCurrency;
    private readonly DateFilter m_Rng;
    private readonly StringBuilder m_Sb = new();
    private readonly Context m_Ctx;

    private string m_Path = "";

    public InquiriesVisitor(Context ctx, DateFilter rng, string baseCurrency)
    {
        m_Ctx = ctx;
        m_BaseCurrency = baseCurrency;
        m_Rng = rng;
    }

    public string Result => m_Sb.ToString();

    public async ValueTask<double> Visit(InquiriesHub inq)
    {
        var tmp = m_Path;
        m_Path = Composite.Merge(m_Path, inq.Name);
        var res = await inq.Inquiries.ToAsyncEnumerable().SumAwaitAsync(q => q.Accept(this));
        m_Path = tmp;
        return res;
    }

    public async ValueTask<double> Visit(SimpleInquiry inq)
        => await VisitImpl(inq, o => $"{inq.Query} {o}");

    public async ValueTask<double> Visit(ComplexInquiry inq)
        => await VisitImpl(inq, o => $"{{{inq.VoucherQuery}}}*{{U A {o}}} : {inq.Emit}");

    private async ValueTask<double> VisitImpl(Inquiry inq, Func<string, string> gen)
    {
        var val = 0D;
        IFundFormatter fmt = new BaseFundFormatter();
        if (!double.IsNaN(inq.Ratio))
            fmt = new RatioDecorator(fmt, inq.Ratio);

        var o =
            $"[{(inq.IsLeftExtended ? "" : m_Rng.StartDate.AsDate())}~{m_Rng.EndDate.AsDate()}] {(inq.General ? "G" : "")}";
        var query = ParsingF.GroupedQuery(
            $"{gen(o)}`{(inq.ByCurrency ? "C" : "")}{(inq.ByTitle ? "t" : "")}{(inq.BySubTitle ? "s" : "")}{(inq.ByContent ? "c" : "")}{(inq.ByRemark ? "r" : "")}{(inq.ByCurrency || inq.ByTitle || inq.BySubTitle || inq.ByContent || inq.ByRemark ? "" : "v")}{(!inq.ByCurrency ? "X" : "")}",
            m_Ctx.Client);
        var gq = await m_Ctx.Accountant.SelectVoucherDetailsGroupedAsync(query);
        if (inq.ByCurrency)
            foreach (var grp in gq.Items.Cast<ISubtotalCurrency>())
            {
                var curr = grp.Currency;
                var ratio = await m_Ctx.Accountant.Query(m_Rng.EndDate!.Value, curr, m_BaseCurrency);

                var theFmt = new CurrencyDecorator(fmt, curr, ratio);

                val += theFmt.GetFund(grp.Fund);

                await foreach (var s in
                    new SubtotalVisitor(Composite.Merge(m_Path, inq.Name), theFmt, inq.HideContent)
                            {
                                Client = m_Ctx.Client,
                            }
                        .PresentSubtotal(grp, query.Subtotal, m_Ctx.Serializer))
                    m_Sb.Append(s);
            }
        else
        {
            val += fmt.GetFund(gq.Fund);

            await foreach (var s in
                new SubtotalVisitor(Composite.Merge(m_Path, inq.Name), fmt, inq.HideContent)
                        {
                            Client = m_Ctx.Client,
                        }
                    .PresentSubtotal(gq, query.Subtotal, m_Ctx.Serializer))
                m_Sb.Append(s);
        }

        return val;
    }
}

internal interface IFundFormatter
{
    double GetFund(double value);

    string GetFundString(double value);
}

internal class BaseFundFormatter : IFundFormatter
{
    public double GetFund(double value) => value;
    public string GetFundString(double value) => $"{value:R}";
}

internal class BaseFundFormatterDecorator : IFundFormatter
{
    protected readonly IFundFormatter Internal;

    protected BaseFundFormatterDecorator(IFundFormatter fmt) => Internal = fmt;

    public virtual double GetFund(double value) => Internal.GetFund(value);
    public virtual string GetFundString(double value) => Internal.GetFundString(value);
}

internal class CurrencyDecorator : BaseFundFormatterDecorator
{
    private readonly string m_Curr;
    private readonly double m_ExgRatio;

    public CurrencyDecorator(IFundFormatter fmt, string curr, double exgRatio) : base(fmt)
    {
        m_Curr = curr;
        m_ExgRatio = exgRatio;
    }

    public override double GetFund(double value) => Internal.GetFund(value) * m_ExgRatio;

    public override string GetFundString(double value) =>
        $"{m_Curr}\t{m_ExgRatio}\t{Internal.GetFundString(value)}";
}

internal class RatioDecorator : BaseFundFormatterDecorator
{
    private readonly double m_Ratio;

    public RatioDecorator(IFundFormatter fmt, double ratio) : base(fmt) => m_Ratio = ratio;

    public override double GetFund(double value) => Internal.GetFund(value) * m_Ratio;
    public override string GetFundString(double value) => $"{Internal.GetFundString(value)}\t{m_Ratio}";
}

internal class SubtotalVisitor : StringSubtotalVisitor
{
    private readonly IFundFormatter m_Formatter;
    private readonly bool m_HideContent;

    private string m_Path;
    private int? m_Title;

    public SubtotalVisitor(string path, IFundFormatter formatter, bool hideContent)
    {
        m_Path = path;
        m_Formatter = formatter;
        m_HideContent = hideContent;
    }

    private async IAsyncEnumerable<string> ShowSubtotal(ISubtotalResult sub)
    {
        if (sub.Items == null)
            yield return $"{m_Path}\t{m_Formatter.GetFundString(sub.Fund)}\n";
        await foreach (var s in VisitChildren(sub))
            yield return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRoot sub)
        => ShowSubtotal(sub);

    public override IAsyncEnumerable<string> Visit(ISubtotalDate sub)
    {
        var prev = m_Path;
        m_Path = Composite.Merge(m_Path, sub.Date.AsDate(sub.Level));
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalUser sub)
    {
        Depth++; // Hack
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalVoucherType sub)
    {
        Depth++; // Hack
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalTitleKind sub)
    {
        Depth++; // Hack
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalVoucherRemark sub)
    {
        Depth++; // Hack
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalCurrency sub)
    {
        Depth++; // Hack
        return ShowSubtotal(sub);
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalTitle sub)
    {
        var prev = m_Path;
        m_Path = Composite.Merge(m_Path, TitleManager.GetTitleName(sub.Title));
        m_Title = sub.Title;
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalSubTitle sub)
    {
        var prev = m_Path;
        m_Path = Composite.Merge(m_Path, TitleManager.GetTitleName(m_Title, sub.SubTitle));
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalContent sub)
    {
        var prev = m_Path;
        if (!m_HideContent)
            m_Path = Composite.Merge(m_Path, sub.Content);
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalRemark sub)
    {
        var prev = m_Path;
        m_Path = Composite.Merge(m_Path, sub.Remark);
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }

    public override IAsyncEnumerable<string> Visit(ISubtotalValue sub)
    {
        var prev = m_Path;
        m_Path = Composite.Merge(m_Path, $"{sub.Value:R}");
        var s = ShowSubtotal(sub);
        m_Path = prev;
        return s;
    }
}

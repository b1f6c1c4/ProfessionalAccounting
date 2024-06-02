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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell;

/// <summary>
///     表达式解释器
/// </summary>
public class Facade
{
    private readonly AccountingShell m_AccountingShell;

    /// <summary>
    ///     复合表达式解释器
    /// </summary>
    private readonly ShellComposer m_Composer;

    private readonly DbSession m_Db;

    private readonly ExchangeShell m_ExchangeShell;

    public Facade(string uri = null, string db = null)
    {
        m_Db = new(uri, db);
        m_AccountingShell = new();
        m_ExchangeShell = new();
        m_Composer =
            new()
                {
                    new CheckShell(),
                    new CarryShell(),
                    m_ExchangeShell,
                    new AssetShell(),
                    new AmortizationShell(),
                    new PluginShell(),
                    m_AccountingShell,
                };
    }

    public Session CreateSession(string user, DateTime dt, string spec = null, int limit = 0)
        => new(m_Db, user, dt, spec, limit);

    /// <summary>
    ///     空记账凭证的表示
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string EmptyVoucher(Session session) => session.Serializer.PresentVoucher(null).Wrap();

    /// <summary>
    ///     执行表达式
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="expr">表达式</param>
    /// <returns>执行结果</returns>
    public IAsyncEnumerable<string> Execute(Session session, string expr)
    {
        switch (expr)
        {
            case null:
            case "":
            case " ":
                return HelloWorld();
            case "T":
                return ListTitles().ToAsyncEnumerable();
            case "?":
                return ListHelp();
            case "reload":
                return Cfg.ReloadAll();
            case "die":
                Environment.Exit(0);
                break;
            case "version":
                return ListVersions().ToAsyncEnumerable();
        }

        switch (ParsingF.Optional(ref expr, "time ", "slow "))
        {
            case "time ":
                var repetition = (ulong)ParsingF.DoubleF(ref expr);
                return ExecuteNormal(session, expr, repetition);
            case "slow ":
                var slow = (int)ParsingF.DoubleF(ref expr);
                return ExecuteProfile(session, expr, slow);
            default:
                return ExecuteNormal(session, expr, null);
        }
    }

    private async IAsyncEnumerable<string> ExecuteProfile(Session session, string expr, int slow)
    {
        if (!await m_Db.StartProfiler(slow))
            throw new ApplicationException("Failed to start profiler");

        try
        {
            await foreach (var s in m_Composer.Execute(expr, session));
            await foreach (var p in m_Db.StopProfiler())
                yield return p;
        }
        finally
        {
            m_Db.StopProfiler();
        }
    }

    private async IAsyncEnumerable<string> ExecuteNormal(Session session, string expr, ulong? repetition)
    {
        var sw = new Stopwatch();
        var output = (repetition ?? 1) == 1;

        for (var i = 0UL; i < (repetition ?? 1UL); i++)
        {
            sw.Start();
            await foreach (var s in m_Composer.Execute(expr, session))
                if (output)
                    yield return s;

            sw.Stop();
            if (!output)
                yield return ".";
        }
        if (!output)
            yield return "\n";

        if (repetition.HasValue)
            yield return $"time = {(double)sw.ElapsedMilliseconds / repetition.Value}ms\n";
    }

    /// <summary>
    ///     执行基础表达式
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="expr">表达式</param>
    /// <returns>执行结果</returns>
    public IAsyncEnumerable<string> SafeExecute(Session session, string expr)
    {
        switch (expr)
        {
            case null:
            case "":
            case " ":
                return HelloWorld();
            case "T":
                return ListTitles().ToAsyncEnumerable();
            case "?":
                return ListHelp();
            case "version":
                return ListVersions().ToAsyncEnumerable();
        }

        return m_AccountingShell.Execute(expr, session);
    }

    private static async IAsyncEnumerable<string> HelloWorld()
    {
        const string str = "Hello, World!\n";
        foreach (var ch in str)
        {
            await Task.Delay(250);
            yield return $"{ch}";
        }
    }

    #region Exchange

    // ReSharper disable once UnusedMember.Global
    public void EnableTimer() => m_ExchangeShell.EnableTimer(m_Db);

    // ReSharper disable once UnusedMember.Global
    public async Task ImmediateExchange(Action<string, bool> logger)
    {
        m_Db.ExchangeLogger = logger;
        await m_ExchangeShell.ImmediateExchange(m_Db);
        m_Db.ExchangeLogger = null;
    }

    #endregion

    #region Miscellaneous

    /// <summary>
    ///     显示控制台帮助
    /// </summary>
    /// <returns>帮助内容</returns>
    private static IAsyncEnumerable<string> ListHelp()
        => ResourceHelper.ReadResource("AccountingServer.Shell.Resources.Document.txt", typeof(Facade));

    /// <summary>
    ///     显示所有会计科目及其编号
    /// </summary>
    /// <returns>会计科目及其编号</returns>
    private static IEnumerable<string> ListTitles()
    {
        foreach (var title in TitleManager.Titles)
        {
            yield return $"{title.Id.AsTitle(),-9}{title.Name}\n";
            foreach (var subTitle in title.SubTitles)
                yield return $"{title.Id.AsTitle()}{subTitle.Id.AsSubTitle(),-4}{subTitle.Name}\n";
        }
    }

    /// <summary>
    ///     显示控制台帮助
    /// </summary>
    /// <returns>帮助内容</returns>
    private static IEnumerable<string> ListVersions()
        => AppDomain.CurrentDomain.GetAssemblies().Select(static asm =>
            {
                var nm = asm.GetName();
                var iv = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                return $"{nm.Name}@{nm.Version}@{iv}\n";
            }).OrderBy(static s => s);

    #endregion

    #region Upsert

    /// <summary>
    ///     更新或添加记账凭证
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">记账凭证的表达式</param>
    /// <returns>新记账凭证的表达式</returns>
    public async ValueTask<string> ExecuteVoucherUpsert(Session session, string str)
    {
        var voucher = session.Serializer.ParseVoucher(str);
        var details = voucher.Details.Where(static d => !d.Currency.EndsWith('#')).ToList();
        var grpCs = details.GroupBy(static d => d.Currency ?? BaseCurrency.Now).ToList();
        var grpUs = details.GroupBy(d => d.User ?? session.Client.User).ToList();
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
            var grpUCs = details.GroupBy(d => (d.User ?? session.Client.User, d.Currency ?? BaseCurrency.Now))
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

        if (!await session.Accountant.UpsertAsync(voucher))
            throw new ApplicationException("更新或添加失败");

        return session.Serializer.PresentVoucher(voucher).Wrap();
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

    /// <summary>
    ///     更新或添加资产
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">资产表达式</param>
    /// <returns>新资产表达式</returns>
    public async ValueTask<string> ExecuteAssetUpsert(Session session, string str)
    {
        var asset = session.Serializer.ParseAsset(str);

        if (!await session.Accountant.UpsertAsync(asset))
            throw new ApplicationException("更新或添加失败");

        return session.Serializer.PresentAsset(asset).Wrap();
    }

    /// <summary>
    ///     更新或添加摊销
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">摊销表达式</param>
    /// <returns>新摊销表达式</returns>
    public async ValueTask<string> ExecuteAmortUpsert(Session session, string str)
    {
        var amort = session.Serializer.ParseAmort(str);

        if (!await session.Accountant.UpsertAsync(amort))
            throw new ApplicationException("更新或添加失败");

        return session.Serializer.PresentAmort(amort).Wrap();
    }

    #endregion

    #region Removal

    /// <summary>
    ///     删除记账凭证
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">记账凭证表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteVoucherRemoval(Session session, string str)
    {
        var voucher = session.Serializer.ParseVoucher(str);

        if (voucher.ID == null)
            throw new ApplicationException("编号未知");

        return await session.Accountant.DeleteVoucherAsync(voucher.ID);
    }

    /// <summary>
    ///     删除资产
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">资产表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteAssetRemoval(Session session, string str)
    {
        var asset = session.Serializer.ParseAsset(str);

        if (!asset.ID.HasValue)
            throw new ApplicationException("编号未知");

        return await session.Accountant.DeleteAssetAsync(asset.ID.Value);
    }

    /// <summary>
    ///     删除摊销
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="str">摊销表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteAmortRemoval(Session session, string str)
    {
        var amort = session.Serializer.ParseAmort(str);

        if (!amort.ID.HasValue)
            throw new ApplicationException("编号未知");

        return await session.Accountant.DeleteAmortizationAsync(amort.ID.Value);
    }

    #endregion
}

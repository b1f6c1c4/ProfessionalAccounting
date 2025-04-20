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
public partial class Facade
{
    private readonly AccountingShell m_AccountingShell;

    /// <summary>
    ///     复合表达式解释器
    /// </summary>
    private readonly ShellComposer m_Composer;

    private readonly DbSession m_Db;

    private readonly ExchangeShell m_ExchangeShell;

    private readonly SessionManager m_SessionManager;

    private readonly AuthnManager m_Auth;

    public Facade(string uri = null, string db = null)
    {
        m_Db = new(uri, db);
        m_AccountingShell = new();
        m_ExchangeShell = new();
        m_SessionManager = new(m_Db);
        m_Auth = new(m_Db);
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

    public Context CreateCtx(string user, DateTime dt, Identity id,
            string assume = null, string spec = null, int limit = 0)
        => new(m_Db, user, dt, id.Assume(assume), id, null, null, spec, limit);

    /// <summary>
    ///     空记账凭证的表示
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public string EmptyVoucher(Context ctx) => ctx.Serializer.PresentVoucher(null).Wrap();

    /// <summary>
    ///     执行表达式
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="expr">表达式</param>
    /// <returns>执行结果</returns>
    public IAsyncEnumerable<string> Execute(Context ctx, string expr)
    {
        ctx.Session?.Extend();

        switch (expr)
        {
            case null:
            case "":
            case " ":
                return HelloWorld();
            case "error now":
                throw new ApplicationException("An example exception");
            case "error":
                return HelloWorld(true);
            case "T":
                return ListTitles().ToAsyncEnumerable();
            case "?":
                return ListHelp();
            case "reload":
                ctx.Identity.WillInvoke("reload");
                return Cfg.ReloadAll();
            case "die":
                ctx.Identity.WillInvoke("die");
                Environment.Exit(0);
                break;
            case "version":
                return ListVersions().ToAsyncEnumerable();
            case "me":
                return ListIdentity(ctx);
            case "logout":
                return Logout(ctx);
            case "save-cert":
                return SaveCert(ctx);
        }

        if (expr.Initial() == "invite")
            return Invite(ctx, expr.Rest());

        ctx.Identity.WillLogin(ctx.Client.User);
        switch (ParsingF.Optional(ref expr, "time ", "slow "))
        {
            case "time ":
                ctx.Identity.WillInvoke("time");
                var repetition = (ulong)ParsingF.DoubleF(ref expr);
                return ExecuteNormal(ctx, expr, repetition);
            case "slow ":
                ctx.Identity.WillInvoke("slow");
                var slow = (int)ParsingF.DoubleF(ref expr);
                return ExecuteProfile(ctx, expr, slow);
            default:
                return ExecuteNormal(ctx, expr, null);
        }
    }

    private async IAsyncEnumerable<string> ExecuteProfile(Context ctx, string expr, int slow)
    {
        if (!await m_Db.StartProfiler(slow))
            throw new ApplicationException("Failed to start profiler");

        try
        {
            await foreach (var s in m_Composer.Execute(expr, ctx, ""));
            await foreach (var p in m_Db.StopProfiler())
                yield return p;
        }
        finally
        {
            m_Db.StopProfiler();
        }
    }

    private async IAsyncEnumerable<string> ExecuteNormal(Context ctx, string expr, ulong? repetition)
    {
        var sw = new Stopwatch();
        var output = (repetition ?? 1) == 1;

        for (var i = 0UL; i < (repetition ?? 1UL); i++)
        {
            sw.Start();
            await foreach (var s in m_Composer.Execute(expr, ctx, ""))
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
    /// <param name="ctx">客户端上下文</param>
    /// <param name="expr">表达式</param>
    /// <returns>执行结果</returns>
    public IAsyncEnumerable<string> SafeExecute(Context ctx, string expr)
    {
        switch (expr)
        {
            case null:
            case "":
            case " ":
                return HelloWorld();
            case "error now":
                throw new ApplicationException("An example exception");
            case "error":
                return HelloWorld(true);
            case "T":
                return ListTitles().ToAsyncEnumerable();
            case "?":
                return ListHelp();
            case "version":
                return ListVersions().ToAsyncEnumerable();
            case "me":
                return ListIdentity(ctx);
        }

        ctx.Identity.WillLogin(ctx.Client.User);
        return m_AccountingShell.Execute(expr, ctx, "");
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

    private static async IAsyncEnumerable<string> HelloWorld(bool error = false)
    {
        const string str = "Hello, World!\n";
        foreach (var ch in str)
        {
            await Task.Delay(250);
            yield return $"{ch}";
        }
        if (error)
            throw new ApplicationException("An example exception -- you should've seen the hello world!");
    }

    #endregion

    #region Upsert

    /// <summary>
    ///     更新或添加记账凭证
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">记账凭证的表达式</param>
    /// <returns>新记账凭证的表达式</returns>
    public async ValueTask<string> ExecuteVoucherUpsert(Context ctx, string str)
    {
        var voucher = ctx.Serializer.ParseVoucher(str);
        voucher.Normalize(ctx);

        ctx.Identity.WillWrite(voucher, true);
        ctx.Identity.WillWrite(await ctx.Accountant.SelectVoucherAsync(voucher.ID));
        if (!await ctx.Accountant.UpsertAsync(voucher))
            throw new ApplicationException("更新或添加失败");

        return ctx.Serializer.PresentVoucher(voucher).Wrap();
    }

    /// <summary>
    ///     更新或添加资产
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">资产表达式</param>
    /// <returns>新资产表达式</returns>
    public async ValueTask<string> ExecuteAssetUpsert(Context ctx, string str)
    {
        var asset = ctx.Serializer.ParseAsset(str);

        ctx.Identity.WillAccess(asset);
        ctx.Identity.WillAccess(await ctx.Accountant.SelectAssetAsync(asset.ID));
        if (!await ctx.Accountant.UpsertAsync(asset))
            throw new ApplicationException("更新或添加失败");

        return ctx.Serializer.PresentAsset(asset).Wrap();
    }

    /// <summary>
    ///     更新或添加摊销
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">摊销表达式</param>
    /// <returns>新摊销表达式</returns>
    public async ValueTask<string> ExecuteAmortUpsert(Context ctx, string str)
    {
        var amort = ctx.Serializer.ParseAmort(str);

        ctx.Identity.WillAccess(amort);
        ctx.Identity.WillAccess(await ctx.Accountant.SelectAmortizationAsync(amort.ID));
        if (!await ctx.Accountant.UpsertAsync(amort))
            throw new ApplicationException("更新或添加失败");

        return ctx.Serializer.PresentAmort(amort).Wrap();
    }

    #endregion

    #region Removal

    /// <summary>
    ///     删除记账凭证
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">记账凭证表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteVoucherRemoval(Context ctx, string str)
    {
        var voucher = ctx.Serializer.ParseVoucher(str);

        if (voucher.ID == null)
            throw new ApplicationException("编号未知");

        ctx.Identity.WillWrite(await ctx.Accountant.SelectVoucherAsync(voucher.ID));
        return await ctx.Accountant.DeleteVoucherAsync(voucher.ID);
    }

    /// <summary>
    ///     删除资产
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">资产表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteAssetRemoval(Context ctx, string str)
    {
        var asset = ctx.Serializer.ParseAsset(str);

        if (!asset.ID.HasValue)
            throw new ApplicationException("编号未知");

        ctx.Identity.WillAccess(await ctx.Accountant.SelectAssetAsync(asset.ID.Value));
        return await ctx.Accountant.DeleteAssetAsync(asset.ID.Value);
    }

    /// <summary>
    ///     删除摊销
    /// </summary>
    /// <param name="ctx">客户端上下文</param>
    /// <param name="str">摊销表达式</param>
    /// <returns>是否成功</returns>
    public async ValueTask<bool> ExecuteAmortRemoval(Context ctx, string str)
    {
        var amort = ctx.Serializer.ParseAmort(str);

        if (!amort.ID.HasValue)
            throw new ApplicationException("编号未知");

        ctx.Identity.WillAccess(await ctx.Accountant.SelectAmortizationAsync(amort.ID.Value));
        return await ctx.Accountant.DeleteAmortizationAsync(amort.ID.Value);
    }

    #endregion
}

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
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;

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
                return MetaConfigManager.ReloadAll().ToAsyncEnumerable();
            case "die":
                Environment.Exit(0);
                break;
        }

        return m_Composer.Execute(expr, session);
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
        var grpCs = voucher.Details.GroupBy(static d => d.Currency ?? BaseCurrency.Now).ToList();
        var grpUs = voucher.Details.GroupBy(d => d.User ?? session.Client.User).ToList();
        foreach (var grpC in grpCs)
        {
            var unc = grpC.SingleOrDefault(static d => !d.Fund.HasValue);
            if (unc != null)
                unc.Fund = -grpC.Sum(static d => d.Fund ?? 0D);
        }

        if (grpCs.Count == 1 && grpUs.Count > 1)
            foreach (var grpU in grpUs)
            {
                var sum = grpU.Sum(static d => d.Fund!.Value);
                if (sum.IsZero())
                    continue;

                voucher.Details.Add(
                    new() { User = grpU.Key, Currency = grpCs.First().Key, Title = 3998, Fund = -sum });
            }
        else if (grpCs.Count > 1 && grpUs.Count == 1)
            foreach (var grpC in grpCs)
            {
                var sum = grpC.Sum(static d => d.Fund!.Value);
                if (sum.IsZero())
                    continue;

                voucher.Details.Add(
                    new() { User = grpUs.First().Key, Currency = grpC.Key, Title = 3999, Fund = -sum });
            }
        else
        {
            var grpUCs = voucher.Details.GroupBy(d => (d.User ?? session.Client.User, d.Currency ?? BaseCurrency.Now))
                .ToList();
            var ps = grpUCs.Where(static grp => !grp.Sum(static d => d.Fund!.Value).IsNonPositive()).ToList();
            var qs = grpUCs.Where(static grp => !grp.Sum(static d => d.Fund!.Value).IsNonNegative()).ToList();
            var pss = ps.Sum(static grp => grp.Sum(static d => d.Fund!.Value));
            var qss = qs.Sum(static grp => grp.Sum(static d => d.Fund!.Value));
            var lst = new List<VoucherDetail>();
            if (ps.Count == 1 && qs.Count >= 1)
                foreach (var q in qs)
                    QuadratureNormalization(lst, ps[0], pss, q, qss);
            else if (ps.Count >= 1 && qs.Count == 1)
                foreach (var p in ps)
                    QuadratureNormalization(lst, p, pss, qs[0], qss);
            voucher.Details.AddRange(lst.GroupBy(static d => (d.User, d.Currency, d.Title), static (key, res)
                => new VoucherDetail
                    {
                        User = key.User, Currency = key.Currency, Title = key.Title, Fund = res.Sum(static d => d.Fund),
                    }));
        }

        if (!await session.Accountant.UpsertAsync(voucher))
            throw new ApplicationException("更新或添加失败");

        return session.Serializer.PresentVoucher(voucher).Wrap();
    }

    private static void QuadratureNormalization(ICollection<VoucherDetail> details,
        IGrouping<(string, string), VoucherDetail> p, double pss,
        IGrouping<(string, string), VoucherDetail> q, double qss)
    {
        var ps0 = p.Sum(static d => d.Fund!.Value);
        var qs0 = q.Sum(static d => d.Fund!.Value);
        var ps = ps0 * qs0 / qss;
        var qs = qs0 * ps0 / pss;
        // UpCp -> UqCp -> UqCq
        details.Add(new() { User = p.Key.Item1, Currency = p.Key.Item2, Title = 3998, Fund = -ps / 2 });
        details.Add(new() { User = q.Key.Item1, Currency = p.Key.Item2, Title = 3998, Fund = +ps / 2 });
        details.Add(new() { User = q.Key.Item1, Currency = p.Key.Item2, Title = 3999, Fund = -ps / 2 });
        details.Add(new() { User = q.Key.Item1, Currency = q.Key.Item2, Title = 3999, Fund = -qs / 2 });
        // UpCp -> UpCq -> UqCq
        details.Add(new() { User = p.Key.Item1, Currency = p.Key.Item2, Title = 3999, Fund = -ps / 2 });
        details.Add(new() { User = p.Key.Item1, Currency = q.Key.Item2, Title = 3999, Fund = -qs / 2 });
        details.Add(new() { User = p.Key.Item1, Currency = q.Key.Item2, Title = 3998, Fund = +qs / 2 });
        details.Add(new() { User = q.Key.Item1, Currency = q.Key.Item2, Title = 3998, Fund = -qs / 2 });
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

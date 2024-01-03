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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.DistributedTest;

public abstract class QueryTestBase
{
    private readonly Client m_Client = new() { User = "b1", Today = DateTime.UtcNow.Date };

    protected abstract ValueTask PrepareAsset(Asset asset);
    protected abstract ValueTask PrepareAmort(Amortization amort);
    protected abstract ValueTask<bool> RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query);
    protected abstract ValueTask<bool> RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query);
    protected abstract ValueTask ResetAssets();
    protected abstract ValueTask ResetAmorts();

    public virtual async Task RunTestA(bool expectedA, bool expectedB, string query)
    {
        var asset = AssetDataProvider.Create("2017-02-03", DepreciationMethod.StraightLine);
        asset.Name = "a nm";
        asset.User = "b1";
        asset.Remark = "rmk1";
        var amort = AmortDataProvider.Create("2017-04-05", AmortizeInterval.EveryDay);
        amort.Name = "o nm";
        amort.User = "b2";
        amort.Remark = null;

        await ResetAssets();
        await ResetAmorts();
        await PrepareAsset(asset);
        await PrepareAmort(amort);
        Assert.Equal(expectedA, await RunAssetQuery(ParsingF.DistributedQuery(query, m_Client)));
        Assert.Equal(expectedB, await RunAmortQuery(ParsingF.DistributedQuery(query, m_Client)));
        await ResetAssets();
        await ResetAmorts();
    }

    protected class DataProvider : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = new()
            {
                new object[] { true, false, "9F4FDBA8-94BD-45C2-AF53-36BCFFB591FF" },
                new object[] { true, false, "" },
                new object[] { true, true, "U" },
                new object[] { false, true, "Ub2" },
                new object[] { true, true, "U /[ao] n/" },
                new object[] { false, false, "U %whatever%" },
                new object[] { false, true, "U %%" },
                new object[] { true, true, "{U %%}+{[[201702]]}" },
            };

        public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
    }
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class DbQueryTest : QueryTestBase, IDisposable
{
    private readonly IDbAdapter m_Adapter;

    public DbQueryTest()
    {
        m_Adapter = Facade.Create(db: "accounting-test");

        m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance).AsTask().Wait();
        m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance).AsTask().Wait();
    }

    public void Dispose()
    {
        m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance).AsTask().Wait();
        m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance).AsTask().Wait();
    }

    protected override async ValueTask PrepareAsset(Asset asset) => await m_Adapter.Upsert(asset);
    protected override async ValueTask PrepareAmort(Amortization amort) => await m_Adapter.Upsert(amort);

    protected override async ValueTask<bool> RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => await m_Adapter.SelectAssets(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask<bool> RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => await m_Adapter.SelectAmortizations(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask ResetAssets()
        => await m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);

    protected override async ValueTask ResetAmorts()
        => await m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expectedA, bool expectedB, string query)
        => base.RunTestA(expectedA, expectedB, query);
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class BLLQueryTest : QueryTestBase, IDisposable
{
    private readonly Accountant m_Accountant;

    public BLLQueryTest()
    {
        m_Accountant = new(new(db: "accounting-test"), "b1", DateTime.UtcNow.Date);

        m_Accountant.DeleteAssetsAsync(DistributedQueryUnconstrained.Instance).AsTask().Wait();
        m_Accountant.DeleteAmortizationsAsync(DistributedQueryUnconstrained.Instance).AsTask().Wait();
    }

    public void Dispose()
    {
        m_Accountant.DeleteAssetsAsync(DistributedQueryUnconstrained.Instance).AsTask().Wait();
        m_Accountant.DeleteAmortizationsAsync(DistributedQueryUnconstrained.Instance).AsTask().Wait();
    }

    protected override async ValueTask PrepareAsset(Asset asset) => await m_Accountant.UpsertAsync(asset);
    protected override async ValueTask PrepareAmort(Amortization amort) => await m_Accountant.UpsertAsync(amort);

    protected override async ValueTask<bool> RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => await m_Accountant.SelectAssetsAsync(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask<bool> RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => await m_Accountant.SelectAmortizationsAsync(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask ResetAssets()
        => await m_Accountant.DeleteAssetsAsync(DistributedQueryUnconstrained.Instance);

    protected override async ValueTask ResetAmorts()
        => await m_Accountant.DeleteAmortizationsAsync(DistributedQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expectedA, bool expectedB, string query)
        => base.RunTestA(expectedA, expectedB, query);
}

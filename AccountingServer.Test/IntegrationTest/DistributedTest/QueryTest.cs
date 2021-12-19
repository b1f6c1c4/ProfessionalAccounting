/* Copyright (C) 2020-2021 b1f6c1c4
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
using AccountingServer.BLL;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.DistributedTest;

public abstract class QueryTestBase
{
    protected QueryTestBase()
        => ClientUser.Set("b1");

    protected virtual void PrepareAsset(Asset asset) { }
    protected virtual void PrepareAmort(Amortization amort) { }
    protected abstract bool RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query);
    protected abstract bool RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query);
    protected virtual void ResetAssets() { }
    protected virtual void ResetAmorts() { }

    public virtual void RunTestA(bool expectedA, bool expectedB, string query)
    {
        var asset = AssetDataProvider.Create("2017-02-03", DepreciationMethod.StraightLine);
        asset.Name = "a nm";
        asset.User = "b1";
        asset.Remark = "rmk1";
        var amort = AmortDataProvider.Create("2017-04-05", AmortizeInterval.EveryDay);
        amort.Name = "o nm";
        amort.User = "b2";
        amort.Remark = null;

        ResetAssets();
        ResetAmorts();
        PrepareAsset(asset);
        PrepareAmort(amort);
        Assert.Equal(expectedA, RunAssetQuery(ParsingF.DistributedQuery(query)));
        Assert.Equal(expectedB, RunAmortQuery(ParsingF.DistributedQuery(query)));
        ResetAssets();
        ResetAmorts();
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

        m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);
        m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
    }

    public void Dispose()
    {
        m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);
        m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
    }

    protected override void PrepareAsset(Asset asset) => m_Adapter.Upsert(asset);
    protected override void PrepareAmort(Amortization amort) => m_Adapter.Upsert(amort);

    protected override bool RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => m_Adapter.SelectAssets(query).SingleOrDefault() != null;

    protected override bool RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => m_Adapter.SelectAmortizations(query).SingleOrDefault() != null;

    protected override void ResetAssets() => m_Adapter.DeleteAssets(DistributedQueryUnconstrained.Instance);
    protected override void ResetAmorts() => m_Adapter.DeleteAmortizations(DistributedQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override void RunTestA(bool expectedA, bool expectedB, string query)
        => base.RunTestA(expectedA, expectedB, query);
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class BLLQueryTest : QueryTestBase, IDisposable
{
    private readonly Accountant m_Accountant;

    public BLLQueryTest()
    {
        m_Accountant = new(db: "accounting-test");

        m_Accountant.DeleteAssets(DistributedQueryUnconstrained.Instance);
        m_Accountant.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
    }

    public void Dispose()
    {
        m_Accountant.DeleteAssets(DistributedQueryUnconstrained.Instance);
        m_Accountant.DeleteAmortizations(DistributedQueryUnconstrained.Instance);
    }

    protected override void PrepareAsset(Asset asset) => m_Accountant.Upsert(asset);
    protected override void PrepareAmort(Amortization amort) => m_Accountant.Upsert(amort);

    protected override bool RunAssetQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => m_Accountant.SelectAssets(query).SingleOrDefault() != null;
    protected override bool RunAmortQuery(IQueryCompounded<IDistributedQueryAtom> query)
        => m_Accountant.SelectAmortizations(query).SingleOrDefault() != null;

    protected override void ResetAssets() => m_Accountant.DeleteAssets(DistributedQueryUnconstrained.Instance);
    protected override void ResetAmorts() => m_Accountant.DeleteAmortizations(DistributedQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override void RunTestA(bool expectedA, bool expectedB, string query)
        => base.RunTestA(expectedA, expectedB, query);
}
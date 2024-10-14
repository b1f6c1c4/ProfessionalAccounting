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

namespace AccountingServer.Test.IntegrationTest.VoucherTest;

public abstract class QueryTestBase
{
    protected abstract Client Client { get; }

    protected abstract ValueTask PrepareVoucher(Voucher voucher);
    protected abstract ValueTask<bool> RunQuery(IQueryCompounded<IVoucherQueryAtom> query);
    protected abstract ValueTask ResetVouchers();

    public virtual async Task RunTestA(bool expected, string query)
    {
        var voucher = new Voucher
            {
                ID = null,
                Date = null,
                Type = VoucherType.Uncertain,
                Remark = "rmk1",
                Details = new()
                    {
                        new()
                            {
                                User = "b1",
                                Currency = "JPY",
                                Title = 1001,
                                SubTitle = 05,
                                Content = "cont1",
                                Fund = 123.45,
                            },
                        new()
                            {
                                User = "b1",
                                Currency = "JPY",
                                Title = 1234,
                                Remark = "remk2",
                                Fund = -123.45,
                            },
                        new()
                            {
                                User = "b1",
                                Currency = "USD",
                                Title = 2345,
                                Content = "cont3",
                                Fund = -77.66,
                            },
                        new()
                            {
                                User = "b1",
                                Currency = "USD",
                                Title = 3456,
                                SubTitle = 05,
                                Content = "cont4",
                                Remark = "remk4",
                                Fund = 77.66,
                            },
                        new()
                            {
                                User = "b1&b2",
                                Currency = "eur#",
                                Title = 1111,
                                SubTitle = 22,
                                Fund = 114514,
                            },
                    },
            };

        await ResetVouchers();
        await PrepareVoucher(voucher);
        Assert.Equal(expected, await RunQuery(ParsingF.VoucherQuery(query, Client)));
        await ResetVouchers();
    }

    protected class DataProvider : IEnumerable<object[]>
    {
        private static readonly List<object[]> Data = new()
            {
                new object[] { true, "" },
                new object[] { false, "[null]%rmk%Uncertain" },
                new object[] { false, "[null]%rMk1%Uncertain" },
                new object[] { true, "[null]%rMk%.*Uncertain" },
                new object[] { false, "[null]Uncertain%rmk%" },
                new object[] { false, "[null]Uncertain%rMk1%" },
                new object[] { true, "[null]Uncertain%rMk%.*" },
                new object[] { true, "{}-{null}*{{Devalue}+{Amortization}}" },
                new object[] { true, "-{%rrr%}+{20170101}" },
                new object[] { false, "{^59278b516c2f021e80f51911^}+{G}*{%asdf%}" },
                new object[] { true, "@JPY T100105'cont1'>" },
                new object[] { true, "@JPY T123400\"remk2\"=-123.45" },
                new object[] { true, "@JPY T123400\"remk2\"=123.45" },
                new object[] { false, "@JPY T123400\"remk2\"=+123.45" },
                new object[] { true, "@USD T2345'cont3'<" },
                new object[] { true, "@USD T3456'cont4'\"remk4\"=77.66" },
                new object[] { true, "@USD T3456'cont4'\"remk4\"=+77.66" },
                new object[] { true, "{(T1234+T345605)*(''+'cont3')}-{Depreciation}-{Carry}" },
                new object[] { false, "T1002+'  '''+\" rmk\"+=77.66001" },
                new object[] { true, "(@USD+@JPY)*(=-123.45+>)+T2345+Ub1&b2@#eur# A" },
                new object[] { false, "(@USD+@JPY)*=-123.45+T2345+Ub1&b2@#eur# A" },
                new object[] { true, "{T1001+(T1234+(T2345)+T3456)+Ub1&b2@#eur# A}-{AnnualCarry}" },
                new object[] { true, "+(T1001+(T1234+T2345))+T3456+Ub1&b2@#eur# A" },
                new object[] { true, "{((<+>)*())+=1*=2+Ub1&b2@#eur# A}-{Ordinary}" },
                new object[] { true, "U-=1*=2 A" },
                new object[] { true, "-=1*=2 A" },
                new object[] { true, "{T1001 null Uncertain}*{T2345 G}-{T3456 A}" },
                new object[] { false, "{20170101~20180101}+{~~20160101}" },
                new object[] { true, "{20170101~~20180101}*{~20160101}" },
                new object[] { true, "U-" },
                new object[] { true, "-" },
                new object[] { false, "U- A" },
                new object[] { false, "- A" },
                new object[] { true, "U@#eur#\"\"" },
                new object[] { true, "U'b1&b2'" },
                new object[] { true, "Ub1+Ub1&b2 A" },
                new object[] { true, "U A" },
                new object[] { true, "@JPY Asset" },
                new object[] { true, "Liability cont3" },
                new object[] { true, "U @USD - U Asset - U Equity - U Revenue - U Expense" },
                new object[] { false, "%%" },
                new object[] { true, "U 'ConT'.**U\"rEMk\".*" },
                new object[] { false, "U 'cont\\'.*+U\"remk#\".*" },
                new object[] { true, "Ub1&b2 @#@" },
                new object[] { false, "Ub1&b2 @##@" },
            };

        public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
    }
}

[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class MatchTest : QueryTestBase
{
    private Voucher m_Voucher;
    protected override Client Client { get; } = new() { User = "b1", Today = DateTime.UtcNow.Date };

    protected override ValueTask PrepareVoucher(Voucher voucher)
    {
        m_Voucher = voucher;
        return new();
    }

    protected override ValueTask<bool> RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
        => ValueTask.FromResult(MatchHelper.IsMatch(m_Voucher, query));

    protected override ValueTask ResetVouchers()
    {
        m_Voucher = null;
        return new();
    }

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expected, string query) => base.RunTestA(expected, query);
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class DbQueryTest : QueryTestBase, IDisposable
{
    private readonly IDbAdapter m_Adapter;

    public DbQueryTest()
    {
        m_Adapter = Facade.Create(db: "accounting-test");

        m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance).AsTask().Wait();
    }

    protected override Client Client { get; } = new() { User = "b1", Today = DateTime.UtcNow.Date };

    public void Dispose() => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance).AsTask().Wait();

    protected override async ValueTask PrepareVoucher(Voucher voucher) => await m_Adapter.Upsert(voucher);

    protected override async ValueTask<bool> RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
        => await m_Adapter.SelectVouchers(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask ResetVouchers()
        => await m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expected, string query) => base.RunTestA(expected, query);
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class BLLQueryTest : QueryTestBase, IDisposable
{
    private readonly Accountant m_Accountant;

    public BLLQueryTest()
    {
        m_Accountant = new(new(db: "accounting-test"), "b1", DateTime.UtcNow.Date);

        m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance).AsTask().Wait();
    }

    protected override Client Client => m_Accountant.Client;

    public void Dispose() => m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance).AsTask().Wait();

    protected override async ValueTask PrepareVoucher(Voucher voucher) => await m_Accountant.UpsertAsync(voucher);

    protected override async ValueTask<bool> RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
        => await m_Accountant.SelectVouchersAsync(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask ResetVouchers()
        => await m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expected, string query) => base.RunTestA(expected, query);
}

[Collection("DbTestCollection")]
[SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
public class VirtualizedQueryTest : QueryTestBase, IAsyncDisposable
{
    private readonly Accountant m_Accountant;
    private readonly Accountant.VirtualizeLock m_Lock;

    public VirtualizedQueryTest()
    {
        m_Accountant = new(new(db: "accounting-test"), "b1", DateTime.UtcNow.Date);

        m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance).AsTask().Wait();
        m_Lock = m_Accountant.Virtualize();
    }

    protected override Client Client => m_Accountant.Client;

    public async ValueTask DisposeAsync()
    {
        await m_Lock.DisposeAsync();
        await m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance);
    }

    protected override async ValueTask PrepareVoucher(Voucher voucher) => await m_Accountant.UpsertAsync(voucher);

    protected override async ValueTask<bool> RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
        => await m_Accountant.SelectVouchersAsync(query).SingleOrDefaultAsync() != null;

    protected override async ValueTask ResetVouchers()
        => await m_Accountant.DeleteVouchersAsync(VoucherQueryUnconstrained.Instance);

    [Theory]
    [ClassData(typeof(DataProvider))]
    public override Task RunTestA(bool expected, string query) => base.RunTestA(expected, query);
}

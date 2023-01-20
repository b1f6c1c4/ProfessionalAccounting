/* Copyright (C) 2020-2023 b1f6c1c4
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
using System.Linq;
using System.Threading.Tasks;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.VoucherTest;

[Collection("DbTestCollection")]
public class VirtualizerTest
{
    private readonly DbSession m_Db = new(db: "accounting-test");

    [Fact]
    public async Task ZeroTest()
    {
        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using var vir = ac.Virtualize();
        Assert.Equal(0, vir.CachedVouchers);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task CreateTest(bool multi, bool abort)
    {
        await m_Db.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using (var vir = ac.Virtualize())
        {
            var v = new Voucher { Remark = "r1" };
            if (multi)
                Assert.Equal(1, await ac.UpsertAsync(new[] { v }));
            else
                Assert.True(await ac.UpsertAsync(v));
            v.Remark = "r2";
            Assert.Null(v.ID);
            Assert.Equal(1, vir.CachedVouchers);
            Assert.Equal(0, await m_Db.SelectVouchers(VoucherQueryUnconstrained.Instance).CountAsync());
            var res = await ac.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).ToArrayAsync();
            Assert.Single(res);
            Assert.Equal("r1", res[0].Remark);
            if (abort)
            {
                await vir.Abort();
                Assert.Throws<InvalidOperationException>(() => vir.CachedVouchers);
            }
        }

        {
            var res = await m_Db.SelectVouchers(VoucherQueryUnconstrained.Instance).ToArrayAsync();
            if (abort)
                Assert.Empty(res);
            else
            {
                Assert.Single(res);
                Assert.Equal("r1", res[0].Remark);
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ModifyTest(bool delete, bool abort)
    {
        var client = new Client { User = "b1", Today = DateTime.UtcNow.Date };

        await m_Db.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        const string id = "6330fe8e3305507513620c44";
        var voucher = new Voucher
            {
                ID = id, Remark = "r1", Details = new() { new() { User = "b1", Currency = "XXX", Fund = 1 } }
            };
        await m_Db.Upsert(voucher);

        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using (var vir = ac.Virtualize())
        {
            voucher.Remark = "r2";
            voucher.Details[0].Fund = 2;
            Assert.True(await ac.UpsertAsync(voucher));
            Assert.Equal(1, await ac.UpsertAsync(new[] { voucher }));
            voucher.Remark = "r3";
            voucher.Details[0].Fund = 3;
            Assert.Equal(1, vir.CachedVouchers);
            var res = await ac.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).ToArrayAsync();
            Assert.Single(res);
            Assert.Equal("r2", res[0].Remark);
            Assert.Equal("r2", (await ac.SelectVoucherAsync(id)).Remark);
            Assert.Equal(2,
                (await ac.SelectVoucherDetailsAsync(ParsingF.DetailQuery("A:", client)).SingleAsync()).Fund);
            Assert.False(await ac.DeleteVoucherAsync("6330fe8e3305507513620c45"));
            if (delete)
            {
                Assert.True(await ac.DeleteVoucherAsync(id));
                Assert.Equal(1, vir.CachedVouchers);
                Assert.Equal(0, await ac.SelectVouchersAsync(VoucherQueryUnconstrained.Instance).CountAsync());
                Assert.Null(await ac.SelectVoucherAsync(id));
            }

            Assert.Equal(1, await m_Db.SelectVouchers(VoucherQueryUnconstrained.Instance).CountAsync());
            Assert.Equal("r1", (await m_Db.SelectVoucher(id))!.Remark);
            if (abort)
            {
                await vir.Abort();
                Assert.Throws<InvalidOperationException>(() => vir.CachedVouchers);
            }
        }

        if (delete && !abort)
            Assert.Equal(0, await m_Db.SelectVouchers(VoucherQueryUnconstrained.Instance).CountAsync());
        else
        {
            var res = await m_Db.SelectVouchers(VoucherQueryUnconstrained.Instance).ToArrayAsync();
            Assert.Single(res);
            Assert.Equal(abort ? "r1" : "r2", res[0].Remark);
            Assert.Equal(abort ? 1 : 2,
                (await m_Db.SelectVoucherDetails(ParsingF.DetailQuery("A:", client)).SingleAsync()).Fund);
        }
    }

    [Fact]
    public async Task VoucherGroupTest()
    {
        var client = new Client { User = "b1", Today = DateTime.UtcNow.Date };
        await m_Db.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        await m_Db.Upsert(new[]
            {
                new Voucher { Date = "2023-01-01".ToDateTime() },
                new Voucher { Date = "2023-02-01".ToDateTime() },
                new Voucher { Date = "2023-02-02".ToDateTime() },
            });

        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using var vir = ac.Virtualize();

        {
            var res = (await ac.SelectVouchersGroupedAsync(ParsingF.VoucherGroupedQuery("A !!m", client))).Items
                .OrderBy(static grp => (grp as ISubtotalDate)!.Date).ToList();
            Assert.Equal(2, res.Count);
            Assert.Equal(1, res[0].Fund);
            Assert.Equal(2, res[1].Fund);
        }
        await ac.UpsertAsync(new[]
            {
                new Voucher { Date = "2023-02-03".ToDateTime() },
                new Voucher { Date = "2023-03-01".ToDateTime() },
                new Voucher { Date = "2023-01-02".ToDateTime() },
            });
        {
            var res = (await ac.SelectVouchersGroupedAsync(ParsingF.VoucherGroupedQuery("A !!m", client))).Items
                .OrderBy(static grp => (grp as ISubtotalDate)!.Date).ToList();
            Assert.Equal(3, res.Count);
            Assert.Equal(2, res[0].Fund);
            Assert.Equal(3, res[1].Fund);
            Assert.Equal(1, res[2].Fund);
        }
    }

    [Fact]
    public async Task DetailGroupTest()
    {
        var client = new Client { User = "b1", Today = DateTime.UtcNow.Date };
        await m_Db.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        var voucher = new Voucher
            {
                Date = "2023-01-01".ToDateTime(),
                Details = new()
                    {
                        new()
                            {
                                User = "b1",
                                Currency = "CNY",
                                Title = 1234,
                                SubTitle = 56,
                                Content = "cnt1",
                                Fund = 123.45,
                                Remark = "rmk1",
                            },
                        new()
                            {
                                User = "b1",
                                Currency = "JPY",
                                Title = 6541,
                                SubTitle = 98,
                                Content = "cnt1",
                                Fund = -123.45,
                                Remark = "rmk2",
                            },
                    },
            };
        await m_Db.Upsert(voucher);

        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using var vir = ac.Virtualize();
        voucher.Date = "2023-02-01".ToDateTime();
        await ac.UpsertAsync(voucher);
        {
            var res = (await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A !m", client))).Items
                .OrderBy(static grp => (grp as ISubtotalDate)!.Date).ToList();
            Assert.Equal(2, res.Count);
            Assert.Equal(2, res[0].Fund);
            Assert.Equal(2, res[1].Fund);
        }
        {
            var res = (await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A ``m", client))).Items
                .OrderBy(static grp => (grp as ISubtotalDate)!.Date).ToList();
            Assert.Equal(2, res.Count);
            Assert.Equal(0, res[0].Fund);
            Assert.Equal(0, res[1].Fund);
        }
        {
            Assert.Empty((await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A `m", client))).Items);
        }
        {
            var res = (await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A !vM", client))).Items
                .OrderBy(static grp => (grp as ISubtotalDate)!.Date).ToList();
            Assert.Equal(2, res.Count);
            Assert.Equal(2, res[0].Fund);
            Assert.Equal(4, res[1].Fund);
        }
        {
            var res = (await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A !U", client))).Items
                .OrderBy(static grp => (grp as ISubtotalUser)!.User).ToList();
            Assert.Single(res);
            Assert.Equal(4, res[0].Fund);
        }
        {
            var res = (await ac.SelectVoucherDetailsGroupedAsync(ParsingF.GroupedQuery("A !C", client))).Items
                .OrderBy(static grp => (grp as ISubtotalCurrency)!.Currency).ToList();
            Assert.Equal(2, res.Count);
            Assert.Equal(2, res[0].Fund);
            Assert.Equal(2, res[1].Fund);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task DeleteTest(bool multi, bool abort)
    {
        var client = new Client { User = "b1", Today = DateTime.UtcNow.Date };
        await m_Db.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        const string id = "6330fe8e3305507513620c44";
        var voucher = new Voucher { ID = id };
        await m_Db.Upsert(voucher);
        var ac = new Accountant(m_Db, "b1", DateTime.UtcNow.Date);
        await using (var vir = ac.Virtualize())
        {
            if (multi)
                Assert.Equal(0,
                    await ac.DeleteVouchersAsync(ParsingF.VoucherQuery("A ^6330fe8e3305507513620c45^", client)));
            else
                Assert.False(await ac.DeleteVoucherAsync("6330fe8e3305507513620c45"));
            Assert.Equal(0, vir.CachedVouchers);
            if (multi)
                Assert.Equal(1, await ac.DeleteVouchersAsync(ParsingF.VoucherQuery($"A {id.Quotation('^')}", client)));
            else
                Assert.True(await ac.DeleteVoucherAsync(id));
            Assert.Equal(1, vir.CachedVouchers);
            Assert.NotNull(await m_Db.SelectVoucher(id));
            if (abort)
            {
                await vir.Abort();
                Assert.Throws<InvalidOperationException>(() => vir.CachedVouchers);
            }
        }

        if (abort)
            Assert.NotNull(await m_Db.SelectVoucher(id));
        else
            Assert.Null(await m_Db.SelectVoucher(id));
    }
}

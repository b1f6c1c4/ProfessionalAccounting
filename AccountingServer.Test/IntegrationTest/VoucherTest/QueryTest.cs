/* Copyright (C) 2020 b1f6c1c4
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
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest.VoucherTest
{
    public abstract class QueryTestBase
    {
        protected QueryTestBase()
            => ClientUser.Set("b1");

        protected virtual void PrepareVoucher(Voucher voucher) { }
        protected abstract bool RunQuery(IQueryCompounded<IVoucherQueryAtom> query);
        protected virtual void ResetVouchers() { }

        public virtual void RunTestA(bool expected, string query)
        {
            var voucher = new Voucher
                {
                    ID = "59278b516c2f021e80f51912",
                    Date = null,
                    Type = VoucherType.Uncertain,
                    Remark = "rmk1",
                    Details = new List<VoucherDetail>
                        {
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "JPY",
                                    Title = 1001,
                                    SubTitle = 05,
                                    Content = "cont1",
                                    Fund = 123.45,
                                },
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "JPY",
                                    Title = 1234,
                                    Remark = "remk2",
                                    Fund = -123.45,
                                },
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 2345,
                                    Content = "cont3",
                                    Fund = -77.66,
                                },
                            new VoucherDetail
                                {
                                    User = "b1",
                                    Currency = "USD",
                                    Title = 3456,
                                    SubTitle = 05,
                                    Content = "cont4",
                                    Remark = "remk4",
                                    Fund = 77.66,
                                },
                            new VoucherDetail
                                {
                                    User = "b1&b2",
                                    Currency = "EUR",
                                    Title = 1111,
                                    SubTitle = 22,
                                    Fund = 114514,
                                },
                        },
                };

            ResetVouchers();
            PrepareVoucher(voucher);
            Assert.Equal(expected, RunQuery(ParsingF.VoucherQuery(query)));
            ResetVouchers();
        }

        protected class DataProvider : IEnumerable<object[]>
        {
            private static readonly List<object[]> Data = new List<object[]>
                {
                    new object[] { true, "" },
                    new object[] { true, "^59278b516c2f021e80f51912^[null]Uncertain%rmk%" },
                    new object[] { true, "{^59278b516c2f021e80f51912^}-{null}*{Devalue}" },
                    new object[] { true, "-{%rrr%}+{20170101}" },
                    new object[] { false, "{^59278b516c2f021e80f51911^}+{G}*{%asdf%}" },
                    new object[] { true, "@JPY T100105'cont1'>" },
                    new object[] { true, "@JPY T123400\"remk2\"=-123.45" },
                    new object[] { true, "@USD T2345'cont3'<" },
                    new object[] { true, "@USD T3456'cont4'\"remk4\"=77.66" },
                    new object[] { true, "{(T1234+T345605)*(''+'cont3')}-{Depreciation}-{Carry}" },
                    new object[] { false, "T1002+'  '''+\" rmk\"+=77.66001" },
                    new object[] { true, "(@USD+@JPY)*(=-123.45+>)+T2345+Ub1&b2@EUR A" },
                    new object[] { false, "(@USD+@JPY)*=-123.45+T2345+Ub1&b2@EUR A" },
                    new object[] { true, "{T1001+(T1234+(T2345)+T3456)+Ub1&b2@EUR A}-{AnnualCarry}" },
                    new object[] { true, "+(T1001+(T1234+T2345))+T3456+Ub1&b2@EUR A" },
                    new object[] { true, "{((<+>)*())+=1*=2+Ub1&b2@EUR A}-{Ordinary}" },
                    new object[] { true, "U-=1*=2 A" },
                    new object[] { true, "-=1*=2 A" },
                    new object[] { true, "{T1001 null Uncertain}*{T2345 G}-{T3456 A}" },
                    new object[] { false, "{20170101~20180101}+{~~20160101}" },
                    new object[] { true, "{20170101~~20180101}*{~20160101}" },
                    new object[] { true, "U-" },
                    new object[] { true, "-" },
                    new object[] { false, "U- A" },
                    new object[] { false, "- A" },
                    new object[] { true, "U@EUR" },
                    new object[] { true, "U'b1&b2'" },
                    new object[] { true, "Ub1+Ub1&b2 A" },
                    new object[] { true, "U A" },
                };

            public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();
        }
    }

    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class MatchTest : QueryTestBase
    {
        private Voucher m_Voucher;
        protected override void PrepareVoucher(Voucher voucher) => m_Voucher = voucher;

        protected override bool RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
            => MatchHelper.IsMatch(m_Voucher, query);

        protected override void ResetVouchers() => m_Voucher = null;

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void RunTestA(bool expected, string query) => base.RunTestA(expected, query);
    }

    [Collection("DbTestCollection")]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class DbQueryTest : QueryTestBase, IDisposable
    {
        private readonly IDbAdapter m_Adapter;

        public DbQueryTest()
        {
            m_Adapter = Facade.Create(db: "accounting-test");

            m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);
        }

        public void Dispose() => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

        protected override void PrepareVoucher(Voucher voucher) => m_Adapter.Upsert(voucher);

        protected override bool RunQuery(IQueryCompounded<IVoucherQueryAtom> query)
            => m_Adapter.SelectVouchers(query).SingleOrDefault() != null;

        protected override void ResetVouchers() => m_Adapter.DeleteVouchers(VoucherQueryUnconstrained.Instance);

        [Theory]
        [ClassData(typeof(DataProvider))]
        public override void RunTestA(bool expected, string query) => base.RunTestA(expected, query);
    }
}

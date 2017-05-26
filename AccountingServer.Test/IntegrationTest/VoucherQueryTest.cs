using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using Xunit;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Test.IntegrationTest
{
    public abstract class VoucherQueryTestBase
    {
        protected virtual void PrepareVoucher(Voucher voucher) { }
        protected abstract bool RunQuery(IQueryCompunded<IVoucherQueryAtom> query);
        protected virtual void ResetVouchers() { }

        [InlineData(true, "")]
        [InlineData(true, "^59278b516c2f021e80f51912^[null]Uncertain%rmk%")]
        [InlineData(true, "{^59278b516c2f021e80f51912^}-{null}*{Devalue}")]
        [InlineData(true, "-{%rrr%}+{20170101}")]
        [InlineData(false, "{^59278b516c2f021e80f51911^}+{G}*{%asdf%}")]
        [InlineData(true, "@JPY T100105'cont1'>")]
        [InlineData(true, "@JPY T123400\"remk2\"=-123.45")]
        [InlineData(true, "@USD T2345'cont3'<")]
        [InlineData(true, "@USD T3456'cont4'\"remk4\"=77.66")]
        [InlineData(true, "(T1234+T345605)*(''+'cont3')")]
        [InlineData(false, "T1002+'  '''+\" rmk\"+=77.66001")]
        [InlineData(true, "(@USD+@JPY)*(=-123.45+>)+T2345 A")]
        [InlineData(false, "(@USD+@JPY)*=-123.45+T2345 A")]
        [InlineData(true, "T1001+(T1234+(T2345)+T3456) A")]
        [InlineData(true, "+(T1001+(T1234+T2345))+T3456 A")]
        [InlineData(true, "((<+>)*())+=1*=2 A")]
        [InlineData(true, "-=1*=2 A")]
        [InlineData(true, "{T1001 null}*{T2345 G}-{T3456 A}")]
        [InlineData(false, "{20170101~20180101}+{~~20160101}")]
        [InlineData(true, "{20170101~~20180101}*{~20160101}")]
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
                                    Currency = "JPY",
                                    Title = 1001,
                                    SubTitle = 05,
                                    Content = "cont1",
                                    Fund = 123.45
                                },
                            new VoucherDetail
                                {
                                    Currency = "JPY",
                                    Title = 1234,
                                    Remark = "remk2",
                                    Fund = -123.45
                                },
                            new VoucherDetail
                                {
                                    Currency = "USD",
                                    Title = 2345,
                                    Content = "cont3",
                                    Fund = -77.66
                                },
                            new VoucherDetail
                                {
                                    Currency = "USD",
                                    Title = 3456,
                                    SubTitle = 05,
                                    Content = "cont4",
                                    Remark = "remk4",
                                    Fund = 77.66
                                }
                        }
                };

            ResetVouchers();
            PrepareVoucher(voucher);
            Assert.Equal(expected, RunQuery(ParsingF.VoucherQuery(query)));
            ResetVouchers();
        }
    }

    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class VoucherMatchTest : VoucherQueryTestBase
    {
        private Voucher m_Voucher;
        protected override void PrepareVoucher(Voucher voucher) => m_Voucher = voucher;

        protected override bool RunQuery(IQueryCompunded<IVoucherQueryAtom> query) => MatchHelper.IsMatch(
            m_Voucher,
            query);

        protected override void ResetVouchers() => m_Voucher = null;

        [Theory]
        public override void RunTestA(bool expected, string query) { base.RunTestA(expected, query); }
    }

    [Collection("DbTestCollection")]
    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    public class VoucherDbTest : VoucherQueryTestBase,IDisposable
    {
        private readonly IDbAdapter m_Adapter;

        public VoucherDbTest()
        {
            m_Adapter = Facade.Create("mongodb://localhost/accounting-test");

            m_Adapter.DeleteVouchers(null);
        }

        public void Dispose() => m_Adapter.DeleteVouchers(null);

        protected override void PrepareVoucher(Voucher voucher) => m_Adapter.Upsert(voucher);

        protected override bool RunQuery(IQueryCompunded<IVoucherQueryAtom> query) => m_Adapter.SelectVouchers(query)
            .SingleOrDefault() != null;

        protected override void ResetVouchers() => m_Adapter.DeleteVouchers(null);

        [Theory]
        public override void RunTestA(bool expected, string query) { base.RunTestA(expected, query); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.YieldRate
{
    /// <summary>
    ///     实际收益率计算
    /// </summary>
    [Plugin(Alias = "yr")]
    public class YieldRate : PluginBase
    {
        public YieldRate(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            var endDate = DateTime.Now.Date;
            if (pars.Any())
                endDate = pars[0].AsDate() ?? endDate;
            var rng = new DateFilter(null, endDate);

            // {T1101}-{T1101+T611102+T610101 A} : T1101``cd
            var query1 = new VoucherQueryAtomBase(filter: new VoucherDetail { Title = 1101 }, rng: rng);
            var query2 =
                new VoucherQueryAtomBase(
                    filters:
                        new[]
                            {
                                new VoucherDetail { Title = 1101 }, new VoucherDetail { Title = 6101, SubTitle = 01 },
                                new VoucherDetail { Title = 6111, SubTitle = 02 }
                            },
                    forAll: true);
            var voucherQuery = new VoucherQueryAryBase(
                OperatorType.Substract,
                new IQueryCompunded<IVoucherQueryAtom>[] { query1, query2 });
            var emitFilter = new EmitBase { DetailFilter = new DetailQueryAtomBase(new VoucherDetail { Title = 1101 }) };
            var result =
                Accountant.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase
                                                           {
                                                               VoucherEmitQuery =
                                                                   new VoucherDetailQueryBase
                                                                       {
                                                                           VoucherQuery = voucherQuery,
                                                                           DetailEmitFilter = emitFilter
                                                                       },
                                                               Subtotal =
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new[]
                                                                                        {
                                                                                            SubtotalLevel.Content,
                                                                                            SubtotalLevel.Day
                                                                                        }
                                                                       }
                                                           });
            var sb = new StringBuilder();
            foreach (var grp in result.GroupByContent())
            {
                var rate = GetRate(grp.ToList(), grp.Key, endDate);
                sb.AppendFormat("{0}\t{1:P2}", grp.Key, rate * 360);
                sb.AppendLine();
            }
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     计算实际收益率
        /// </summary>
        /// <param name="lst">现金流</param>
        /// <param name="content">内容</param>
        /// <param name="endDate">最末日期</param>
        /// <returns>实际收益率</returns>
        private double GetRate(IReadOnlyList<Balance> lst, string content, DateTime endDate)
        {
            var rng = new DateFilter(null, endDate);
            var query1 =
                new VoucherQueryAtomBase(filter: new VoucherDetail { Title = 1101, Content = content }, rng: rng);
            var pv =
                Accountant.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase
                                                           {
                                                               VoucherEmitQuery =
                                                                   new VoucherDetailQueryBase
                                                                       {
                                                                           VoucherQuery = query1
                                                                       },
                                                               Subtotal =
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new SubtotalLevel[] { }
                                                                       }
                                                           }).Single().Fund;
            var days = new double[lst.Count + 1];
            var fund = new double[lst.Count + 1];
            for (var i = 0; i < lst.Count; i++)
            {
                var dt = lst[i].Date;
                if (dt == null)
                    throw new ApplicationException("无法处理无穷长时间以前的交易性金融资产");
                days[i] = endDate.Subtract(dt.Value).TotalDays;
                fund[i] = lst[i].Fund;
            }
            days[lst.Count] = 0;
            fund[lst.Count] = -pv;
            var solver = new YieldRateSolver(days, fund);
            var rate = solver.Solve();
            return rate;
        }
    }
}

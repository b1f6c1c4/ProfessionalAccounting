using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Console;
using AccountingServer.Console.Plugin;
using AccountingServer.Entities;

namespace AccountingServer.Plugins.Interest
{
    /// <summary>
    ///     自动计算利息收入和还款
    /// </summary>
    [Plugin(Alias = "ir")]
    public class InterestRevenue : PluginBase
    {
        public InterestRevenue(Accountant accountant) : base(accountant) { }

        /// <summary>
        ///     执行插件表达式
        /// </summary>
        /// <param name="pars">
        ///     参数列表，总共三个参数
        ///     <list type="bullet">
        ///         <item>
        ///             <description>第一个参数为借款人；</description>
        ///         </item>
        ///         <item>
        ///             <description>第二个参数为借款代码（模糊匹配）；</description>
        ///         </item>
        ///         <item>
        ///             <description>第三个参数为利率；</description>
        ///         </item>
        ///         <item>
        ///             <description>第四个参数可选，若没有表示只计算上次计息至今的部分。</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <returns>执行结果</returns>
        public override IQueryResult Execute(params string[] pars)
        {
            if (pars.Length > 4 ||
                pars.Length < 3)
                throw new ArgumentException();

            var loans =
                Accountant.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase(
                                                           filter: new VoucherDetail { Title = 1221, Content = pars[0] },
                                                           subtotal: new SubtotalBase
                                                                         {
                                                                             GatherType = GatheringType.Zero,
                                                                             Levels = new[] { SubtotalLevel.Remark }
                                                                         })).ToList();
            var rmk =
                loans.Single(
                             b => b.Remark != null &&
                                  b.Remark.StartsWith(pars[1], StringComparison.InvariantCultureIgnoreCase) &&
                                  !b.Remark.EndsWith("-利息"))
                     .Remark;

            if (pars.Length == 3)
            {
                var filter = new VoucherDetail { Title = 1221, Content = pars[0], Remark = rmk };
                var filter0 = new VoucherDetail { Title = 1221, Content = pars[0], Remark = rmk + "-利息" };
                // ReSharper disable once PossibleInvalidOperationException
                var lastD = Accountant.SelectVouchers(new VoucherQueryAtomBase(filter: filter0, dir: 1))
                                      .OrderByDescending(v => v.Date, new DateComparer())
                                      .First().Date.Value;
                var rng = new DateFilter(null, lastD);
                var capQuery = new VoucherDetailQueryBase
                                   {
                                       DetailEmitFilter = new EmitBase(),
                                       VoucherQuery = new VoucherQueryAtomBase(filter: filter, rng: rng)
                                   };
                var intQuery = new VoucherDetailQueryBase
                                   {
                                       DetailEmitFilter = new EmitBase(),
                                       VoucherQuery = new VoucherQueryAtomBase(filter: filter0, rng: rng)
                                   };
                var subtotal = new SubtotalBase
                                   {
                                       Levels = new SubtotalLevel[] { },
                                       GatherType = GatheringType.Zero
                                   };
                var capitalIntegral =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase
                                                               {
                                                                   VoucherEmitQuery = capQuery,
                                                                   Subtotal = subtotal
                                                               }).Single().Fund;
                var interestIntegral =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase
                                                               {
                                                                   VoucherEmitQuery = intQuery,
                                                                   Subtotal = subtotal
                                                               }).Single().Fund;
                Regularize(
                           pars[0],
                           rmk,
                           Convert.ToDouble(pars[2]) / 10000D,
                           ref capitalIntegral,
                           ref interestIntegral,
                           lastD);
            }
            else
            {
                var capitalIntegral = 0D;
                var interestIntegral = 0D;
                Regularize(
                           pars[0],
                           rmk,
                           Convert.ToDouble(pars[2]) / 10000D,
                           ref capitalIntegral,
                           ref interestIntegral,
                           null);
            }
            return new Suceed();
        }

        /// <summary>
        ///     从上次计息日后一日起计算单利利息并整理还款
        /// </summary>
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="rate">利率</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="interestIntegral">剩余利息</param>
        /// <param name="lastSettlementDate">上次计息日</param>
        private void Regularize(string content, string rmk, double rate, ref double capitalIntegral,
                                ref double interestIntegral, DateTime? lastSettlementDate)
        {
            var capitalPattern = new VoucherDetail
                                     {
                                         Title = 1221,
                                         Content = content,
                                         Remark = rmk
                                     };
            var interestPattern = new VoucherDetail
                                      {
                                          Title = 1221,
                                          Content = content,
                                          Remark = rmk + "-利息"
                                      };
            var rng = lastSettlementDate.HasValue
                          ? new DateFilter(lastSettlementDate.Value.AddDays(1), null)
                          : DateFilter.Unconstrained;
            foreach (var grp in Accountant.SelectVouchers(
                                                          new VoucherQueryAtomBase(
                                                              filters: new[]
                                                                           {
                                                                               capitalPattern,
                                                                               interestPattern
                                                                           },
                                                              rng: rng))
                                          .GroupBy(v => v.Date).OrderBy(grp => grp.Key, new DateComparer()))
            {
                if (!grp.Key.HasValue)
                    throw new NullReferenceException();
                if (!lastSettlementDate.HasValue)
                    lastSettlementDate = grp.Key;

                // Settle Interest
                interestIntegral += SettleInterest(
                                                   content,
                                                   rmk,
                                                   rate,
                                                   capitalIntegral,
                                                   grp.Key.Value.Subtract(lastSettlementDate.Value).Days,
                                                   grp.SingleOrDefault(
                                                                       v =>
                                                                       v.Details.Any(d => d.IsMatch(interestPattern, 1)))
                                                   ?? new Voucher { Date = grp.Key, Details = new VoucherDetail[0] });
                lastSettlementDate = grp.Key;

                // Settle Loan
                // ReSharper disable once PossibleInvalidOperationException
                capitalIntegral +=
                    grp.SelectMany(v => v.Details.Where(d => d.IsMatch(capitalPattern, 1)))
                       .Select(d => d.Fund.Value)
                       .Sum();

                // Settle Return
                foreach (
                    var voucher in
                        grp.Where(
                                  v =>
                                  v.Details.Any(d => d.IsMatch(capitalPattern, -1) || d.IsMatch(interestPattern, -1)))
                           .OrderBy(v => v.ID))
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var value =
                        - voucher.Details.Where(d => d.IsMatch(capitalPattern, -1) || d.IsMatch(interestPattern, -1))
                                 .Select(d => d.Fund.Value)
                                 .Sum();
                    if (-value + interestIntegral > -Accountant.Tolerance)
                    {
                        RegularizeVoucherDetail(content, rmk, voucher, 0, interestIntegral);
                        interestIntegral -= value;
                    }
                    else
                    {
                        RegularizeVoucherDetail(content, rmk, voucher, value - interestIntegral, interestIntegral);
                        capitalIntegral -= value - interestIntegral;
                        interestIntegral = 0;
                    }
                }
            }
            if (lastSettlementDate == null)
                throw new NullReferenceException();

            var today = DateTime.Now.Date;
            if (lastSettlementDate != today)
                interestIntegral += SettleInterest(
                                                   content,
                                                   rmk,
                                                   rate,
                                                   capitalIntegral,
                                                   today.Subtract(lastSettlementDate.Value).Days,
                                                   new Voucher { Date = today, Details = new VoucherDetail[0] });
        }

        /// <summary>
        ///     计算利息
        /// </summary>
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="rate">利率</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="delta">间隔日数</param>
        /// <param name="voucher">记账凭证</param>
        /// <returns>利息</returns>
        private double SettleInterest(string content, string rmk, double rate,
                                      double capitalIntegral, int delta, Voucher voucher)
        {
            var interestPattern = new VoucherDetail
                                      {
                                          Title = 1221,
                                          Content = content,
                                          Remark = rmk + "-利息"
                                      };
            var revenuePattern = new VoucherDetail
                                     {
                                         Title = 6603,
                                         SubTitle = 02,
                                         Content = "贷款利息",
                                     };
            var interest = delta * rate * capitalIntegral;
            var create = new[]
                             {
                                 new VoucherDetail
                                     {
                                         Title = 1221,
                                         Content = content,
                                         Remark = rmk + "-利息",
                                         Fund = interest
                                     },
                                 new VoucherDetail
                                     {
                                         Title = 6603,
                                         SubTitle = 02,
                                         Content = "贷款利息",
                                         Fund = -interest
                                     }
                             };

            // ReSharper disable once PossibleInvalidOperationException
            var detail = voucher.Details.SingleOrDefault(d => d.IsMatch(interestPattern));
            if (detail == null)
            {
                voucher.Details = create;
                Accountant.Upsert(voucher);
            }
                // ReSharper disable once PossibleInvalidOperationException
            else if (!Accountant.IsZero(detail.Fund.Value - interest))
            {
                if (!voucher.Details.All(d => d.IsMatch(interestPattern) || d.IsMatch(revenuePattern)))
                    throw new InvalidOperationException();
                voucher.Details = create;
                Accountant.Upsert(voucher);
            }
            return interest;
        }

        /// <summary>
        ///     正确登记还款
        /// </summary>
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="voucher">记账凭证</param>
        /// <param name="capVol">本金还款额</param>
        /// <param name="intVol">利息还款额</param>
        private void RegularizeVoucherDetail(string content, string rmk, Voucher voucher, double capVol, double intVol)
        {
            var capitalPattern = new VoucherDetail
                                     {
                                         Title = 1221,
                                         Content = content,
                                         Remark = rmk
                                     };
            var interestPattern = new VoucherDetail
                                      {
                                          Title = 1221,
                                          Content = content,
                                          Remark = rmk + "-利息"
                                      };
            var flag = false;
            var capFlag = false;
            var intFlag = false;
            for (var i = 0; i < voucher.Details.Count; i++)
            {
                if (voucher.Details[i].IsMatch(capitalPattern, -1))
                {
                    if (capFlag || Accountant.IsZero(capVol))
                    {
                        voucher.Details.RemoveAt(i);
                        flag = true;
                        i--;
                        continue;
                    }
                    // ReSharper disable once PossibleInvalidOperationException
                    if (!Accountant.IsZero(voucher.Details[i].Fund.Value - capVol))
                    {
                        voucher.Details[i].Fund = -capVol;
                        flag = true;
                    }
                    capFlag = true;
                }
                if (voucher.Details[i].IsMatch(interestPattern, -1))
                {
                    if (intFlag || Accountant.IsZero(intVol))
                    {
                        voucher.Details.RemoveAt(i);
                        flag = true;
                        i--;
                        continue;
                    }
                    // ReSharper disable once PossibleInvalidOperationException
                    if (!Accountant.IsZero(voucher.Details[i].Fund.Value - intVol))
                    {
                        voucher.Details[i].Fund = -intVol;
                        flag = true;
                    }
                    intFlag = true;
                }
            }
            if (!capFlag &&
                !Accountant.IsZero(capVol))
            {
                voucher.Details.Add(
                                    new VoucherDetail
                                        {
                                            Title = 1221,
                                            Content = content,
                                            Remark = rmk,
                                            Fund = -capVol
                                        });
                flag = true;
            }
            if (!intFlag &&
                !Accountant.IsZero(intVol))
            {
                voucher.Details.Add(
                                    new VoucherDetail
                                        {
                                            Title = 1221,
                                            Content = content,
                                            Remark = rmk + "-利息",
                                            Fund = -intVol
                                        });
                flag = true;
            }
            if (flag)
                Accountant.Upsert(voucher);
        }
    }
}

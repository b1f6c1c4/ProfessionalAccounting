using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Console;
using AccountingServer.Console.Plugin;
using AccountingServer.Entities;

namespace AccountingServer.Plugins.Interest
{
    [Plugin(Alias = "ir")]
    public class InterestRevenue : PluginBase
    {
        public InterestRevenue(Accountant accountant) : base(accountant) { }

        public override IQueryResult Execute(params string[] pars)
        {
            if (pars.Length != 3)
                throw new ArgumentException();

            var rmk =
                Accountant.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase(
                                                           filter: new VoucherDetail { Title = 1221, Content = pars[0] },
                                                           subtotal: new SubtotalBase
                                                                         {
                                                                             GatherType = GatheringType.Zero,
                                                                             Levels = new[] { SubtotalLevel.Remark }
                                                                         }))
                          .Single(
                                  b => b.Remark != null &&
                                       b.Remark.StartsWith(pars[1], StringComparison.InvariantCultureIgnoreCase) &&
                                       !b.Remark.EndsWith("-利息"))
                          .Remark;

            DateTime? lastSettlementDate = null;
            var capitalIntegral = 0D;
            var interestIntegral = 0D;
            var rate = Convert.ToDouble(pars[2]) / 10000D;
            var capitalPattern = new VoucherDetail
                                     {
                                         Title = 1221,
                                         Content = pars[0],
                                         Remark = rmk
                                     };
            var interestPattern = new VoucherDetail
                                      {
                                          Title = 1221,
                                          Content = pars[0],
                                          Remark = rmk + "-利息"
                                      };
            var revenuePattern = new VoucherDetail
                                     {
                                         Title = 6603,
                                         SubTitle = 02,
                                         Content = "贷款利息",
                                     };
            Func<double, VoucherDetail[]> create = interest => new[]
                                                                   {
                                                                       new VoucherDetail
                                                                           {
                                                                               Title = 1221,
                                                                               Content = pars[0],
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
            foreach (var grp in Accountant.SelectVouchers(
                                                          new VoucherQueryAtomBase(
                                                              filters: new[]
                                                                           {
                                                                               capitalPattern,
                                                                               interestPattern
                                                                           }))
                                          .GroupBy(v => v.Date).OrderBy(grp => grp.Key, new DateComparer()))
            {
                if (!grp.Key.HasValue)
                    throw new NullReferenceException();
                if (!lastSettlementDate.HasValue)
                    lastSettlementDate = grp.Key;

                // Settle Interest
                {
                    var delta = grp.Key.Value.Subtract(lastSettlementDate.Value).Days;
                    var interest = delta * rate * capitalIntegral;

                    var oldVoucher = grp.SingleOrDefault(v => v.Details.Any(d => d.IsMatch(interestPattern, 1)));
                    if (oldVoucher != null)
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        if (!Accountant.IsZero(
                                               oldVoucher.Details.Single(d => d.IsMatch(interestPattern)).Fund.Value
                                               - interest))
                        {
                            if (
                                !oldVoucher.Details.All(
                                                        d => d.IsMatch(interestPattern) || d.IsMatch(revenuePattern)))
                                throw new InvalidOperationException();
                            oldVoucher.Details = create(interest);
                            Accountant.Upsert(oldVoucher);
                        }
                    }
                    else if (!Accountant.IsZero(interest))
                        Accountant.Upsert(
                                          new Voucher
                                              {
                                                  Date = grp.Key,
                                                  Details = create(interest)
                                              });
                    interestIntegral += interest;
                    lastSettlementDate = grp.Key;
                }

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
                        voucher.Details.Where(d => d.IsMatch(capitalPattern, -1) || d.IsMatch(interestPattern, -1))
                               .Select(d => d.Fund.Value)
                               .Sum();
                    if (value + interestIntegral > -Accountant.Tolerance)
                    {
                        var flag = false;
                        var intFlag = false;
                        for (var i = 0; i < voucher.Details.Count; i++)
                        {
                            if (voucher.Details[i].IsMatch(capitalPattern, -1))
                            {
                                voucher.Details.RemoveAt(i);
                                flag = true;
                                i--;
                                continue;
                            }
                            if (voucher.Details[i].IsMatch(interestPattern, -1))
                            {
                                if (intFlag)
                                {
                                    voucher.Details.RemoveAt(i);
                                    flag = true;
                                    i--;
                                    continue;
                                }
                                // ReSharper disable once PossibleInvalidOperationException
                                if (!Accountant.IsZero(voucher.Details[i].Fund.Value - value))
                                {
                                    voucher.Details[i].Fund = value;
                                    flag = true;
                                }
                                intFlag = true;
                            }
                        }
                        if (!intFlag &&
                            !Accountant.IsZero(value))
                        {
                            voucher.Details.Add(
                                                new VoucherDetail
                                                    {
                                                        Title = 1221,
                                                        Content = pars[0],
                                                        Remark = rmk + "-利息",
                                                        Fund = value
                                                    });
                            flag = true;
                        }
                        if (flag)
                            Accountant.Upsert(voucher);
                        interestIntegral -= value;
                    }
                    else
                    {
                        var vol = value + interestIntegral;
                        var flag = false;
                        var capFlag = false;
                        var intFlag = false;
                        for (var i = 0; i < voucher.Details.Count; i++)
                        {
                            if (voucher.Details[i].IsMatch(capitalPattern, -1))
                            {
                                if (capFlag)
                                {
                                    voucher.Details.RemoveAt(i);
                                    flag = true;
                                    i--;
                                    continue;
                                }
                                // ReSharper disable once PossibleInvalidOperationException
                                if (!Accountant.IsZero(voucher.Details[i].Fund.Value - vol))
                                {
                                    voucher.Details[i].Fund = vol;
                                    flag = true;
                                }
                                capFlag = true;
                            }
                            if (voucher.Details[i].IsMatch(interestPattern, -1))
                            {
                                if (intFlag)
                                {
                                    voucher.Details.RemoveAt(i);
                                    flag = true;
                                    i--;
                                    continue;
                                }
                                // ReSharper disable once PossibleInvalidOperationException
                                if (!Accountant.IsZero(voucher.Details[i].Fund.Value + interestIntegral))
                                {
                                    voucher.Details[i].Fund = -interestIntegral;
                                    flag = true;
                                }
                                intFlag = true;
                            }
                        }
                        if (!capFlag &&
                            !Accountant.IsZero(vol))
                        {
                            voucher.Details.Add(
                                                new VoucherDetail
                                                    {
                                                        Title = 1221,
                                                        Content = pars[0],
                                                        Remark = rmk,
                                                        Fund = vol
                                                    });
                            flag = true;
                        }
                        if (!intFlag &&
                            !Accountant.IsZero(interestIntegral))
                        {
                            voucher.Details.Add(
                                                new VoucherDetail
                                                    {
                                                        Title = 1221,
                                                        Content = pars[0],
                                                        Remark = rmk + "-利息",
                                                        Fund = -interestIntegral
                                                    });
                            flag = true;
                        }
                        if (flag)
                            Accountant.Upsert(voucher);
                        interestIntegral = 0;
                        capitalIntegral += vol;
                    }
                }
            }
            var today = DateTime.Now.Date;
            if (lastSettlementDate != today)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var delta = today.Subtract(lastSettlementDate.Value).Days;
                var interest = delta * rate * capitalIntegral;
                Accountant.Upsert(new Voucher { Date = today, Details = create(interest) });
            }
            return new Suceed();
        }
    }
}

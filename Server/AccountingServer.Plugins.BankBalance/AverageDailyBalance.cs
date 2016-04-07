﻿using System;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.BankBalance
{
    /// <summary>
    ///     计算日均余额
    /// </summary>
    [Plugin(Alias = "adb")]
    public class AverageDailyBalance : PluginBase
    {
        public AverageDailyBalance(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            if (pars.Length != 2)
                throw new ArgumentException("参数个数不正确", nameof(pars));

            var tdy = DateTime.Now.Date;
            var ldom = AccountantHelper.LastDayOfMonth(tdy.Year, tdy.Month);
            var rng = new DateFilter(null, tdy);
            var srng = new DateFilter(new DateTime(tdy.Year, tdy.Month, 1), tdy);
            var balance =
                Accountant.SelectVoucherDetailsGrouped(
                                                       new GroupedQueryBase(
                                                           filter: new VoucherDetail { Title = 1002, Content = pars[0] },
                                                           rng: rng,
                                                           subtotal: new SubtotalBase
                                                                         {
                                                                             AggrType = AggregationType.EveryDay,
                                                                             EveryDayRange = srng,
                                                                             GatherType = GatheringType.NonZero,
                                                                             Levels = new SubtotalLevel[] { }
                                                                         }))
                          .AggregateEveryDay(srng);

            var bal = 0D;
            var btd = 0D;
            foreach (var b in balance)
            {
                if (b.Date == tdy)
                    btd += b.Fund;
                else
                    bal += b.Fund;
            }

            var avg = double.Parse(pars[1]);
            var targ = ldom.Day * avg;
            
            var sb = new StringBuilder();
            sb.AppendLine($"Target: {targ.AsCurrency()}");
            sb.AppendLine($"Balance until yesterday: {bal.AsCurrency()}");
            if ((bal - targ).IsNonNegative())
            {
                sb.AppendLine("Achieved.");
                sb.AppendLine();

                sb.AppendLine(
                              (btd - avg).IsNonNegative()
                                  ? $"Plan A: Credit {(btd - avg).AsCurrency()}, Balance {avg.AsCurrency()}"
                                  : $"Plan A: Debit {(avg - btd).AsCurrency()}, Balance {avg.AsCurrency()}");
                sb.AppendLine("Plan B: No Action");
            }
            else
            {
                var res = targ - bal;
                var rsd = ldom.Day - tdy.Day + 1;
                sb.AppendLine($"Deficiency: {res.AsCurrency()}");
                var avx = res / rsd;
                if ((rsd * avg - res).IsNonNegative())
                {
                    sb.AppendLine($"Average deficiency: {avx.AsCurrency()} <= {avg.AsCurrency()}");
                    sb.AppendLine();

                    sb.AppendLine(
                                  (btd - avx).IsNonNegative()
                                      ? $"Plan A: Credit {(btd - avx).AsCurrency()}, Balance {avx.AsCurrency()}"
                                      : $"Plan A: Debit {(avx - btd).AsCurrency()}, Balance {avx.AsCurrency()}");
                    sb.AppendLine(
                                  (btd - avg).IsNonNegative()
                                      ? $"Plan B: Credit {(btd - avg).AsCurrency()}, Balance {avg.AsCurrency()}"
                                      : $"Plan B: Debit {(avg - btd).AsCurrency()}, Balance {avg.AsCurrency()}");
                }
                else
                {
                    sb.AppendLine($"Average deficiency: {avx.AsCurrency()} > {avg.AsCurrency()}");
                    sb.AppendLine();

                    sb.AppendLine(
                                  (btd - avx).IsNonNegative()
                                      ? $"Plan: Credit {(btd - avx).AsCurrency()}, Balance {avx.AsCurrency()}"
                                      : $"Plan: Debit {(avx - btd).AsCurrency()}, Balance {avx.AsCurrency()}");
                }
            }
            return new UnEditableText(sb.ToString());
        }
    }
}

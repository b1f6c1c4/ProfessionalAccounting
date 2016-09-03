﻿using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.Utilities
{
    /// <summary>
    ///     常见记账凭证自动填写
    /// </summary>
    public class Utilities : PluginBase
    {
        public Utilities(Accountant accountant, AccountingShell shell) : base(accountant, shell) { }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            var count = 0;
            foreach (var par in pars)
            {
                var voucher = GenerateVoucher(par);
                if (voucher != null)
                    if (Accountant.Upsert(voucher))
                        count++;
            }
            return new NumberAffected(count);
        }

        /// <summary>
        ///     生成记账凭证
        /// </summary>
        /// <param name="par">参数</param>
        /// <returns>记账凭证</returns>
        private Voucher GenerateVoucher(string par)
        {
            var date = GetDate(ref par);
            par = par.TrimStart();

            if (par.StartsWith("xy", StringComparison.Ordinal))
                return new Voucher
                           {
                               Date = date,
                               Details = new[]
                                             {
                                                 new VoucherDetail
                                                     {
                                                         Title = 1123,
                                                         Content = "洗衣卡",
                                                         Fund = -2.5
                                                     },
                                                 new VoucherDetail
                                                     {
                                                         Title = 6602,
                                                         SubTitle = 06,
                                                         Content = "洗衣",
                                                         Fund = 2.5
                                                     }
                                             }
                           };
            if (par.StartsWith("z", StringComparison.Ordinal))
            {
                double bal2;
                if (!double.TryParse(par.Substring(1).TrimStart(' ', '='), out bal2))
                    return null;
                var bal1 =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase(
                                                               filter: new VoucherDetail
                                                                           {
                                                                               Title = 1123,
                                                                               Content = "洗澡卡"
                                                                           },
                                                               subtotal:
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new SubtotalLevel[] { }
                                                                       }))
                              .Single()
                              .Fund;
                var fund = Math.Round(bal1 - bal2, 8);
                return new Voucher
                           {
                               Date = date,
                               Details = new[]
                                             {
                                                 new VoucherDetail
                                                     {
                                                         Title = 1123,
                                                         Content = "洗澡卡",
                                                         Fund = -fund
                                                     },
                                                 new VoucherDetail
                                                     {
                                                         Title = 6602,
                                                         SubTitle = 06,
                                                         Content = "洗澡",
                                                         Fund = fund
                                                     }
                                             }
                           };
            }
            if (par.StartsWith("sm", StringComparison.Ordinal))
            {
                double fund;
                if (!double.TryParse(par.Substring(1), out fund))
                    return null;
                return new Voucher
                           {
                               Date = date,
                               Details = new[]
                                             {
                                                 new VoucherDetail
                                                     {
                                                         Title = 1123,
                                                         Content = "审美",
                                                         Fund = -fund
                                                     },
                                                 new VoucherDetail
                                                     {
                                                         Title = 6602,
                                                         SubTitle = 06,
                                                         Content = "打印费",
                                                         Fund = fund
                                                     }
                                             }
                           };
            }
            if (par.StartsWith("ye", StringComparison.Ordinal))
            {
                double bal2;
                if (!double.TryParse(par.Substring(2).TrimStart(' ', '='), out bal2))
                    return null;
                var bal1 =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase(
                                                               filter: new VoucherDetail
                                                                           {
                                                                               Title = 1101,
                                                                               Content = "余额宝"
                                                                           },
                                                               subtotal:
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new SubtotalLevel[] { }
                                                                       }))
                              .Single()
                              .Fund;
                var fund = Math.Round(bal2 - bal1, 8);
                return fund.IsZero()
                           ? null
                           : new Voucher
                                 {
                                     Date = date,
                                     Details = new[]
                                                   {
                                                       new VoucherDetail
                                                           {
                                                               Title = 1101,
                                                               SubTitle = 02,
                                                               Content = "余额宝",
                                                               Fund = fund
                                                           },
                                                       new VoucherDetail
                                                           {
                                                               Title = 6111,
                                                               SubTitle = 02,
                                                               Content = "余额宝",
                                                               Fund = -fund
                                                           }
                                                   }
                                 };
            }
            if (par.StartsWith("wy", StringComparison.Ordinal))
            {
                double bal2;
                if (!double.TryParse(par.Substring(2).TrimStart(' ', '='), out bal2))
                    return null;
                var bal1 =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase(
                                                               filter: new VoucherDetail
                                                                           {
                                                                               Title = 1101,
                                                                               Content = "无忧宝"
                                                                           },
                                                               subtotal:
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new SubtotalLevel[] { }
                                                                       }))
                              .Single()
                              .Fund;
                var fund = Math.Round(bal2 - bal1, 8);
                return fund.IsZero()
                           ? null
                           : new Voucher
                                 {
                                     Date = date,
                                     Details = new[]
                                                   {
                                                       new VoucherDetail
                                                           {
                                                               Title = 1101,
                                                               SubTitle = 02,
                                                               Content = "无忧宝",
                                                               Fund = fund
                                                           },
                                                       new VoucherDetail
                                                           {
                                                               Title = 6111,
                                                               SubTitle = 02,
                                                               Content = "无忧宝",
                                                               Fund = -fund
                                                           }
                                                   }
                                 };
            }
            if (par.StartsWith("iv", StringComparison.Ordinal))
            {
                double bal2;
                if (!double.TryParse(par.Substring(2).TrimStart(' ', '='), out bal2))
                    return null;
                var bal1 =
                    Accountant.SelectVoucherDetailsGrouped(
                                                           new GroupedQueryBase(
                                                               filter: new VoucherDetail
                                                                           {
                                                                               Title = 1001
                                                                           },
                                                               subtotal:
                                                                   new SubtotalBase
                                                                       {
                                                                           GatherType = GatheringType.Zero,
                                                                           Levels = new SubtotalLevel[] { }
                                                                       }))
                              .Single()
                              .Fund;
                var fund = Math.Round(bal2 - bal1, 8);
                return fund.IsZero()
                           ? null
                           : new Voucher
                                 {
                                     Date = date,
                                     Details = new[]
                                                   {
                                                       new VoucherDetail
                                                           {
                                                               Title = 1001,
                                                               Fund = fund
                                                           },
                                                       new VoucherDetail
                                                           {
                                                               Title = 6711,
                                                               SubTitle = 10,
                                                               Content = fund < 0 ? "盘亏" : "盘盈",
                                                               Fund = -fund
                                                           }
                                                   }
                                 };
            }
            if (par.StartsWith("w", StringComparison.Ordinal))
            {
                double fund;
                if (!double.TryParse(par.Substring(1), out fund))
                    fund = 0.42;
                return new Voucher
                           {
                               Date = date,
                               Details = new[]
                                             {
                                                 new VoucherDetail
                                                     {
                                                         Title = 1123,
                                                         Content = "学生卡小钱包",
                                                         Fund = -fund
                                                     },
                                                 new VoucherDetail
                                                     {
                                                         Title = 6602,
                                                         SubTitle = 01,
                                                         Content = "水费",
                                                         Fund = fund
                                                     }
                                             }
                           };
            }
            return null;
        }

        /// <summary>
        ///     获取日期部分
        /// </summary>
        /// <param name="par">参数</param>
        /// <returns>日期</returns>
        private static DateTime GetDate(ref string par)
        {
            var rng = par.TrimStart().TakeWhile(c => c == '.').Count();
            par = par.Substring(rng);
            return rng == 0 ? DateTime.Now.Date : DateTime.Now.Date.AddDays(1 - rng);
        }
    }
}

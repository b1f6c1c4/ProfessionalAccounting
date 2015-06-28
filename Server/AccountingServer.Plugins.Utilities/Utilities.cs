using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Plugin;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.Utilities
{
    /// <summary>
    ///     自动计算利息收入和还款
    /// </summary>
    [Plugin(Alias = "u")]
    public class Utilities : PluginBase
    {
        public Utilities(Accountant accountant) : base(accountant) { }

        /// <summary>
        ///     执行插件表达式
        /// </summary>
        /// <param name="pars">
        ///     参数列表，每个参数独立作用，格式为(&lt;rng>.+)\s*(?&lt;nm>)，其中rng为日期，nm意义为
        ///     <list type="bullet">
        ///         <item>
        ///             <description>wx___表示x元水费，默认0.42元；</description>
        ///         </item>
        ///         <item>
        ///             <description>xy表示2.5元洗衣；</description>
        ///         </item>
        ///         <item>
        ///             <description>z=x__表示洗澡至x元；</description>
        ///         </item>
        ///         <item>
        ///             <description>px__表示x元打印费。</description>
        ///         </item>
        ///     </list>
        /// </param>
        /// <returns>执行结果</returns>
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

            if (par.StartsWith("w"))
            {
                double fund;
                if (!Double.TryParse(par.Substring(1), out fund))
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
            if (par.StartsWith("xy"))
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
            if (par.StartsWith("z"))
            {
                double bal2;
                if (!Double.TryParse(par.Substring(1).TrimStart(' ', '='), out bal2))
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
                var fund = bal1 - bal2;
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
            if (par.StartsWith("p"))
            {
                double fund;
                if (!Double.TryParse(par.Substring(1), out fund))
                    return null;
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

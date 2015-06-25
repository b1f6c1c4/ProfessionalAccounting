using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Console;
using AccountingServer.Console.Plugin;
using AccountingServer.Entities;
using Problem = System.Tuple
    <System.Collections.Generic.List<AccountingServer.Plugins.THUInfo.TransactionRecord>,
        System.Collections.Generic.List<AccountingServer.Entities.Voucher>>;

namespace AccountingServer.Plugins.THUInfo
{
    [Plugin(Alias = "f")]
    public partial class THUInfo : IPlugin
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public THUInfo(Accountant accountant) { m_Accountant = accountant; }

        /// <inheritdoc />
        public IQueryResult Execute(params string[] pars)
        {
            if (pars.Length >= 3)
                if (String.IsNullOrEmpty(pars[0]))
                    FetchData(pars[1], pars[2]);
            lock (m_Lock)
            {
                if (m_Data == null)
                    throw new NullReferenceException();
                List<Problem> noRemark, tooMuch, tooFew;
                Compare(out noRemark, out tooMuch, out tooFew);
                if (pars.Length == 0 ||
                    String.IsNullOrEmpty(pars[0]) ||
                    noRemark.Any() ||
                    tooMuch.Any() ||
                    tooFew.Any(p => p.Item2.Any()))
                {
                    var sb = new StringBuilder();
                    foreach (var problem in noRemark)
                    {
                        sb.AppendLine("---No Remark");
                        foreach (var r in problem.Item1)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Item2)
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    foreach (var problem in tooMuch)
                    {
                        sb.AppendLine("---Too Much Voucher");
                        foreach (var r in problem.Item1)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Item2)
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    foreach (var problem in tooFew)
                    {
                        sb.AppendLine("---Too Few Voucher");
                        foreach (var r in problem.Item1)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Item2)
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    if (sb.Length == 0)
                        return new Suceed();
                    return new EditableText(sb.ToString());
                }
                if (!tooFew.Any())
                    return new Suceed();

                List<TransactionRecord> fail;
                foreach (var voucher in AutoGenerate(tooFew.SelectMany(p => p.Item1), pars, out fail))
                    m_Accountant.Upsert(voucher);

                if (fail.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("---Can not generate");
                    foreach (var r in fail)
                        sb.AppendLine(r.ToString());
                    return new EditableText(sb.ToString());
                }
                return new Suceed();
            }
        }

        private enum RegularType
        {
            Meals,
            Deposit,
            Shopping
        }

        /// <summary>
        ///     自动生成记账凭证
        /// </summary>
        /// <param name="records">交易记录</param>
        /// <param name="pars">生成记账所需参数</param>
        /// <param name="failed">无法生产记账凭证的交易记录</param>
        private IEnumerable<Voucher> AutoGenerate(IEnumerable<TransactionRecord> records, IEnumerable<string> pars,
                                                  out List<TransactionRecord> failed)
        {
            var dic = GetDic(pars);
            var res = new List<Voucher>();
            failed = new List<TransactionRecord>();
            foreach (var grp in records.GroupBy(r => r.Date))
            {
                var lst = new List<TransactionRecord>();
                foreach (var record in grp)
                {
                    var item = new VoucherDetail
                                   {
                                       Title = 1012,
                                       SubTitle = 05,
                                       Fund = record.Fund,
                                       Remark = record.Index.ToString(CultureInfo.InvariantCulture)
                                   };
                    switch (record.Type)
                    {
                        case "支付宝充值":
                            res.Add(
                                    new Voucher
                                        {
                                            Date = grp.Key.Date,
                                            Details = new List<VoucherDetail>
                                                          {
                                                              item,
                                                              new VoucherDetail
                                                                  {
                                                                      Title = 1221,
                                                                      Content = "学生卡待领",
                                                                      Fund = -record.Fund
                                                                  }
                                                          }
                                        });
                            break;
                        case "自助缴费(学生公寓水费)":
                            item.Fund = -item.Fund;
                            res.Add(
                                    new Voucher
                                        {
                                            Date = grp.Key.Date,
                                            Details = new List<VoucherDetail>
                                                          {
                                                              item,
                                                              new VoucherDetail
                                                                  {
                                                                      Title = 1123,
                                                                      Content = "学生卡小钱包",
                                                                      Fund = record.Fund
                                                                  }
                                                          }
                                        });
                            break;
                        default:
                            lst.Add(record);
                            break;
                    }
                }
                if (!lst.Any())
                    continue;

                if (!dic.ContainsKey(grp.Key.Date))
                    failed.AddRange(grp);

                var ins = dic[grp.Key.Date];
                var id = 0;
                foreach (var inst in ins)
                {
                    if (id == lst.Count)
                        throw new Exception();
                    switch (inst.Item1)
                    {
                        case RegularType.Meals:
                            DateTime? t = null;
                            while (true)
                            {
                                if (t.HasValue)
                                {
                                    if (lst[id].Time.Subtract(t.Value).TotalHours > 1)
                                        break;
                                }
                                else
                                    t = lst[id].Time;
                                if (lst[id].Type == "消费" &&
                                    lst[id].Location == "饮食中心")
                                    res.Add(
                                            new Voucher
                                                {
                                                    Date = grp.Key.Date,
                                                    Details = new List<VoucherDetail>
                                                                  {
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = 1012,
                                                                              SubTitle = 05,
                                                                              Fund = -lst[id].Fund,
                                                                              Remark =
                                                                                  lst[id].Index.ToString(
                                                                                                         CultureInfo
                                                                                                             .InvariantCulture)
                                                                          },
                                                                      new VoucherDetail
                                                                          {
                                                                              Title = 6602,
                                                                              SubTitle = 03,
                                                                              Content = inst.Item2,
                                                                              Fund = lst[id].Fund
                                                                          }
                                                                  }
                                                });
                                else
                                    break;
                                id++;
                                if (id == lst.Count)
                                    break;
                            }
                            break;
                        case RegularType.Deposit:
                            if (lst[id].Type == "领取圈存")
                            {
                                res.Add(
                                        new Voucher
                                            {
                                                Date = grp.Key.Date,
                                                Details = new List<VoucherDetail>
                                                              {
                                                                  new VoucherDetail
                                                                      {
                                                                          Title = 1012,
                                                                          SubTitle = 05,
                                                                          Fund = lst[id].Fund,
                                                                          Remark =
                                                                              lst[id].Index.ToString(
                                                                                                     CultureInfo
                                                                                                         .InvariantCulture)
                                                                      },
                                                                  new VoucherDetail
                                                                      {
                                                                          Title = 1002,
                                                                          Content = inst.Item2,
                                                                          Fund = -lst[id].Fund
                                                                      }
                                                              }
                                            });
                                id++;
                            }
                            break;
                        case RegularType.Shopping:
                            if (lst[id].Type == "消费" &&
                                (lst[id].Location == "紫荆服务楼超市" ||
                                 lst[id].Location == "饮食广场超市"))
                            {
                                res.Add(
                                        new Voucher
                                            {
                                                Date = grp.Key.Date,
                                                Details = new List<VoucherDetail>
                                                              {
                                                                  new VoucherDetail
                                                                      {
                                                                          Title = 1012,
                                                                          SubTitle = 05,
                                                                          Fund = -lst[id].Fund,
                                                                          Remark =
                                                                              lst[id].Index.ToString(
                                                                                                     CultureInfo
                                                                                                         .InvariantCulture)
                                                                      },
                                                                  new VoucherDetail
                                                                      {
                                                                          Title = 6602,
                                                                          SubTitle = 03,
                                                                          Content = inst.Item2,
                                                                          Fund = lst[id].Fund
                                                                      }
                                                              }
                                            });
                                id++;
                            }
                            break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        ///     解析自动补全指令
        /// </summary>
        /// <param name="pars">自动补全指令</param>
        /// <returns>解析结果</returns>
        private static Dictionary<DateTime, List<Tuple<RegularType, string>>> GetDic(IEnumerable<string> pars)
        {
            var dic = new Dictionary<DateTime, List<Tuple<RegularType, string>>>();
            foreach (var par in pars)
            {
                var sp = par.Split(' ', '/', ',');
                if (sp.Length == 0)
                    continue;
                var dt = DateTime.Now.Date;
                var lst = new List<Tuple<RegularType, string>>();
                foreach (var s in sp)
                {
                    if (s.StartsWith("."))
                    {
                        dt = DateTime.Now.Date.AddDays(1 - sp[0].Trim().Length);
                        continue;
                    }

                    var canteens = new Dictionary<string, string>
                                       {
                                           { "zj", "紫荆园" },
                                           { "tl", "桃李园" },
                                           { "gc", "观畴园" },
                                           { "yh", "清青永和" },
                                           { "bs", "清青比萨" },
                                           { "xx", "清青休闲" },
                                           { "sd", "清青时代" },
                                           { "kc", "清青快餐" },
                                           { "ys", "玉树园" },
                                           { "tt", "听涛园" },
                                           { "dx", "丁香园" },
                                           { "wx", "闻馨园" },
                                           { "zl", "芝兰园" }
                                       };

                    if (canteens.ContainsKey(s.Trim().ToLowerInvariant()))
                        lst.Add(
                                new Tuple<RegularType, string>(
                                    RegularType.Meals,
                                    canteens[s.Trim().ToLowerInvariant()]));
                    else
                        switch (s.Trim().ToLowerInvariant())
                        {
                            case "sp":
                                lst.Add(
                                        new Tuple<RegularType, string>(
                                            RegularType.Shopping,
                                            "食品"));
                                break;
                            case "sh":
                                lst.Add(
                                        new Tuple<RegularType, string>(
                                            RegularType.Shopping,
                                            "生活用品"));
                                break;
                            case "5184":
                            case "3593":
                            case "9767":
                                lst.Add(
                                        new Tuple<RegularType, string>(
                                            RegularType.Deposit,
                                            s.Trim()));
                                break;
                            default:
                                throw new InvalidOperationException();
                        }
                }
                dic.Add(dt, lst);
            }
            return dic;
        }

        /// <summary>
        ///     对账
        /// </summary>
        /// <param name="noRemark">无法补足备注的记账凭证</param>
        /// <param name="tooMuch">一组交易记录对应的记账凭证过多</param>
        /// <param name="tooFew">一组交易记录对应的记账凭证过少</param>
        private void Compare(out List<Problem> noRemark, out List<Problem> tooMuch, out List<Problem> tooFew)
        {
            var filter = new VoucherDetail { Title = 1012, SubTitle = 05 };
            var data = m_Data;

            var voucherQuery = new VoucherQueryAtomBase
                                   {
                                       DetailFilter =
                                           new DetailQueryAryBase(
                                           OperatorType.Substract,
                                           new IQueryCompunded<IDetailQueryAtom>[]
                                               {
                                                   new DetailQueryAtomBase(
                                                       new VoucherDetail { Title = 1012, SubTitle = 05 }),
                                                   new DetailQueryAtomBase(new VoucherDetail { Remark = "" })
                                               })
                                   };
            foreach (var d in 
                m_Accountant.SelectVoucherDetails(
                                                  new VoucherDetailQueryBase
                                                      {
                                                          DetailEmitFilter = new EmitBase(),
                                                          VoucherQuery = voucherQuery
                                                      }))
            {
                var id = Convert.ToInt32(d.Remark);
                data.RemoveAll(r => r.Index == id);
            }


            var account =
                m_Accountant.SelectVouchers(
                                            new VoucherQueryAtomBase(
                                                filter: filter))
                            .SelectMany(
                                        v =>
                                        v.Details.Where(d => d.IsMatch(filter))
                                         .Select(d => new { Detail = d, Voucher = v })).ToList();
            noRemark = new List<Problem>();
            tooMuch = new List<Problem>();
            tooFew = new List<Problem>();

            foreach (var dfgrp in data.GroupBy(r => new { r.Date, r.Fund }))
            {
                var grp = dfgrp;
                var acc =
                    account.Where(
                                  d =>
                                  d.Voucher.Date == grp.Key.Date &&
                                  // ReSharper disable once PossibleInvalidOperationException
                                  Accountant.IsZero(Math.Abs(d.Detail.Fund.Value) - grp.Key.Fund)).ToList();
                switch (acc.Count.CompareTo(grp.Count()))
                {
                    case 0:
                        if (acc.Count == 1)
                        {
                            acc.Single().Detail.Remark = grp.Single().Index.ToString(CultureInfo.InvariantCulture);
                            m_Accountant.Upsert(acc.Single().Voucher);
                        }
                        else
                            noRemark.Add(new Problem(grp.ToList(), acc.Select(d => d.Voucher).ToList()));
                        break;
                    case 1:
                        tooMuch.Add(new Problem(grp.ToList(), acc.Select(d => d.Voucher).ToList()));
                        break;
                    case -1:
                        tooFew.Add(new Problem(grp.ToList(), acc.Select(d => d.Voucher).ToList()));
                        break;
                }
            }
        }
    }
}

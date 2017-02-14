using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Plugins.THUInfo
{
    // ReSharper disable once UnusedMember.Global
    public partial class THUInfo
    {
        /// <summary>
        ///     自动生成的记账凭证类型
        /// </summary>
        private enum RegularType
        {
            /// <summary>
            ///     餐费
            /// </summary>
            Meals,

            /// <summary>
            ///     超市购物
            /// </summary>
            Shopping,

            /// <summary>
            ///     洗澡卡和洗衣卡充值
            /// </summary>
            Charging
        }

        /// <summary>
        ///     自动生成记账凭证
        /// </summary>
        /// <param name="records">交易记录</param>
        /// <param name="pars">生成记账所需参数</param>
        /// <param name="failed">无法生产记账凭证的交易记录</param>
        private IEnumerable<Voucher> AutoGenerate(IEnumerable<TransactionRecord> records,
            IReadOnlyList<string> pars,
            out List<TransactionRecord> failed)
        {
            var dic = pars.Count > 0 ? GetDic(pars) : null;
            var res = Enumerable.Empty<Voucher>();
            failed = new List<TransactionRecord>();
            foreach (var grp in records.GroupBy(r => r.Date))
            {
                List<Voucher> res0, res1;
                List<TransactionRecord> lst;
                GenerateCommon(grp, out res0, out lst);
                res = res.Concat(res0);
                if (!lst.Any())
                    continue;

                if (dic != null)
                {
                    if (!dic.ContainsKey(grp.Key.Date))
                    {
                        failed.AddRange(grp);
                        continue;
                    }

                    GenerateSpecial(dic[grp.Key.Date], lst, grp.Key.Date, out res1);
                }
                else
                    GenerateSpecial(Enumerable.Empty<Tuple<RegularType, string>>(), lst, grp.Key.Date, out res1);

                res = res.Concat(res1);
            }

            return res;
        }

        /// <summary>
        ///     自动生成常规记账凭证
        /// </summary>
        /// <param name="recordsGroup">待处理交易记录组</param>
        /// <param name="result">生成的记账凭证</param>
        /// <param name="specialRecords">未能处理的交易记录</param>
        private static void GenerateCommon(IGrouping<DateTime, TransactionRecord> recordsGroup, out List<Voucher> result,
            out List<TransactionRecord> specialRecords)
        {
            specialRecords = new List<TransactionRecord>();
            result = new List<Voucher>();
            foreach (var record in recordsGroup)
            {
                var rec = record;
                Func<int, VoucherDetail> item =
                    dir => new VoucherDetail
                        {
                            Title = 1012,
                            SubTitle = 05,
                            Fund = dir * rec.Fund,
                            Remark = rec.Index.ToString(CultureInfo.InvariantCulture)
                        };
                switch (record.Type)
                {
                    case "支付宝充值":
                        result.Add(
                            new Voucher
                                {
                                    Date = recordsGroup.Key.Date,
                                    Details = new List<VoucherDetail>
                                        {
                                            item(1),
                                            new VoucherDetail
                                                {
                                                    Title = 1221,
                                                    Content = "学生卡待领",
                                                    Fund = -record.Fund
                                                }
                                        }
                                });
                        break;
                    case "领取圈存":
                        result.Add(
                            new Voucher
                                {
                                    Date = recordsGroup.Key.Date,
                                    Details = new List<VoucherDetail>
                                        {
                                            item(1),
                                            new VoucherDetail
                                                {
                                                    Title = 1002,
                                                    Content = "3593",
                                                    Fund = -record.Fund
                                                }
                                        }
                                });
                        break;
                    case "自助缴费(学生公寓水费)":
                        result.Add(
                            new Voucher
                                {
                                    Date = recordsGroup.Key.Date,
                                    Details = new List<VoucherDetail>
                                        {
                                            item(-1),
                                            new VoucherDetail
                                                {
                                                    Title = 1123,
                                                    Content = "学生卡小钱包",
                                                    Fund = record.Fund
                                                }
                                        }
                                });
                        break;
                    case "消费":
                        if (record.Location == "游泳馆通道机消费")
                            result.Add(
                                new Voucher
                                    {
                                        Date = recordsGroup.Key.Date,
                                        Details = new List<VoucherDetail>
                                            {
                                                item(-1),
                                                new VoucherDetail
                                                    {
                                                        Title = 6401,
                                                        Content = "训练费",
                                                        Fund = record.Fund
                                                    }
                                            }
                                    });
                        else
                        {
                            var ep = Convert.ToInt32(record.Endpoint);
                            var res = Templates.Where(t => t.Ranges.Any(iv => iv.Start <= ep && iv.End >= ep)).ToList();
                            if (res.Count == 1)
                                result.Add(
                                    new Voucher
                                        {
                                            Date = recordsGroup.Key.Date,
                                            Details = new List<VoucherDetail>
                                                {
                                                    item(-1),
                                                    new VoucherDetail
                                                        {
                                                            Title = 6602,
                                                            SubTitle = 03,
                                                            Content = res[0].Name,
                                                            Fund = record.Fund
                                                        }
                                                }
                                        });
                            else if (res.Count > 1)
                                throw new ApplicationException(
                                    $"{ep} 匹配了多个模板：" + string.Join(" ", res.Select(t => t.Name)));

                            specialRecords.Add(record);
                        }

                        break;
                    default:
                        specialRecords.Add(record);
                        break;
                }
            }
        }

        /// <summary>
        ///     自动生成常规记账凭证
        /// </summary>
        /// <param name="par">参数</param>
        /// <param name="records">待处理的交易记录</param>
        /// <param name="date">日期</param>
        /// <param name="result">生成的记账凭证</param>
        private static void GenerateSpecial(IEnumerable<Tuple<RegularType, string>> par,
            IReadOnlyList<TransactionRecord> records, DateTime date,
            out List<Voucher> result)
        {
            result = new List<Voucher>();
            var id = 0;
            foreach (var inst in par)
            {
                if (id == records.Count)
                    throw new ArgumentException("生成记账所需参数不足", nameof(par));

                Func<TransactionRecord, int, VoucherDetail> newDetail =
                    (tr, dir) => new VoucherDetail
                        {
                            Title = 1012,
                            SubTitle = 05,
                            Fund = dir * tr.Fund,
                            Remark = tr.Index.ToString(CultureInfo.InvariantCulture)
                        };
                switch (inst.Item1)
                {
                    case RegularType.Meals:
                        DateTime? t = null;
                        while (true)
                        {
                            if (t.HasValue)
                            {
                                if (records[id].Time.Subtract(t.Value).TotalHours > 1)
                                    break;
                            }
                            else
                                t = records[id].Time;

                            if (records[id].Type == "消费" &&
                                records[id].Location == "饮食中心")
                                result.Add(
                                    new Voucher
                                        {
                                            Date = date,
                                            Details = new List<VoucherDetail>
                                                {
                                                    newDetail(records[id], -1),
                                                    new VoucherDetail
                                                        {
                                                            Title = 6602,
                                                            SubTitle = 03,
                                                            Content = inst.Item2,
                                                            Fund = records[id].Fund
                                                        }
                                                }
                                        });
                            else
                                break;

                            id++;
                            if (id == records.Count)
                                break;
                        }

                        break;
                    case RegularType.Shopping:
                        if (records[id].Type == "消费" &&
                        (records[id].Location == "紫荆服务楼超市" ||
                            records[id].Location == "饮食广场超市"))
                        {
                            result.Add(
                                new Voucher
                                    {
                                        Date = date,
                                        Details = new List<VoucherDetail>
                                            {
                                                newDetail(records[id], -1),
                                                new VoucherDetail
                                                    {
                                                        Title = 6602,
                                                        SubTitle = 06,
                                                        Content = inst.Item2,
                                                        Fund = records[id].Fund
                                                    }
                                            }
                                    });
                            id++;
                        }
                        break;
                    case RegularType.Charging:
                        if (records[id].Type == "消费" &&
                            records[id].Location == "")
                        {
                            result.Add(
                                new Voucher
                                    {
                                        Date = date,
                                        Details = new List<VoucherDetail>
                                            {
                                                newDetail(records[id], -1),
                                                new VoucherDetail
                                                    {
                                                        Title = 1123,
                                                        Content = inst.Item2,
                                                        Fund = records[id].Fund
                                                    }
                                            }
                                    });
                            id++;
                        }
                        break;
                }
            }
        }
    }
}

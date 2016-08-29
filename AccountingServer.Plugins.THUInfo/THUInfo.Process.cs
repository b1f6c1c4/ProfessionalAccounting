using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;
using CredentialManagement;

namespace AccountingServer.Plugins.THUInfo
{
    /// <summary>
    ///     从info.tsinghua.edu.cn更新账户
    /// </summary>
    [Plugin(Alias = "f")]
    public partial class THUInfo : PluginBase
    {
        /// <summary>
        ///     对账忽略标志
        /// </summary>
        private const string IgnoranceMark = "reconciliation";

        private static readonly IQueryCompunded<IDetailQueryAtom> DetailQuery =
            new DetailQueryAryBase(
                OperatorType.Substract,
                new IQueryCompunded<IDetailQueryAtom>[]
                    {
                        new DetailQueryAtomBase(new VoucherDetail { Title = 1012, SubTitle = 05 }),
                        new DetailQueryAryBase(
                            OperatorType.Union,
                            new IQueryCompunded<IDetailQueryAtom>[]
                                {
                                    new DetailQueryAtomBase(new VoucherDetail { Remark = "" }),
                                    new DetailQueryAtomBase(new VoucherDetail { Remark = IgnoranceMark })
                                }
                            )
                    });

        private static readonly List<Tuple<string, List<Tuple<int, int>>>> EndPointTemplates;

        static THUInfo()
        {
            try
            {
                XmlDocument doc;
                using (
                    var stream =
                        Assembly.GetExecutingAssembly()
                                .GetManifestResourceStream("AccountingServer.Plugins.THUInfo.Resources.EndPoint.xml")
                    )
                {
                    if (stream == null)
                        throw new Exception();
                    doc = new XmlDocument();
                    doc.Load(stream);
                }
                EndPointTemplates = new List<Tuple<string, List<Tuple<int, int>>>>();
                // ReSharper disable once PossibleNullReferenceException
                foreach (XmlElement element in doc.DocumentElement.ChildNodes)
                    EndPointTemplates.Add(
                                          new Tuple<string, List<Tuple<int, int>>>(
                                              element.GetElementsByTagName("Name")[0].InnerText,
                                              element.GetElementsByTagName("Range")
                                                     .OfType<XmlElement>()
                                                     .Select(
                                                             r =>
                                                             new Tuple<int, int>(
                                                                 Convert.ToInt32(
                                                                                 r.GetElementsByTagName("Start")[0]
                                                                                     .InnerText),
                                                                 Convert.ToInt32(
                                                                                 r.GetElementsByTagName("End")[0]
                                                                                     .InnerText)))
                                                     .ToList()));
            }
            catch (Exception)
            {
                EndPointTemplates = null;
            }
        }

        public THUInfo(Accountant accountant) : base(accountant) { }

        /// <summary>
        ///     读取凭证并获取数据
        /// </summary>
        public void FetchData()
        {
            var cred = CredentialTemplate();
            if (cred.Exists())
            {
                cred.Load();
                FetchData(cred.Username, cred.Password);
                return;
            }

            var credential = PromptForCredential();
            if (credential == null)
                return;

            FetchData(credential.Username, credential.Password);
        }

        private static Credential CredentialTemplate() =>
            new Credential
                {
                    Target = "THUInfo",
                    PersistanceType = PersistanceType.Enterprise,
                    Type = CredentialType.DomainVisiblePassword
                };

        /// <summary>
        ///     删除保存的凭证
        /// </summary>
        private static void DropCredential()
        {
            var cred = CredentialTemplate();
            cred.Delete();
        }

        /// <summary>
        ///     提示输入凭证
        /// </summary>
        /// <returns>凭证</returns>
        private static Credential PromptForCredential()
        {
            var prompt = new XPPrompt
                             {
                                 Target = "THUInfo",
                                 Persist = true
                             };
            if (prompt.ShowDialog() != DialogResult.OK)
                return null;

            var cred = CredentialTemplate();
            cred.Username = prompt.Username;
            cred.Password = prompt.Password;

            cred.Save();
            return cred;
        }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            if (pars.Length == 1 &&
                pars[0] == "ep")
            {
                var sb = new StringBuilder();
                var voucherQuery = new VoucherQueryAtomBase { DetailFilter = DetailQuery };
                var bin = new HashSet<int>();
                foreach (var d in Accountant.SelectVouchers(voucherQuery)
                                            .SelectMany(
                                                        v => v.Details.Where(d => d.IsMatch(DetailQuery))
                                                              .Select(d => new VDetail { Detail = d, Voucher = v })))
                {
                    var id = Convert.ToInt32(d.Detail.Remark);
                    if (!bin.Add(id))
                        continue;
                    var record = m_Data.SingleOrDefault(r => r.Index == id);
                    if (record == null)
                        continue;
                    // ReSharper disable once PossibleInvalidOperationException
                    if (!(Math.Abs(d.Detail.Fund.Value) - record.Fund).IsZero())
                        continue;
                    var tmp = d.Voucher.Details.Where(dd => dd.Title == 6602).ToArray();
                    var content = tmp.Length == 1 ? tmp.First().Content : string.Empty;
                    sb.AppendLine($"{d.Voucher.ID}\t{d.Detail.Remark}\t{content}\t{record.Endpoint}");
                }

                return new UnEditableText(sb.ToString());
            }

            if (pars.Length == 1 &&
                pars[0] == "cred")
            {
                DropCredential();
                FetchData();
            }

            lock (m_Lock)
            {
                if (m_Data == null)
                    throw new InvalidOperationException("没有消费记录");
                List<Problem> noRemark, tooMuch, tooFew;
                List<VDetail> noRecord;
                Compare(out noRemark, out tooMuch, out tooFew, out noRecord);
                if ((pars.Length == 1 && pars[0] == "whatif") ||
                    noRemark.Any() ||
                    tooMuch.Any() ||
                    tooFew.Any(p => p.Details.Any()) ||
                    noRecord.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var problem in noRemark)
                    {
                        sb.AppendLine("---No Remark");
                        foreach (var r in problem.Records)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    foreach (var problem in tooMuch)
                    {
                        sb.AppendLine("---Too Much Voucher");
                        foreach (var r in problem.Records)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    foreach (var problem in tooFew)
                    {
                        sb.AppendLine("---Too Few Voucher");
                        foreach (var r in problem.Records)
                            sb.AppendLine(r.ToString());
                        foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                        sb.AppendLine();
                    }
                    if (noRecord.Any())
                    {
                        sb.AppendLine("---No Record");
                        foreach (var v in noRecord.Select(d => d.Voucher).Distinct())
                            sb.AppendLine(CSharpHelper.PresentVoucher(v));
                    }
                    if (sb.Length == 0)
                        return new Succeed();
                    return new EditableText(sb.ToString());
                }
                if (!tooFew.Any())
                    return new Succeed();

                List<TransactionRecord> fail;
                foreach (var voucher in AutoGenerate(tooFew.SelectMany(p => p.Records), pars, out fail))
                    Accountant.Upsert(voucher);

                if (fail.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("---Can not generate");
                    foreach (var r in fail)
                        sb.AppendLine(r.ToString());
                    return new EditableText(sb.ToString());
                }
                return new Succeed();
            }
        }

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
        private static IEnumerable<Voucher> AutoGenerate(IEnumerable<TransactionRecord> records,
                                                         IList<string> pars,
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
                {
                    GenerateSpecial(Enumerable.Empty<Tuple<RegularType, string>>(), lst, grp.Key.Date, out res1);
                }

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
                        try
                        {
                            var ep = Convert.ToInt32(record.Endpoint);
                            var res =
                                EndPointTemplates.SingleOrDefault(
                                                                  t =>
                                                                  t.Item2.Any(iv => iv.Item1 <= ep && iv.Item2 >= ep));
                            if (res != null)
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
                                                                             Content = res.Item1,
                                                                             Fund = record.Fund
                                                                         }
                                                                 }
                                               });
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        specialRecords.Add(record);
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

        /// <summary>
        ///     解析自动补全指令
        /// </summary>
        /// <param name="pars">自动补全指令</param>
        /// <returns>解析结果</returns>
        private static Dictionary<DateTime, List<Tuple<RegularType, string>>> GetDic(IList<string> pars)
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
                    var ss = s.Trim().ToLowerInvariant();
                    if (ss.StartsWith(".", StringComparison.Ordinal))
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

                    if (canteens.ContainsKey(ss))
                        lst.Add(new Tuple<RegularType, string>(RegularType.Meals, canteens[ss]));
                    else
                        switch (ss)
                        {
                            case "sp":
                                lst.Add(new Tuple<RegularType, string>(RegularType.Shopping, "食品"));
                                break;
                            case "sh":
                                lst.Add(new Tuple<RegularType, string>(RegularType.Shopping, "生活用品"));
                                break;
                            case "bg":
                                lst.Add(new Tuple<RegularType, string>(RegularType.Shopping, "办公用品"));
                                break;
                            case "xz":
                                lst.Add(new Tuple<RegularType, string>(RegularType.Charging, "洗澡卡"));
                                break;
                            case "xy":
                                lst.Add(new Tuple<RegularType, string>(RegularType.Charging, "洗衣"));
                                break;
                            default:
                                throw new ArgumentException("未知参数", nameof(pars));
                        }
                }
                dic.Add(dt, lst);
            }
            return dic;
        }

        /// <summary>
        ///     包含记账凭证信息的细目
        /// </summary>
        private struct VDetail
        {
            /// <summary>
            ///     细目
            /// </summary>
            public VoucherDetail Detail;

            /// <summary>
            ///     细目所在的记账凭证
            /// </summary>
            public Voucher Voucher;
        }

        /// <summary>
        ///     对账问题
        /// </summary>
        private sealed class Problem
        {
            /// <summary>
            ///     消费记录组
            /// </summary>
            public List<TransactionRecord> Records { get; }

            /// <summary>
            ///     记账凭证组
            /// </summary>
            public List<VDetail> Details { get; }

            public Problem(List<TransactionRecord> records, List<VDetail> details)
            {
                Records = records;
                Details = details;
            }
        }

        /// <summary>
        ///     对账并自动补足备注
        /// </summary>
        /// <param name="noRemark">无法自动补足备注的记账凭证</param>
        /// <param name="tooMuch">一组交易记录对应的记账凭证过多</param>
        /// <param name="tooFew">一组交易记录对应的记账凭证过少</param>
        /// <param name="noRecord">一个记账凭证没有可能对应的交易记录</param>
        private void Compare(out List<Problem> noRemark, out List<Problem> tooMuch, out List<Problem> tooFew,
                             out List<VDetail> noRecord)
        {
            var data = new List<TransactionRecord>(m_Data);

            var voucherQuery = new VoucherQueryAtomBase { DetailFilter = DetailQuery };
            var bin = new HashSet<int>();
            var binConflict = new HashSet<int>();
            foreach (var d in Accountant.SelectVouchers(voucherQuery)
                                        .SelectMany(
                                                    v => v.Details.Where(d => d.IsMatch(DetailQuery))
                                                          .Select(d => new VDetail { Detail = d, Voucher = v })))
            {
                var id = Convert.ToInt32(d.Detail.Remark);
                if (!bin.Add(id))
                {
                    binConflict.Add(id);
                    continue;
                }
                var record = data.SingleOrDefault(r => r.Index == id);
                if (record == null)
                {
                    d.Detail.Remark = null;
                    Accountant.Upsert(d.Voucher);
                    continue;
                }
                // ReSharper disable once PossibleInvalidOperationException
                if (!(Math.Abs(d.Detail.Fund.Value) - record.Fund).IsZero())
                {
                    d.Detail.Remark = null;
                    Accountant.Upsert(d.Voucher);
                    continue;
                }
                data.Remove(record);
            }
            foreach (var id in binConflict)
            {
                var record = m_Data.SingleOrDefault(r => r.Index == id);
                if (record != null)
                    data.Add(record);

                var fltr = new VoucherDetail
                               {
                                   Title = 1012,
                                   SubTitle = 05,
                                   Remark = id.ToString(CultureInfo.InvariantCulture)
                               };
                foreach (var voucher in Accountant.SelectVouchers(new VoucherQueryAtomBase(filter: fltr)))
                {
                    foreach (var d in voucher.Details.Where(d => d.IsMatch(fltr)))
                        d.Remark = null;
                    Accountant.Upsert(voucher);
                }
            }

            var filter0 = new VoucherDetail { Title = 1012, SubTitle = 05, Remark = "" };
            var account = Accountant.SelectVouchers(new VoucherQueryAtomBase(filter: filter0))
                                    .SelectMany(
                                                v => v.Details.Where(d => d.IsMatch(filter0))
                                                      .Select(d => new VDetail { Detail = d, Voucher = v })).ToList();

            noRemark = new List<Problem>();
            tooMuch = new List<Problem>();
            tooFew = new List<Problem>();
            noRecord = new List<VDetail>(account);

            foreach (var dfgrp in data.GroupBy(r => new { r.Date, r.Fund }))
            {
                var grp = dfgrp;
                var acc =
                    account.Where(
                                  d =>
                                  d.Voucher.Date == grp.Key.Date &&
                                  // ReSharper disable once PossibleInvalidOperationException
                                  (Math.Abs(d.Detail.Fund.Value) - grp.Key.Fund).IsZero()).ToList();
                noRecord.RemoveAll(acc.Contains);
                switch (acc.Count.CompareTo(grp.Count()))
                {
                    case 0:
                        if (acc.Count == 1)
                        {
                            acc.Single().Detail.Remark = grp.Single().Index.ToString(CultureInfo.InvariantCulture);
                            Accountant.Upsert(acc.Single().Voucher);
                        }
                        else
                            noRemark.Add(new Problem(grp.ToList(), acc));
                        break;
                    case 1:
                        tooMuch.Add(new Problem(grp.ToList(), acc));
                        break;
                    case -1:
                        tooFew.Add(new Problem(grp.ToList(), acc));
                        break;
                }
            }
        }
    }
}

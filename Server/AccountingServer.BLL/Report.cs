using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    public class Report
    {
        public struct ReportItem
        {
            public string Title { get; set; }
            public string Remark { get; set; }
            public string OrigRemark { get; set; }
            public double Fund { get; set; }
            public double Coefficient { get; set; }
        }

        private IEnumerable<ReportItem> m_Report;

        public IEnumerable<ReportItem> ReportData { get { return m_Report; } }

        public Report(BHelper helper, DateTime startDate, DateTime endDate)
        {
            var sID = helper.GetIDFromDate(startDate);
            var eID = helper.GetIDFromDate(endDate.AddDays(1)) - 1;
            m_Report = GenerateReport(helper, sID, eID);
        }

        public void ToCSV(string path)
        {
            using (var fs = File.OpenWrite(path))
            using (var tw = new StreamWriter(fs, Encoding.UTF8))
                foreach (var reportItem in m_Report)
                    tw.WriteLine(
                                 "\"{0}\",\"{1}\",\"{2}\",{3:R},{4:R}",
                                 reportItem.Title,
                                 reportItem.Remark,
                                 reportItem.OrigRemark,
                                 reportItem.Fund,
                                 reportItem.Coefficient);
        }

        private static IEnumerable<ReportItem> GenerateReport(BHelper helper, int sID, int eID)
        {
            {
                var lst = helper.GetXBalances((decimal)1403.00, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = "（移动电源零部件）",
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0.2
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)1412.00, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)1701.00, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6401.00, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    if (detail.Remark == "本科学费")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6402.00, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    if (detail.Remark == "贵金属")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.01, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                            {
                                Title = helper.GetTitleName(detail.Title),
                                Remark = detail.Remark,
                                OrigRemark = detail.Remark,
                                Fund = (double)detail.Fund.Value,
                                Coefficient = (detail.Remark == "水费" || detail.Remark == "电费") ? 1 : 0.2
                            };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.03, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    switch (detail.Remark)
                    {
                        case "观畴园":
                        case "紫荆园":
                        case "桃李园":
                        case "清青比萨":
                        case "清青快餐":
                        case "玉树园":
                        case "闻馨园":
                        case "听涛园":
                        case "丁香园":
                        case "芝兰园":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = "（清华大学食堂）",
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 1
                                };
                            break;
                        default:
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 0.01
                                };
                            break;
                    }
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.04, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.05, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.06, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    switch (detail.Remark)
                    {
                        case "食品":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 0.9
                                };
                            break;
                        case "洗衣":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 1
                                };
                            break;
                        case "洗澡":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 1
                                };
                            break;
                        case "生活用品":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 0.5
                                };
                            break;
                        case "理发":
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 0.5
                                };
                            break;
                        default:
                            yield return
                                new ReportItem
                                {
                                    Title = helper.GetTitleName(detail.Title),
                                    Remark = detail.Remark,
                                    OrigRemark = detail.Remark,
                                    Fund = (double)detail.Fund.Value,
                                    Coefficient = 0
                                };
                            break;
                    }
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.08, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0.01
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.09, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.10, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = null,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetXBalances((decimal)6602.99, false, true, sID, eID);
                foreach (var detail in lst)
                {
                    if (detail.Remark != "维修费")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = helper.GetTitleName(detail.Title),
                            Remark = detail.Remark,
                            OrigRemark = detail.Remark,
                            Fund = (double)detail.Fund.Value,
                            Coefficient = 0.05
                        };
                }
            }
            {
                var lst = helper.GetXBalancesD((decimal)1601.00, 1, true, sID, eID);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                            {
                                Title = helper.GetTitleName(detail.Title),
                                Remark =
                                    (String.IsNullOrEmpty(detail.Remark))
                                        ? null
                                        : helper.GetFixedAssetName(Guid.Parse(detail.Remark)),
                                OrigRemark = detail.Remark,
                                Fund = (double)detail.Fund.Value,
                                Coefficient = 0
                            };
                }
            }
        }
    }
}

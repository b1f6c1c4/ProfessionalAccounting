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
    /// <summary>
    /// 会计报表报表
    /// </summary>
    public class ReimbursementReport
    {
        /// <summary>
        /// 报表项目
        /// </summary>
        public struct ReportItem
        {
            /// <summary>
            /// 科目
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 内容
            /// </summary>
            public string Content { get; set; }
            /// <summary>
            /// 实际内容
            /// </summary>
            public string OrigContent { get; set; }
            /// <summary>
            /// 金额
            /// </summary>
            public double Fund { get; set; }
            /// <summary>
            /// 系数
            /// </summary>
            public double Coefficient { get; set; }
        }

        /// <summary>
        /// 报表数据
        /// </summary>
        private IEnumerable<ReportItem> m_Report;

        /// <summary>
        /// 报表数据
        /// </summary>
        public IEnumerable<ReportItem> ReportData { get { return m_Report; } }

        /// <summary>
        /// 创建报表
        /// </summary>
        /// <param name="helper">会计业务处理类</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">截止日期</param>
        public ReimbursementReport(Accountant helper, DateTime startDate, DateTime endDate)
        {
            m_Report = GenerateReport(helper, startDate, endDate);
        }

        /// <summary>
        /// 写入CSV文件
        /// </summary>
        /// <param name="path">文件路径</param>
        public void ToCSV(string path)
        {
            using (var fs = File.OpenWrite(path))
            using (var tw = new StreamWriter(fs, Encoding.UTF8))
                foreach (var reportItem in m_Report)
                    tw.WriteLine(
                                 "\"{0}\",\"{1}\",\"{2}\",{3:R},{4:R}",
                                 reportItem.Title,
                                 reportItem.Content,
                                 reportItem.OrigContent,
                                 reportItem.Fund,
                                 reportItem.Coefficient);
        }

        /// <summary>
        /// 生成报表数据
        /// </summary>
        /// <param name="helper">会计业务处理类</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">截止日期</param>
        /// <returns>报表数据</returns>
        private static IEnumerable<ReportItem> GenerateReport(Accountant helper, DateTime startDate, DateTime endDate)
        {
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1403 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = "（移动电源零部件）",
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0.2
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1412 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1701 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6401 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    if (detail.Content == "本科学费")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6402 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    if (detail.Content == "贵金属")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 01 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                            {
                                Title = Accountant.GetTitleName(detail.Title),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = (detail.Content == "水费" || detail.Content == "电费") ? 1 : 0.2
                            };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 03 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    switch (detail.Content)
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
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = "（清华大学食堂）",
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 1
                                };
                            break;
                        default:
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 0.01
                                };
                            break;
                    }
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 04 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 05 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 06 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    switch (detail.Content)
                    {
                        case "食品":
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 0.9
                                };
                            break;
                        case "洗衣":
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 1
                                };
                            break;
                        case "洗澡":
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 1
                                };
                            break;
                        case "生活用品":
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 0.5
                                };
                            break;
                        case "理发":
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 0.5
                                };
                            break;
                        default:
                            yield return
                                new ReportItem
                                {
                                    Title = Accountant.GetTitleName(detail.Title),
                                    Content = detail.Content,
                                    OrigContent = detail.Content,
                                    Fund = detail.Fund,
                                    Coefficient = 0
                                };
                            break;
                    }
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 08 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0.01
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 09 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 1
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 10 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = null,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0
                        };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 99 }, startDate, endDate);
                foreach (var detail in lst)
                {
                    if (detail.Content != "维修费")
                        continue;
                    yield return
                        new ReportItem
                        {
                            Title = Accountant.GetTitleName(detail.Title),
                            Content = detail.Content,
                            OrigContent = detail.Content,
                            Fund = detail.Fund,
                            Coefficient = 0.05
                        };
                }
            }
            //{
            //    var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1601 }, startDate, endDate); // TODO : INC ONLY
            //    foreach (var detail in lst)
            //    {
            //        yield return
            //            new ReportItem
            //                {
            //                    Title = Accountant.GetTitleName(detail.Title),
            //                    Content =
            //                        (String.IsNullOrEmpty(detail.Content))
            //                            ? null
            //                            : helper.GetFixedAssetName(Guid.Parse(detail.Content)), // TODO : Get Fixed Asset Name
            //                    OrigContent = detail.Content,
            //                    Fund = detail.Fund,
            //                    Coefficient = 0
            //                };
            //    }
            //}
        }
    }
}

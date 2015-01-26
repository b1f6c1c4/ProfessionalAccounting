using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     会计报表报表
    /// </summary>
    public class ReimbursementReport
    {
        /// <summary>
        ///     报表项目
        /// </summary>
        public struct ReportItem
        {
            /// <summary>
            ///     科目
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            ///     内容
            /// </summary>
            public string Content { get; set; }

            /// <summary>
            ///     实际内容
            /// </summary>
            public string OrigContent { get; set; }

            /// <summary>
            ///     金额
            /// </summary>
            public double Fund { get; set; }

            /// <summary>
            ///     系数
            /// </summary>
            public double Coefficient { get; set; }
        }

        /// <summary>
        ///     报表数据
        /// </summary>
        private readonly List<ReportItem> m_Report;

        /// <summary>
        ///     报表数据
        /// </summary>
        public IEnumerable<ReportItem> ReportData { get { return m_Report; } }

        /// <summary>
        ///     创建报表
        /// </summary>
        /// <param name="helper">会计业务处理类</param>
        /// <param name="rng">日期过滤器</param>
        public ReimbursementReport(Accountant helper, DateFilter rng)
        {
            m_Report = GenerateReport(helper, rng).ToList();
        }

        /// <summary>
        ///     写入CSV文件
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
        ///     预览
        /// </summary>
        /// <returns>空格对齐的报表</returns>
        public string Preview()
        {
            var sb = new StringBuilder();
            foreach (var reportItem in m_Report)
            {
                sb.AppendFormat(
                                "{0}{1}{2}{3}{4}",
                                reportItem.Title.CPadRight(20),
                                reportItem.Content.CPadRight(20),
                                reportItem.OrigContent.CPadRight(20),
                                reportItem.Fund.AsCurrency().CPadLeft(12),
                                reportItem.Coefficient.ToString("0.0%").CPadLeft(12));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        ///     导出
        /// </summary>
        /// <returns>制表符分隔的报表</returns>
        public string ExportString()
        {
            var sb = new StringBuilder();
            foreach (var reportItem in m_Report)
            {
                sb.AppendFormat(
                                "{0}\t{1}\t{2:R}\t{3:R}",
                                reportItem.Title,
                                reportItem.Content,
                                reportItem.Fund,
                                reportItem.Coefficient);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        ///     生成报表数据
        /// </summary>
        /// <param name="helper">会计业务处理类</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>报表数据</returns>
        private static IEnumerable<ReportItem> GenerateReport(Accountant helper, DateFilter rng)
        {
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1403 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = "（移动电源零部件）",
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 0.2
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1412 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 0
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1601 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = detail.Content == null ? 1 : 0.01
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 1701 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 0
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6401 }, rng);
                foreach (var detail in lst)
                {
                    if (detail.Content == "本科学费")
                        continue;
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 1
                            };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6402 }, rng);
                foreach (var detail in lst)
                {
                    if (detail.Content == "贵金属")
                        continue;
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 0
                            };
                }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 01 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = (detail.Content == "水费" || detail.Content == "电费") ? 1 : 0.2
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 03 }, rng);
                foreach (var detail in lst)
                    switch (detail.Content)
                    {
                        case "观畴园":
                        case "紫荆园":
                        case "桃李园":
                        case "清青比萨":
                        case "清青快餐":
                        case "清青时代":
                        case "玉树园":
                        case "闻馨园":
                        case "听涛园":
                        case "丁香园":
                        case "芝兰园":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = "（食堂）",
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "丽华快餐":
                        case "庆丰包子铺":
                        case "永和大王":
                        case "拉登烤肉拌饭":
                        case "吉野家":
                        case "没名儿生煎":
                        case "嘉口福":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = "（定点I）",
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.9
                                    };
                            break;
                        case "麦当劳":
                        case "赛百味":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = "（定点II）",
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.2
                                    };
                            break;
                        default:
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.01
                                    };
                            break;
                    }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 04 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 1
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 05 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 1
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 06 }, rng);
                foreach (var detail in lst)
                    switch (detail.Content)
                    {
                        case "食品":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
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
                                        Title = TitleManager.GetTitleName(detail),
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
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "服装":
                        case "生活用品":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
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
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.5
                                    };
                            break;
                        case "电影":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.01
                                    };
                            break;
                        case "音乐会":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        default:
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0
                                    };
                            break;
                    }
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 08 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content != null && detail.Content.EndsWith("路") ? "（公交）" : detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = detail.Content == "地铁" || detail.Content != null && detail.Content.EndsWith("路") ? 1.00 : 0.01
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 09 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = detail.Content,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 1
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 10 }, rng);
                foreach (var detail in lst)
                    yield return
                        new ReportItem
                            {
                                Title = TitleManager.GetTitleName(detail),
                                Content = null,
                                OrigContent = detail.Content,
                                Fund = detail.Fund,
                                Coefficient = 1
                            };
            }
            {
                var lst = helper.GetBalancesAcrossContent(new Balance { Title = 6602, SubTitle = 99 }, rng);
                foreach (var detail in lst)
                    switch (detail.Content)
                    {
                        case "Github":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "团费":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "培训费":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "短信告知":
                        case "短信服务":
                        case "短信通知":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = "（银行短信提醒）",
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 1
                                    };
                            break;
                        case "维修费":
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0.05
                                    };
                            break;
                        default:
                            yield return
                                new ReportItem
                                    {
                                        Title = TitleManager.GetTitleName(detail),
                                        Content = detail.Content,
                                        OrigContent = detail.Content,
                                        Fund = detail.Fund,
                                        Coefficient = 0
                                    };
                            break;
                    }
            }
        }
    }
}

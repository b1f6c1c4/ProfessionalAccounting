using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Chart
{
    internal abstract class AccountingChart
    {
        private DateTime m_CurDate;
        private DateFilter m_DateFilter;

        protected readonly Accountant Accountant;

        protected AccountingChart(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
        {
            Accountant = helper;
            m_DateFilter = new DateFilter(startDate, endDate);
            m_CurDate = curDate;
        }

        public DateTime CurDate { get { return m_CurDate; } set { m_CurDate = value; } }

        public DateTime StartDate
        {
            // ReSharper disable once PossibleInvalidOperationException
            get { return m_DateFilter.StartDate.Value; }
            set { m_DateFilter.StartDate = value; }
        }

        public DateTime EndDate
        {
            // ReSharper disable once PossibleInvalidOperationException
            get { return m_DateFilter.EndDate.Value; }
            set { m_DateFilter.EndDate = value; }
        }

        protected DateFilter DateRange { get { return m_DateFilter; } }

        protected void SetupChartArea(ChartArea area)
        {
            area.CursorX.Position = m_CurDate.ToOADate();
            area.AxisX.IsMarginVisible = false;
            area.AxisX.Minimum = StartDate.ToOADate();
            area.AxisX.Maximum = EndDate.ToOADate();
            area.AxisX.MinorGrid.Enabled = true;
            area.AxisX.MinorGrid.Interval = 1;
            area.AxisX.MinorGrid.LineColor = Color.DarkGray;
            area.AxisX.MajorGrid.Interval = 7;
            area.AxisX.MajorGrid.LineWidth = 2;
            area.AxisX.MajorGrid.IntervalOffset = -(int)StartDate.DayOfWeek;
            area.AxisX.LabelStyle = new LabelStyle
                                        {
                                            Format = "MM-dd",
                                            Font =
                                                new Font(
                                                "Consolas",
                                                14,
                                                GraphicsUnit.Pixel)
                                        };
        }

        public abstract ChartArea Setup();

        public abstract IEnumerable<Series> Gather();
    }

    internal sealed class 投资资产 : AccountingChart
    {
        public 投资资产(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("投资资产");
            SetupChartArea(ar);
            ar.AxisY.Minimum = 0;
            return ar;
        }

        private Series Gather(string content, Balance filter, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.StackedArea, ChartArea = "投资资产" };
            var balances = Accountant.GetDailyBalance(filter, DateRange);
            foreach (var balance in balances)
                s.Points.AddXY(balance.Date.Value, balance.Fund);
            s.Color = color;
            return s;
        }
        private Series Gather(string content, IEnumerable<Balance> filter, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.StackedArea, ChartArea = "投资资产" };
            var balances = Accountant.GetDailyBalance(filter, DateRange);
            foreach (var balance in balances)
                s.Points.AddXY(balance.Date.Value, balance.Fund);
            s.Color = color;
            return s;
        }

        public override IEnumerable<Series> Gather()
        {
            yield return Gather("存出投资款", new Balance { Title = 1012, SubTitle = 04 }, Color.Maroon);
            yield return Gather("中银活期宝", new Balance { Title = 1101, Content = "中银活期宝" }, Color.SpringGreen);
            yield return Gather("广发基金天天红", new Balance { Title = 1101, Content = "广发基金天天红" }, Color.GreenYellow);
            yield return Gather("余额宝", new Balance { Title = 1101, Content = "余额宝" }, Color.MediumSpringGreen);
            yield return Gather("中银优选", new Balance { Title = 1101, Content = "中银优选" }, Color.DarkOrange);
            yield return Gather("中银增利", new Balance { Title = 1101, Content = "中银增利" }, Color.DarkMagenta);
            yield return Gather("中银纯债C", new Balance { Title = 1101, Content = "中银纯债C" }, Color.DarkOrchid);
            yield return Gather("定存宝A", new Balance { Title = 1101, Content = "定存宝A" }, Color.Navy);
            yield return Gather("月息通", new[]
                                           {
                                               new Balance { Title = 1101, Content = "月息通 YAD14I3000" } ,
                                               new Balance { Title = 1101, Content = "月息通 YDK15A1651" } 
                                           }, Color.Olive);
            yield return Gather("富盈人生第34期", new Balance { Title = 1101, Content = "富盈人生第34期" }, Color.MidnightBlue);
            yield return Gather("贵金属", new Balance { Title = 1441 }, Color.Gold);
        }
    }

    internal sealed class 生活资产 : AccountingChart
    {
        public 生活资产(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("生活资产");
            SetupChartArea(ar);
            ar.AlignWithChartArea = "投资资产";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            //ar.AxisY.Maximum = 7000;
            return ar;
        }

        private Series Gather生活资产(string content, Balance filter, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.StackedArea, ChartArea = "生活资产" };
            var balances = Accountant.GetDailyBalance(filter, DateRange);
            foreach (var balance in balances)
                s.Points.AddXY(balance.Date.Value, balance.Fund);
            s.Color = color;
            return s;
        }

        public override IEnumerable<Series> Gather()
        {
            yield return Gather生活资产("学生卡", new Balance { Title = 1012, SubTitle = 05 }, Color.LightSkyBlue);
            yield return Gather生活资产("现金", new Balance { Title = 1001 }, Color.PaleVioletRed);
            yield return Gather生活资产("公交卡", new Balance { Title = 1012, SubTitle = 01 }, Color.BlueViolet);
            yield return Gather生活资产("借记卡", new Balance { Title = 1002 }, Color.Chartreuse);
        }
    }

    internal sealed class 其他资产 : AccountingChart
    {
        public 其他资产(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("其他资产");
            SetupChartArea(ar);
            ar.AlignWithChartArea = "投资资产";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            return ar;
        }

        private Series Gather(string content, IEnumerable<Balance> filters, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.StackedArea, ChartArea = "其他资产" };
            var balances = Accountant.GetDailyBalance(filters, DateRange);
            foreach (var balance in balances)
                s.Points.AddXY(balance.Date.Value, balance.Fund);
            s.Color = color;
            return s;
        }

        public override IEnumerable<Series> Gather()
        {
            yield return Gather(
                                "固定资产",
                                new[]
                                    {
                                        new Balance { Title = 1601 }, new Balance { Title = 1602 },
                                        new Balance { Title = 1603 }
                                    },
                                Color.BlueViolet);
            yield return Gather(
                                "无形资产",
                                new[]
                                    {
                                        new Balance { Title = 1701 }, new Balance { Title = 1702 },
                                        new Balance { Title = 1703 }
                                    },
                                Color.Purple);
            yield return Gather(
                                "原材料等",
                                new[]
                                    {
                                        new Balance { Title = 1403 }, new Balance { Title = 1405 },
                                        new Balance { Title = 1412 }, new Balance { Title = 1604 },
                                        new Balance { Title = 1605 }
                                    },
                                Color.DarkViolet);
            yield return Gather(
                                "其他（含应收）",
                                new[]
                                    {
                                        new Balance { Title = 1012, SubTitle = 03 }, new Balance { Title = 1122 },
                                        new Balance { Title = 1221 }, new Balance { Title = 1511 }
                                    },
                                Color.MediumVioletRed);
            yield return Gather(
                                "其他（含预付）",
                                new[]
                                    {
                                        new Balance { Title = 1123 }, new Balance { Title = 1606 },
                                        new Balance { Title = 1901 }
                                    },
                                Color.Violet);
        }
    }

    internal sealed class 生活费用 : AccountingChart
    {
        public 生活费用(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("生活费用");
            SetupChartArea(ar);
            ar.AxisY.Minimum = 0;
            //ar.AxisY.Maximum = 2000;
            return ar;
        }

        public override IEnumerable<Series> Gather()
        {
            var balance1 =
                Accountant.GetDailyBalance(new Balance { Title = 6602, SubTitle = 03 }, DateRange, 1).ToArray();
            var balance2 =
                Accountant.GetDailyBalance(
                                           new Balance { Title = 6602, SubTitle = 06, Content = "食品" },
                                           DateRange,
                                           1).ToArray();
            var balance3 =
                Accountant.GetDailyBalance(
                                           new Balance { Title = 6602, SubTitle = 01, Content = "水费" },
                                           DateRange,
                                           1).ToArray();
            var balance4 =
                Accountant.GetDailyBalance(new Balance { Title = 6602, SubTitle = 06 }, DateRange, 1).ToArray();
            var balance5 = Accountant.GetDailyBalance(
                                                      new[]
                                                          { new Balance { Title = 6401 }, new Balance { Title = 6402 } },
                                                      DateRange,
                                                      1).ToArray();
            var balance6 =
                Accountant.GetDailyBalance(
                                           new[]
                                               {
                                                   new Balance { Title = 6602, SubTitle = 01 },
                                                   new Balance { Title = 6602, SubTitle = 02 },
                                                   new Balance { Title = 6602, SubTitle = 04 },
                                                   new Balance { Title = 6602, SubTitle = 05 },
                                                   new Balance { Title = 6602, SubTitle = 08 },
                                                   new Balance { Title = 6602, SubTitle = 09 },
                                                   new Balance { Title = 6602, SubTitle = 10 },
                                                   new Balance { Title = 6602, SubTitle = 99 },
                                                   new Balance { Title = 6603 },
                                                   new Balance { Title = 6711, SubTitle = 08 },
                                                   new Balance { Title = 6711, SubTitle = 09 },
                                                   new Balance { Title = 6711, SubTitle = 10 }
                                               },
                                           DateRange,
                                           1).ToArray();

            {
                var s = new Series("餐费与食品")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "生活费用",
                                Color = Color.Brown
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance1[i].Fund - balance1[0].Fund)
                                   + (balance2[i].Fund - balance2[0].Fund));
                yield return s;
            }
            {
                var s = new Series("水费与其他福利费")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "生活费用",
                                Color = Color.CornflowerBlue
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance3[i].Fund - balance3[0].Fund)
                                   + (balance4[i].Fund - balance4[0].Fund)
                                   - (balance2[i].Fund - balance2[0].Fund));
                yield return s;
            }
            {
                var s = new Series("主营其他业务成本")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "生活费用",
                                Color = Color.Chocolate
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance5[i].Fund - balance5[0].Fund));
                yield return s;
            }
            {
                var s = new Series("其他非折旧费用")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "生活费用",
                                Color = Color.Coral
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance6[i].Fund - balance6[0].Fund)
                                   - (balance3[i].Fund - balance3[0].Fund));
                yield return s;
            }
        }
    }

    internal sealed class 其他费用 : AccountingChart
    {
        public 其他费用(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("其他费用");
            SetupChartArea(ar);
            ar.AlignWithChartArea = "生活费用";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            //ar.AxisY.Maximum = 2000;
            return ar;
        }

        public override IEnumerable<Series> Gather()
        {
            var balance7 = Accountant.GetDailyBalance(
                                                      new[]
                                                          { new Balance { Title = 1601 }, new Balance { Title = 1701 } },
                                                      DateRange,
                                                      1).ToArray();
            var balance8 = Accountant.GetDailyBalance(
                                                      new[]
                                                          {
                                                              new Balance { Title = 1403 }, new Balance { Title = 1405 },
                                                              new Balance { Title = 1605 }
                                                          },
                                                      DateRange,
                                                      1).ToArray();


            {
                var s = new Series("购置原材料")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "其他费用",
                                Color = Color.Fuchsia
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance8.Length; i++)
                    s.Points.AddXY(
                                   balance8[i].Date.Value,
                                   (balance8[i].Fund - balance8[0].Fund));
                yield return s;
            }
            {
                var s = new Series("购置资产")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "其他费用",
                                Color = Color.Orchid
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance7.Length; i++)
                    s.Points.AddXY(
                                   balance7[i].Date.Value,
                                   (balance7[i].Fund - balance7[0].Fund));
                yield return s;
            }
        }
    }

    internal sealed class 负债 : AccountingChart
    {
        public 负债(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("负债");
            SetupChartArea(ar);
            ar.AlignWithChartArea = "生活费用";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            ar.AxisY.Maximum = 9000;
            return ar;
        }

        public override IEnumerable<Series> Gather()
        {
            var balance9 = Accountant.GetDailyBalance(
                                                      new[]
                                                          {
                                                              new Balance { Title = 2001 }, new Balance { Title = 2202 },
                                                              new Balance { Title = 2203 }, new Balance { Title = 2211 },
                                                              new Balance { Title = 2221, SubTitle = 05 },
                                                              new Balance { Title = 2241 }
                                                          },
                                                      DateRange).ToArray();
            var balance10 =
                Accountant.GetDailyBalance(new Balance { Title = 2241, SubTitle = 01 }, DateRange).ToArray();

            {
                var s = new Series("其他")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "负债",
                                Color = Color.Orange
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance9.Length; i++)
                    s.Points.AddXY(balance9[i].Date.Value, balance10[i].Fund - balance9[i].Fund);
                yield return s;
            }
            {
                var s = new Series("信用卡")
                            {
                                ChartType = SeriesChartType.StackedArea,
                                ChartArea = "负债",
                                Color = Color.OrangeRed
                            };
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance10.Length; i++)
                    s.Points.AddXY(balance10[i].Date.Value, -balance10[i].Fund);
                yield return s;
            }
        }
    }
}

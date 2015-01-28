using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console.Chart
{
    internal class AssetChart : AccountingChart
    {
        public AssetChart(Accountant helper, DateTime startDate, DateTime endDate, DateTime curDate)
            : base(helper, startDate, endDate, curDate) { }

        public override ChartArea Setup()
        {
            var ar = new ChartArea("总资产");
            SetupChartArea(ar);
            ar.AxisY.Minimum = 0;
            return ar;
        }

        private Series GatherDebt(string content, IEnumerable<Balance> filter, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.Line, ChartArea = "总资产" };
            var balances = Accountant.GetDailyBalance(filter, DateRange);
            foreach (var balance in balances)
                // ReSharper disable once AssignNullToNotNullAttribute
                // ReSharper disable once PossibleInvalidOperationException
                s.Points.AddXY(balance.Date.Value, -balance.Fund);
            s.Color = color;
            return s;
        }

        private Series GatherAsset(string content, IEnumerable<Balance> filter, Color color)
        {
            var s = new Series(content) { ChartType = SeriesChartType.StackedArea, ChartArea = "总资产" };
            var balances = Accountant.GetDailyBalance(filter, DateRange);
            foreach (var balance in balances)
                // ReSharper disable once AssignNullToNotNullAttribute
                // ReSharper disable once PossibleInvalidOperationException
                s.Points.AddXY(balance.Date.Value, balance.Fund);
            s.Color = color;
            return s;
        }

        public override IEnumerable<Series> GatherAsset()
        {
            yield return GatherAsset(
                                     "速动资产",
                                     new[]
                                         {
                                             new Balance { Title = 1001 },
                                             new Balance { Title = 1002 },
                                             new Balance { Title = 1012, SubTitle = 01 },
                                             new Balance { Title = 1012, SubTitle = 03 },
                                             new Balance { Title = 1012, SubTitle = 05 },
                                             new Balance { Title = 1101, Content = "华夏基金财富宝" }
                                         },
                                     Color.LawnGreen);
            yield return GatherAsset(
                                     "强流动性资产",
                                     new[]
                                         {
                                             new Balance { Title = 1101, Content = "中银活期宝" },
                                             new Balance { Title = 1101, Content = "广发基金天天红" },
                                             new Balance { Title = 1101, Content = "余额宝" },
                                             new Balance { Title = 1101, Content = "华夏基金财富宝" }
                                         },
                                     Color.YellowGreen);
            yield return GatherAsset(
                                     "中流动性资产",
                                     new[]
                                         {
                                             new Balance { Title = 1012, SubTitle = 04 },
                                             new Balance { Title = 1101, Content = "中银优选" },
                                             new Balance { Title = 1101, Content = "中银增利" },
                                             new Balance { Title = 1101, Content = "中银纯债C" },
                                             new Balance { Title = 1101, Content = "月息通 YAD14I3000" },
                                             new Balance { Title = 1101, Content = "月息通 YDK15A1651" },
                                             new Balance { Title = 1122 },
                                             new Balance { Title = 1221 },
                                             new Balance { Title = 1441 }
                                         },
                                     Color.Orange);
            yield return GatherAsset(
                                     "弱流动性资产",
                                     new[]
                                         {
                                             new Balance { Title = 1101, Content = "定存宝A" },
                                             new Balance { Title = 1101, Content = "富盈人生第34期" },
                                             new Balance { Title = 1101, Content = "民生加银理财月度1027期" },
                                             new Balance { Title = 1123 },
                                             new Balance { Title = 1405 },
                                             new Balance { Title = 1901 }
                                         },
                                     Color.BlueViolet);
            yield return GatherAsset(
                                     "无流动性资产",
                                     new[]
                                         {
                                             new Balance { Title = 1403 },
                                             new Balance { Title = 1412 },
                                             new Balance { Title = 1511 },
                                             new Balance { Title = 1601 },
                                             new Balance { Title = 1602 },
                                             new Balance { Title = 1603 },
                                             new Balance { Title = 1604 },
                                             new Balance { Title = 1605 },
                                             new Balance { Title = 1606 },
                                             new Balance { Title = 1701 },
                                             new Balance { Title = 1702 },
                                             new Balance { Title = 1703 }
                                         },
                                     Color.Maroon);
            yield return GatherDebt(
                                    "负债",
                                    new[]
                                        {
                                            new Balance { Title = 2001 },
                                            new Balance { Title = 2202 },
                                            new Balance { Title = 2203 },
                                            new Balance { Title = 2211 },
                                            new Balance { Title = 2221 },
                                            new Balance { Title = 2221, SubTitle = 05 },
                                            new Balance { Title = 2241 },
                                            new Balance { Title = 2241, SubTitle = 01 }
                                        },
                                    Color.Red);
        }
    }
}

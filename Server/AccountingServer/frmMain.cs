using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Microsoft.CSharp;

namespace AccountingServer
{
    public partial class frmMain : Form
    {
        private readonly BHelper m_BHelper;
        private DateTime startDate;
        private DateTime endDate;
        private bool m_Toggle21;

        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();

        //private delegate void SetCaptionDelegate(string str);
        private void SetCaption(string str)
        {
            if (InvokeRequired)
                Invoke(new Action(() => { Text = str; }));
            else
                Text = str;
        }

        public frmMain()
        {
            InitializeComponent();
            SetProcessDPIAware();
            WindowState = FormWindowState.Maximized;
            m_BHelper = new BHelper();
            m_BHelper.Connect("b1f6c1c4", "142857");
            MainBLL.Connect(
                            m_BHelper,
                            ep => SetCaption(String.Format("AccountingServer-{0}", ep.ToString())),
                            ep => { },
                            ep =>
                            {
                                SetCaption("AccountingServer");
                                MessageBox.Show(String.Format("与{0}的数据传输完毕！", ep.ToString()), @"数据传输");
                            });


            var curDate = DateTime.Now.Date;
            startDate = new DateTime(curDate.Year, curDate.Month - (curDate.Day >= 20 ? 0 : 1), 19);
            endDate = startDate.AddMonths(1);

            chart1.ChartAreas.SuspendUpdates();

            Action<ChartArea> setChartArea = area =>
                                             {
                                                 area.CursorX.Position = curDate.ToOADate();
                                                 area.AxisX.IsMarginVisible = false;
                                                 area.AxisX.Minimum = startDate.ToOADate();
                                                 area.AxisX.Maximum = endDate.ToOADate();
                                                 area.AxisX.MinorGrid.Enabled = true;
                                                 area.AxisX.MinorGrid.Interval = 1;
                                                 area.AxisX.MinorGrid.LineColor = Color.DarkGray;
                                                 area.AxisX.MajorGrid.Interval = 7;
                                                 area.AxisX.MajorGrid.LineWidth = 2;
                                                 area.AxisX.MajorGrid.IntervalOffset = -(int)startDate.DayOfWeek;
                                                 area.AxisX.LabelStyle = new LabelStyle
                                                                             {
                                                                                 Format = "MM-dd",
                                                                                 Font =
                                                                                     new Font(
                                                                                     "Consolas",
                                                                                     14,
                                                                                     GraphicsUnit.Pixel)
                                                                             };
                                             };

            var ar = chart1.ChartAreas[0];
            ar.Name = "投资资产";
            setChartArea(ar);

            ar = chart1.ChartAreas.Add("生活资产");
            setChartArea(ar);
            ar.AlignWithChartArea = "投资资产";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            ar.AxisY.Maximum = m_Toggle21 ? 1000 : 7000;
            /*ar.AxisY.ScaleBreakStyle.Enabled = true;
            ar.AxisY.ScaleBreakStyle.BreakLineStyle = BreakLineStyle.Ragged;
            ar.AxisY.ScaleBreakStyle.Spacing = 2;
            ar.AxisY.ScaleBreakStyle.LineWidth = 2;
            ar.AxisY.ScaleBreakStyle.LineColor = Color.Red;
            ar.AxisY.ScaleBreakStyle.CollapsibleSpaceThreshold = 50;*/
            //chart1.ChartAreas["Default"].AxisY.ScaleBreakStyle.IsStartedFromZero = AutoBool.Auto;

            ar = chart1.ChartAreas.Add("其他资产");
            setChartArea(ar);
            ar.AlignWithChartArea = "投资资产";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisX.Maximum = endDate.ToOADate();
            ar.AxisY.Minimum = 44000;
            ar.AxisY.Maximum = 62000;

            ar = chart1.ChartAreas.Add("生活费用");
            setChartArea(ar);
            ar.AxisY.Minimum = 0;
            ar.AxisY.Maximum = 2000;

            ar = chart1.ChartAreas.Add("其他费用");
            setChartArea(ar);
            ar.AlignWithChartArea = "生活费用";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            ar.AxisY.Maximum = 2000;

            ar = chart1.ChartAreas.Add("负债");
            setChartArea(ar);
            ar.AlignWithChartArea = "生活费用";
            ar.AlignmentOrientation = AreaAlignmentOrientations.Vertical;
            ar.AlignmentStyle = AreaAlignmentStyles.All;
            ar.AxisY.Minimum = 0;
            ar.AxisY.Maximum = 9000;

            chart1.Legends[0].Font = new Font("Microsoft YaHei Mono", 28, GraphicsUnit.Pixel);

            GatherData(startDate, endDate);

            chart1.ChartAreas.ResumeUpdates();
        }

        private void GatherData(DateTime startDate, DateTime endDate)
        {
            chart1.Series.Clear();

            Func<string, Balance, string, Series> action =
                (str, b, ch) =>
                {
                    var s = chart1.Series.Add(str);
                    s.ChartType = SeriesChartType.StackedArea;
                    s.ChartArea = ch;
                    var balances = m_BHelper.GetDailyBalance(b, startDate, endDate);
                    foreach (var balance in balances)
                        s.Points.AddXY(balance.Date.Value, balance.Fund);
                    return s;
                };
            Func<string, IEnumerable<Balance>, string, Series> actionMulti =
                (str, bs, ch) =>
                {
                    var s = chart1.Series.Add(str);
                    s.ChartType = SeriesChartType.StackedArea;
                    s.ChartArea = ch;
                    var balances = m_BHelper.GetDailyBalance(bs, startDate, endDate);
                    foreach (var balance in balances)
                        s.Points.AddXY(balance.Date.Value, balance.Fund);
                    return s;
                };

            action("学生卡", new Balance { Title = 1012, SubTitle = 05 }, "生活资产").Color = Color.LightSkyBlue;
            action("现金", new Balance { Title = 1001 }, "生活资产").Color = Color.PaleVioletRed;
            action("公交卡", new Balance { Title = 1012, SubTitle = 01 }, "生活资产").Color = Color.BlueViolet;
            action("借记卡", new Balance { Title = 1002 }, "生活资产").Color = Color.Chartreuse;

            Func<string, Balance, Series> actionT =
                (str, b) =>
                {
                    var s = chart1.Series.Add(str);
                    s.ChartType = SeriesChartType.StackedArea;
                    s.ChartArea = "投资资产";
                    var balances = m_BHelper.GetDailyBalance(b, startDate, endDate);
                    foreach (var balance in balances)
                        s.Points.AddXY(balance.Date.Value, balance.Fund);
                    return s;
                };
            actionT("存出投资款", new Balance { Title = 1012, SubTitle = 04 }).Color = Color.Maroon;
            actionT("中银活期宝", new Balance { Title = 1101, Content = "中银活期宝" }).Color = Color.SpringGreen;
            actionT("广发基金天天红", new Balance { Title = 1101, Content = "广发基金天天红" }).Color = Color.GreenYellow;
            actionT("余额宝", new Balance { Title = 1101, Content = "余额宝" }).Color = Color.MediumSpringGreen;
            actionT("中银优选", new Balance { Title = 1101, Content = "中银优选" }).Color = Color.DarkOrange;
            actionT("中银增利", new Balance { Title = 1101, Content = "中银增利" }).Color = Color.DarkMagenta;
            actionT("中银纯债C", new Balance { Title = 1101, Content = "中银纯债C" }).Color = Color.DarkOrchid;
            actionT("定存宝A", new Balance { Title = 1101, Content = "定存宝A" }).Color = Color.Navy;
            actionT("月息通 YAD14I3000", new Balance { Title = 1101, Content = "月息通 YAD14I3000" }).Color = Color.Olive;
            actionT("富盈人生第34期", new Balance { Title = 1101, Content = "富盈人生第34期" }).Color = Color.MidnightBlue;
            actionT("贵金属", new Balance { Title = 1441 }).Color = Color.Gold;

            {
                var s = chart1.Series.Add("固定资产和无形资产");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他资产";
                s.Color = Color.BlueViolet;
                var balances = m_BHelper.GetDailyBalance(
                                                         new[]
                                                             {
                                                                 new Balance { Title = 1601 },
                                                                 new Balance { Title = 1602 },
                                                                 new Balance { Title = 1603 },
                                                                 new Balance { Title = 1701 },
                                                                 new Balance { Title = 1702 },
                                                                 new Balance { Title = 1703 }
                                                             },
                                                         startDate,
                                                         endDate);
                foreach (var balance in balances)
                    s.Points.AddXY(balance.Date.Value, balance.Fund);
            }
            actionMulti(
                        "原材料等",
                        new[]
                            {
                                new Balance { Title = 1403 }, new Balance { Title = 1405 }, new Balance { Title = 1412 },
                                new Balance { Title = 1604 }, new Balance { Title = 1605 }
                            },
                        "其他资产").Color = Color.DarkViolet;
            actionMulti(
                        "其他（含应收）",
                        new[]
                            {
                                new Balance { Title = 1012, SubTitle = 03 }, new Balance { Title = 1122 },
                                new Balance { Title = 1221 }, new Balance { Title = 1511 }
                            },
                        "其他资产").Color = Color.MediumVioletRed;
            actionMulti(
                        "其他（含预付）",
                        new[]
                            { new Balance { Title = 1123 }, new Balance { Title = 1606 }, new Balance { Title = 1901 } },
                        "其他资产").Color = Color.Violet;


            var balance1 =
                m_BHelper.GetDailyBalance(new Balance { Title = 6602, SubTitle = 03 }, startDate, endDate, 1).ToArray();
            var balance2 =
                m_BHelper.GetDailyBalance(
                                          new Balance { Title = 6602, SubTitle = 06, Content = "食品" },
                                          startDate,
                                          endDate,
                                          1).ToArray();
            var balance3 =
                m_BHelper.GetDailyBalance(
                                          new Balance { Title = 6602, SubTitle = 01, Content = "水费" },
                                          startDate,
                                          endDate,
                                          1).ToArray();
            var balance4 =
                m_BHelper.GetDailyBalance(new Balance { Title = 6602, SubTitle = 06 }, startDate, endDate, 1).ToArray();
            var balance5 = m_BHelper.GetDailyBalance(
                                                     new[]
                                                         { new Balance { Title = 6401 }, new Balance { Title = 6402 } },
                                                     startDate,
                                                     endDate,
                                                     1).ToArray();
            var balance6 =
                m_BHelper.GetDailyBalance(
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
                                          startDate,
                                          endDate,
                                          1).ToArray();
            var balance7 = m_BHelper.GetDailyBalance(
                                                     new[]
                                                         { new Balance { Title = 1601 }, new Balance { Title = 1701 } },
                                                     startDate,
                                                     endDate,
                                                     1).ToArray();
            var balance8 = m_BHelper.GetDailyBalance(
                                                     new[]
                                                         {
                                                             new Balance { Title = 1403 }, new Balance { Title = 1405 },
                                                             new Balance { Title = 1605 }
                                                         },
                                                     startDate,
                                                     endDate,
                                                     1).ToArray();

            var max1 =
                balance1.Select(
                                (t1, i) =>
                                (t1.Fund - balance1[0].Fund) + (balance4[i].Fund - balance4[0].Fund) +
                                (balance5[i].Fund - balance5[0].Fund) + (balance6[i].Fund - balance6[0].Fund))
                        .Concat(new[] { 1000D })
                        .Max();
            chart1.ChartAreas["生活费用"].AxisY.Maximum = 200.0 * Math.Ceiling(max1 / 200.0);

            {
                var s = chart1.Series.Add("餐费与食品");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Brown;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance1[i].Fund - balance1[0].Fund)
                                   + (balance2[i].Fund - balance2[0].Fund));
            }
            {
                var s = chart1.Series.Add("水费与其他福利费");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.CornflowerBlue;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance3[i].Fund - balance3[0].Fund)
                                   + (balance4[i].Fund - balance4[0].Fund)
                                   - (balance2[i].Fund - balance2[0].Fund));
            }
            {
                var s = chart1.Series.Add("主营其他业务成本");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Chocolate;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance5[i].Fund - balance5[0].Fund));
            }
            {
                var s = chart1.Series.Add("其他非折旧费用");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Coral;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance6[i].Fund - balance6[0].Fund)
                                   - (balance3[i].Fund - balance3[0].Fund));
            }

            var max2 =
                balance8.Select(
                                (t1, i) =>
                                (t1.Fund - balance8[0].Fund) + (balance7[i].Fund - balance7[0].Fund))
                        .Concat(new[] { 1000D })
                        .Max();
            chart1.ChartAreas["其他费用"].AxisY.Maximum = 200.0 * Math.Ceiling(max2 / 200.0);

            {
                var s = chart1.Series.Add("购置原材料");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他费用";
                s.Color = Color.Fuchsia;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance8[i].Fund - balance8[0].Fund));
            }
            {
                var s = chart1.Series.Add("购置资产");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他费用";
                s.Color = Color.Orchid;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(
                                   balance1[i].Date.Value,
                                   (balance7[i].Fund - balance7[0].Fund));
            }

            var balance9 = m_BHelper.GetDailyBalance(
                                                     new[]
                                                         {
                                                             new Balance { Title = 2001 }, new Balance { Title = 2202 },
                                                             new Balance { Title = 2203 },
                                                             new Balance { Title = 2211 },
                                                             new Balance { Title = 2221, SubTitle = 05 },
                                                             new Balance { Title = 2241 }
                                                         },
                                                     startDate,
                                                     endDate).ToArray();
            var balance10 =
                m_BHelper.GetDailyBalance(new Balance { Title = 2241, SubTitle = 01 }, startDate, endDate).ToArray();

            var max3 =
                balance9.Select(
                                (t1, i) =>
                                -t1.Fund - balance10[i].Fund)
                        .Concat(new[] { 2000D })
                        .Max();
            chart1.ChartAreas["负债"].AxisY.Maximum = 500.0 * Math.Ceiling(max3 / 500.0);

            {
                var s = chart1.Series.Add("其他");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "负债";
                s.Color = Color.Orange;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(balance1[i].Date.Value, -balance9[i].Fund);
            }
            {
                var s = chart1.Series.Add("信用卡");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "负债";
                s.Color = Color.OrangeRed;
                s.Color = Color.FromArgb(200, s.Color);
                for (var i = 0; i < balance1.Length; i++)
                    s.Points.AddXY(balance1[i].Date.Value, -balance10[i].Fund);
            }
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    chart1.Series.SuspendUpdates();
                    GatherData(startDate, endDate);
                    chart1.Series.ResumeUpdates();
                    break;
                case Keys.R:
                    (new Report(m_BHelper, startDate.AddDays(1), endDate)).ToCSV(
                                                                                 @"C:\Users\b1f6c1c4\Desktop\Server\Report.csv");
                    break;
                case Keys.T:
                    m_Toggle21 ^= true;
                    chart1.ChartAreas["生活资产"].AxisY.Maximum = m_Toggle21 ? 1000 : 7000;
                    break;
                case Keys.C:
                    THUInfo.FetchFromInfo().ContinueWith(t => THUInfo.Analyze(t.Result));
                    break;
                case Keys.X:
                    m_BHelper.CopyDb();
                    break;
                case Keys.Space:
                    pictureBox1.Visible ^= true;
                    if (pictureBox1.Visible)
                    {
                        var qrCode = MainBLL.GetQRCode(pictureBox1.Width / 8, pictureBox1.Height / 8);
                        var bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        var g = Graphics.FromImage(bitmap);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(
                                    qrCode,
                                    new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height),
                                    new Rectangle(0, 0, pictureBox1.Width / 8, pictureBox1.Height / 8),
                                    GraphicsUnit.Pixel);
                        g.Dispose();
                        pictureBox1.Image = bitmap;
                    }
                    break;
            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                textBox1.SelectionStart = 0;
                textBox1.SelectionLength = textBox1.TextLength;
                return;
            }
            if (e.Shift &&
                e.KeyCode == Keys.Enter)
            {
                /*var id = textBox1.GetLineFromCharIndex(textBox1.SelectionStart);
                var s = textBox1.Lines[id];
                while (s == "" &&
                       id > 0)
                    s = textBox1.Lines[--id];*/
                //if (s == ".")
                /*{
                    var sb = new StringBuilder();
                    foreach (var voucher in m_BHelper.SelectVouchers(new Voucher { Date = DateTime.Now.Date }))
                        PresentVoucher(voucher, sb);
                    textBox1.AppendText(sb.ToString());
                }
                else*/
                {
                    var sb = new StringBuilder();
                    var voucher = ParseVoucher(textBox1.Text);
                    PresentVoucher(voucher,sb);
                    textBox1.AppendText("/*"+sb.ToString() + "*/");
                }
            }
        }

        private void PresentVoucher(Voucher voucher, StringBuilder sb)
        {
            sb.AppendLine("new Voucher");
            sb.AppendLine("    {{");
            sb.AppendFormat("        ID = {0}" + Environment.NewLine, ProcessString(voucher.ID));
            if (voucher.Date.HasValue)
                sb.AppendFormat("        Date = DateTime.Parse(\"{0:yyyy-MM-dd}\")," + Environment.NewLine, voucher.Date);
            else
                sb.AppendLine("        Date = null\",");
            if ((voucher.Type ?? VoucherType.Ordinal) != VoucherType.Ordinal)
                sb.AppendFormat("        Type = VoucherType.{0}," + Environment.NewLine, voucher.Type);
            if (voucher.Remark != null)
                sb.AppendFormat("        Remark = {0}" + Environment.NewLine, ProcessString(voucher.Remark));
            sb.AppendLine("        Details = new[]");
            sb.AppendLine("                        {");
            var flag = false;
            foreach (var detail in voucher.Details)
            {
                if (flag)
                    sb.AppendLine(",");
                sb.Append("                            new VoucherDetail { ");
                sb.AppendFormat("Title = {0:0}", detail.Title);
                if (detail.SubTitle.HasValue)
                    sb.AppendFormat(", SubTitle = {0:00}", detail.SubTitle);
                if (detail.Content != null)
                    sb.AppendFormat(", Content = {0}", ProcessString(detail.Content));
                if (detail.Fund.HasValue)
                    sb.AppendFormat(", Fund = {0}", detail.Fund);
                if (detail.Remark != null)
                    sb.AppendFormat(", Remark = {0}", ProcessString(detail.Remark));
                sb.Append(" }");
                flag = true;
            }
            sb.AppendLine(Environment.NewLine + "                        }" + Environment.NewLine + "    }");
            sb.AppendLine();
        }

        private Voucher ParseVoucher(string str)
        {
            var provider = new CSharpCodeProvider();
            var paras = new CompilerParameters
                            {
                                GenerateExecutable = false,
                                GenerateInMemory = true,
                                ReferencedAssemblies = { "AccountingServer.Entities.dll" }
                            };
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using AccountingServer.Entities;");
            sb.AppendLine("namespace AccountingServer.Dynamic");
            sb.AppendLine("{");
            sb.AppendLine("    public static class VoucherCreator");
            sb.AppendLine("    {");
            sb.AppendLine("        public static Voucher GetVoucher()");
            sb.AppendLine("        {");
            sb.AppendFormat("            return {0};" + Environment.NewLine, str);
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            var result = provider.CompileAssemblyFromSource(paras, sb.ToString());
            var resultAssembly = result.CompiledAssembly;
            return
                (Voucher)
                resultAssembly.GetType("AccountingServer.Dynamic.VoucherCreator")
                              .GetMethod("GetVoucher")
                              .Invoke(null, null);
        }

        private static string ProcessString(string s)
        {
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
            return "\"" + s + "\"";
        }

        private void textBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control) &&
                e.Delta != 0)
                textBox1.Font = new Font(
                    "Microsoft YaHei Mono",
                    textBox1.Font.Size +
                    e.Delta / 12F);
        }
    }
}

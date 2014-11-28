using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;

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
            if (this.InvokeRequired)
                this.Invoke(new Action(()=> { Text = str; }));
            else
                Text = str;
        }

        public frmMain()
        {
            InitializeComponent();
            SetProcessDPIAware();
            WindowState=FormWindowState.Maximized;
            m_BHelper = new BHelper();
            m_BHelper.Connect("b1f6c1c4", "142857");
            MainBLL.Connect(
                            m_BHelper,
                            ep => SetCaption(String.Format("AccountingServer-{0}", ep.ToString())),
                            ep =>{},
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

            Func<string, IEnumerable<decimal>, string, Series> action = (str, titles, ch) =>
                                                                        {
                                                                            var s = chart1.Series.Add(str);
                                                                            s.ChartType = SeriesChartType.StackedArea;
                                                                            s.ChartArea = ch;
                                                                            var balance = m_BHelper.GetDailyBalance(
                                                                                                                    titles,
                                                                                                                    null,
                                                                                                                    startDate,
                                                                                                                    endDate);
                                                                            for (var dt = startDate;
                                                                                 dt <= endDate;
                                                                                 dt = dt.AddDays(1))
                                                                                s.Points.AddXY(dt, balance[dt]);
                                                                            return s;
                                                                        };

            action("学生卡", new[] {(decimal)1012.05}, "生活资产").Color = Color.LightSkyBlue;
            action("现金", new[] { (decimal)1001.00 }, "生活资产").Color = Color.PaleVioletRed;
            action("公交卡", new[] { (decimal)1012.01 }, "生活资产").Color = Color.BlueViolet;
            action("借记卡", new[] { (decimal)1002.00 }, "生活资产").Color = Color.Chartreuse;

            Func<string, string, IEnumerable<decimal>, Series> actionT = (str, rmk, titles) =>
                                                                         {
                                                                             var s = chart1.Series.Add(str);
                                                                             s.ChartType = SeriesChartType.StackedArea;
                                                                             s.ChartArea = "投资资产";
                                                                             var balance = m_BHelper.GetDailyBalance(
                                                                                                                     titles,
                                                                                                                     rmk,
                                                                                                                     startDate,
                                                                                                                     endDate);
                                                                             for (var dt = startDate;
                                                                                  dt <= endDate;
                                                                                  dt = dt.AddDays(1))
                                                                                 s.Points.AddXY(dt, balance[dt]);
                                                                             return s;
                                                                         };
            actionT("存出投资款", null, new[] {(decimal)1012.04}).Color = Color.Maroon;
            actionT("中银活期宝", "中银活期宝", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.SpringGreen;
            actionT("广发基金天天红", "广发基金天天红", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.GreenYellow;
            actionT("余额宝", "余额宝", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.MediumSpringGreen;
            actionT("民生加银理财月度1027期", "民生加银理财月度1027期", new[] { (decimal)1101.01, (decimal)1101.02 }).Color = Color.Indigo;
            actionT("中银优选", "中银优选", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.DarkOrange;
            actionT("中银增利", "中银增利", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.DarkMagenta;
            actionT("中银纯债C", "中银纯债C", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.DarkOrchid;
            actionT("定存宝A", "定存宝A", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.Navy;
            actionT("月息通 YAD14I3000", "月息通 YAD14I3000", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.Olive;
            actionT("富盈人生第34期", "富盈人生第34期", new[] {(decimal)1101.01, (decimal)1101.02}).Color = Color.MidnightBlue;
            actionT("贵金属", null, new[] {(decimal)1441.00}).Color = Color.Gold;

            {
                var s = chart1.Series.Add("固定资产和无形资产");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他资产";
                s.Color = Color.BlueViolet;
                var balance = m_BHelper.GetDailyBalance(
                                                        new[]
                                                            {
                                                                (decimal)1601.00, (decimal)1602.00, (decimal)1603.00,
                                                                (decimal)1701.00, (decimal)1702.00, (decimal)1703.00
                                                            },
                                                        null,
                                                        startDate,
                                                        endDate);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, balance[dt]);
            }
            action(
                   "原材料等",
                   new[] {(decimal)1403.00, (decimal)1405.00, (decimal)1412.00, (decimal)1604.00, (decimal)1605.00},
                   "其他资产").Color = Color.DarkViolet;
            action(
                   "其他（含应收）",
                   new[] {(decimal)1012.03, (decimal)1122.00, (decimal)1221.00, (decimal)1511.00},
                   "其他资产").Color = Color.MediumVioletRed;
            action("其他（含预付）", new[] {(decimal)1123.00, (decimal)1606.00, (decimal)1901.00}, "其他资产").Color = Color.Violet;


            var balance1 = m_BHelper.GetDailyBalance(new[] {(decimal)6602.03}, null, startDate, endDate, 1);
            var balance2 = m_BHelper.GetDailyBalance(new[] {(decimal)6602.06}, "食品", startDate, endDate, 1);
            var balance3 = m_BHelper.GetDailyBalance(new[] {(decimal)6602.01}, "水费", startDate, endDate, 1);
            var balance4 = m_BHelper.GetDailyBalance(new[] {(decimal)6602.06}, null, startDate, endDate, 1);
            var balance5 = m_BHelper.GetDailyBalance(
                                                     new[] {(decimal)6401.00, (decimal)6402.00},
                                                     null,
                                                     startDate,
                                                     endDate,
                                                     1);
            var balance6 =
                m_BHelper.GetDailyBalance(
                                          new[]
                                              {
                                                  (decimal)6602.01, (decimal)6602.02, (decimal)6602.04, (decimal)6602.05,
                                                  (decimal)6602.08, (decimal)6602.09, (decimal)6602.10, (decimal)6602.99,
                                                  (decimal)6603.00, (decimal)6711.08, (decimal)6711.09, (decimal)6711.10
                                              },
                                          null,
                                          startDate,
                                          endDate,
                                          1);
            var balance7 = m_BHelper.GetDailyBalance(
                                                     new[] {(decimal)1601.00, (decimal)1701.00},
                                                     null,
                                                     startDate,
                                                     endDate,
                                                     1);
            var balance8 = m_BHelper.GetDailyBalance(
                                                     new[] {(decimal)1403.00, (decimal)1405.00, (decimal)1605.00},
                                                     null,
                                                     startDate,
                                                     endDate,
                                                     1);

            var max1 = (decimal)1000;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                var t = (balance1[dt] - balance1[startDate]) + (balance4[dt] - balance4[startDate]) +
                        (balance5[dt] - balance5[startDate]) + (balance6[dt] - balance6[startDate]);
                if (t > max1)
                    max1 = t;
            }
            chart1.ChartAreas["生活费用"].AxisY.Maximum = 200.0 * Math.Ceiling((double)max1 / 200.0);

            {
                var s = chart1.Series.Add("餐费与食品");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Brown;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(
                                   dt,
                                   (balance1[dt] - balance1[startDate])
                                   + (balance2[dt] - balance2[startDate]));
            }
            {
                var s = chart1.Series.Add("水费与其他福利费");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.CornflowerBlue;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(
                                   dt,
                                   (balance3[dt] - balance3[startDate])
                                   + (balance4[dt] - balance4[startDate])
                                   - (balance2[dt] - balance2[startDate]));
            }
            {
                var s = chart1.Series.Add("主营其他业务成本");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Chocolate;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, (balance5[dt] - balance5[startDate]));
            }
            {
                var s = chart1.Series.Add("其他非折旧费用");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "生活费用";
                s.Color = Color.Coral;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(
                                   dt,
                                   (balance6[dt] - balance6[startDate])
                                   - (balance3[dt] - balance3[startDate]));
            }

            var max2 = (decimal)1000;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                var t = (balance8[dt] - balance8[startDate]) + (balance7[dt] - balance7[startDate]);
                if (t > max2)
                    max2 = t;
            }
            chart1.ChartAreas["其他费用"].AxisY.Maximum = 200.0 * Math.Ceiling((double)max2 / 200.0);

            {
                var s = chart1.Series.Add("购置原材料");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他费用";
                s.Color = Color.Fuchsia;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, (balance8[dt] - balance8[startDate]));
            }
            {
                var s = chart1.Series.Add("购置资产");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "其他费用";
                s.Color = Color.Orchid;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, (balance7[dt] - balance7[startDate]));
            }

            var balance9 = m_BHelper.GetDailyBalance(
                                                    new[]
                                                            {
                                                                (decimal)2001.00, (decimal)2202.00, (decimal)2203.00,
                                                                (decimal)2211.00, (decimal)2221.05, (decimal)2241.00
                                                            },
                                                    null,
                                                    startDate,
                                                    endDate);
            var balance10 = m_BHelper.GetDailyBalance(new[] { (decimal)2241.01 }, null, startDate, endDate);

            var max3 = (decimal)2000;
            for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
            {
                var t = -balance9[dt] - balance10[dt];
                if (t > max3)
                    max3 = t;
            }
            chart1.ChartAreas["负债"].AxisY.Maximum = 500.0 * Math.Ceiling((double)max3 / 500.0);

            {
                var s = chart1.Series.Add("其他");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "负债";
                s.Color = Color.Orange;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, -balance9[dt]);
            }
            {
                var s = chart1.Series.Add("信用卡");
                s.ChartType = SeriesChartType.StackedArea;
                s.ChartArea = "负债";
                s.Color = Color.OrangeRed;
                s.Color = Color.FromArgb(200, s.Color);
                for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
                    s.Points.AddXY(dt, -balance10[dt]);
            }
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                chart1.Series.SuspendUpdates();
                GatherData(startDate, endDate);
                chart1.Series.ResumeUpdates();
            }
            else if (e.KeyCode == Keys.R)
            {
                (new Report(m_BHelper, startDate.AddDays(1), endDate)).ToCSV(@"C:\Users\b1f6c1c4\Desktop\Server\Report.csv");
            }
            else if (e.KeyCode == Keys.T)
            {
                m_Toggle21 ^= true;

                chart1.ChartAreas["生活资产"].AxisY.Maximum = m_Toggle21 ? 1000 : 7000;
            }
            else if (e.KeyCode == Keys.C)
            {
                FetchFromInfo().ContinueWith(t=>Analyze(t.Result));
            }
            else if (e.KeyCode == Keys.Space)
            {
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
            }
        }

        private static async Task<Stream> FetchFromInfo()
        {
            var cookie = new CookieContainer();
            {
                var req = WebRequest.Create(@"http://info.tsinghua.edu.cn/") as HttpWebRequest;

                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "GET";
                req.CookieContainer = cookie;
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                var res = await req.GetResponseAsync();
                var responseStream = res.GetResponseStream();
                if (responseStream != null)
                    Debug.Print(SaveTempFile(responseStream));
                    //using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("GB2312")))
                    //    Debug.Print(sr.ReadToEnd());
            }
            {
                var buf = Encoding.GetEncoding("GB2312").GetBytes("redirect=NO&userName=2014010914&password=MTQyODU3&x=5&y=5");
                var req = WebRequest.Create(@"https://info.tsinghua.edu.cn:443/Login") as HttpWebRequest;

                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "POST";
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
                req.GetRequestStream().Flush();
                req.CookieContainer = cookie;
                req.Referer = @"http://info.tsinghua.edu.cn/";
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                var res = await req.GetResponseAsync();
                var responseStream = res.GetResponseStream();
                if (responseStream != null)
                    using (var sr = new StreamReader(responseStream, Encoding.GetEncoding("GB2312")))
                        Debug.Print(sr.ReadToEnd());
            }
            return null;
            {
                var buf = Encoding.GetEncoding("GB2312").GetBytes("begindate=&enddate=&transtype=&dept=");
                var req = WebRequest.Create(@"http://ecard.tsinghua.edu.cn/user/ExDetailsDown.do?") as HttpWebRequest;
                
                Debug.Assert(req != null, "req != null");

                req.KeepAlive = false;
                req.Method = "POST";
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
                req.CookieContainer = cookie;
                req.Accept = "text/html, application/xhtml+xml, */*";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";
                req.Credentials = CredentialCache.DefaultCredentials;
                req.AllowAutoRedirect = false;
                req.Referer = @"http://ecard.tsinghua.edu.cn/user/UserExDetails.do?dir=jump&currentPage=1";
                
                var res = await req.GetResponseAsync();
                return res.GetResponseStream();
            }
        }

        private void Analyze(Stream stream)
        {
            return;
            var tempFileName = SaveTempFile(stream);

            var excel = new Microsoft.Office.Interop.Excel.ApplicationClass();
            excel.Workbooks.Add(true);
            excel.Visible = true;
            excel.Workbooks.Open(
                                 tempFileName,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value,
                                 System.Reflection.Missing.Value);
        }

        private static string SaveTempFile(Stream stream)
        {
            var buf = new byte[1024];
            var tempFileName = Path.GetTempPath() + Path.GetRandomFileName() + ".xls";
            using (var s = File.OpenWrite(tempFileName))
            {
                var length = stream.Read(buf, 0, 1024);
                while (length > 0)
                {
                    s.Write(buf, 0, length);
                    length = stream.Read(buf, 0, 1024);
                }

                s.Flush();
            }
            return tempFileName;
        }
    }
}

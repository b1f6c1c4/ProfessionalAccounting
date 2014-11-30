using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.BLL;
using AccountingServer.Chart;
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

        private AccountingConsole m_Console;
        private AccountingChart[] m_Charts;

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
            m_BHelper.Connect("b1f6c1c4", "");
            MainBLL.Connect(
                            m_BHelper,
                            ep => SetCaption(String.Format("AccountingServer-{0}", ep.ToString())),
                            ep => { },
                            ep =>
                            {
                                SetCaption("AccountingServer");
                                MessageBox.Show(String.Format("与{0}的数据传输完毕！", ep.ToString()), @"数据传输");
                            });

            m_Console=new AccountingConsole(m_BHelper);

            var curDate = DateTime.Now.Date;
            startDate = new DateTime(curDate.Year, curDate.Month - (curDate.Day >= 20 ? 0 : 1), 19);
            endDate = startDate.AddMonths(1);

            m_Charts = new AccountingChart[]
                           {
                               new 投资资产(m_BHelper, startDate, endDate, curDate) ,
                               new 生活资产(m_BHelper, startDate, endDate, curDate) , 
                               new 其他资产(m_BHelper, startDate, endDate, curDate) , 
                               new 生活费用(m_BHelper, startDate, endDate, curDate) , 
                               new 其他费用(m_BHelper, startDate, endDate, curDate) , 
                               new 负债(m_BHelper, startDate, endDate, curDate) 
                           };

            chart1.ChartAreas.SuspendUpdates();

            chart1.ChartAreas.Clear();
            foreach (var chart in m_Charts)
                chart1.ChartAreas.Add(chart.Setup());
            
            chart1.Legends[0].Font = new Font("Microsoft YaHei Mono", 28, GraphicsUnit.Pixel);

            GatherData();

            chart1.ChartAreas.ResumeUpdates();
        }

        private void GatherData()
        {
            chart1.Series.Clear();

            foreach (var series in m_Charts.SelectMany(chart => chart.Gather()))
                chart1.Series.Add(series);
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    tabControl1.SelectTab(tabPage1);
                    chart1.Series.SuspendUpdates();
                    GatherData();
                    chart1.Series.ResumeUpdates();
                    break;
                case Keys.F2:
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
                    tabControl1.SelectTab(tabPage2);
                    break;
                case Keys.F3:
                    tabControl1.SelectTab(tabPage3);
                    break;
                    //case Keys.T:
                    //    m_Toggle21 ^= true;
                    //    chart1.ChartAreas["生活资产"].AxisY.Maximum = m_Toggle21 ? 1000 : 7000;
                    //    break;
                    //case Keys.C:
                    //    THUInfo.FetchFromInfo().ContinueWith(t => THUInfo.Analyze(t.Result));
                    //    break;
                    //case Keys.X:
                    //    m_BHelper.CopyDb();
                    //    break;
            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                textBox1.SelectionStart = 0;
                textBox1.SelectionLength = textBox1.Text.Length;
                return;
            }
            if (e.Shift &&
                e.KeyCode == Keys.Enter)
            {
                var id = textBox1.GetLineFromCharIndex(textBox1.SelectionStart)-1;
                var s = textBox1.Lines[id];
                var sb = new StringBuilder();
                //foreach (var voucher in )
                //    PresentVoucher(voucher, sb);
                textBox1.SelectedText = sb.ToString();
                //else
                //{
                //    var sb = new StringBuilder();
                //    var voucher = ParseVoucher(textBox1.Text);
                //    PresentVoucher(voucher,sb);
                //    textBox1.AppendText("/*"+sb.ToString() + "*/");
                //}
            }
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

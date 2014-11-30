using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using AccountingServer.BLL;
using AccountingServer.Chart;

namespace AccountingServer
{
    public partial class frmMain : Form
    {
        private readonly Accountant m_Accountant;
        private readonly MobileComm m_Mobile;

        private readonly AccountingConsole m_Console;
        private readonly AccountingChart[] m_Charts;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

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

            m_Accountant = new Accountant();
            m_Accountant.Connect("b1f6c1c4", "");

            m_Mobile = new MobileComm();

            m_Mobile.Connect(
                            m_Accountant,
                            ep => SetCaption(String.Format("AccountingServer-{0}", ep.ToString())),
                            ep => { },
                            ep =>
                            {
                                SetCaption("AccountingServer");
                                MessageBox.Show(String.Format("与{0}的数据传输完毕！", ep.ToString()), @"数据传输");
                            });

            m_Console=new AccountingConsole(m_Accountant);

            var curDate = DateTime.Now.Date;
            DateTime startDate = new DateTime(curDate.Year, curDate.Month - (curDate.Day >= 20 ? 0 : 1), 19);
            DateTime endDate = startDate.AddMonths(1);

            m_Charts = new AccountingChart[]
                           {
                               new 投资资产(m_Accountant, startDate, endDate, curDate) ,
                               new 生活资产(m_Accountant, startDate, endDate, curDate) , 
                               new 其他资产(m_Accountant, startDate, endDate, curDate) , 
                               new 生活费用(m_Accountant, startDate, endDate, curDate) , 
                               new 其他费用(m_Accountant, startDate, endDate, curDate) , 
                               new 负债(m_Accountant, startDate, endDate, curDate) 
                           };

            chart1.ChartAreas.SuspendUpdates();

            chart1.ChartAreas.Clear();
            foreach (var chart in m_Charts)
                chart1.ChartAreas.Add(chart.Setup());
            
            chart1.Legends[0].Font = new Font("Microsoft YaHei Mono", 12, GraphicsUnit.Pixel);

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
                        var qrCode = m_Mobile.GetQRCode(pictureBox1.Width / 8, pictureBox1.Height / 8);
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
                    //    m_Accountant.CopyDb();
                    //    break;
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!ModifierKeys.HasFlag(Keys.Shift) && ! ModifierKeys.HasFlag(Keys.Control))
                return;
            if ((e.KeyChar == '\r' || e.KeyChar == '\n'))
                e.Handled = true;
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                textBox1.SelectionStart = 0;
                textBox1.SelectionLength = textBox1.Text.Length;
                return;
            }
            if (e.Control && e.Shift &&
                e.KeyCode == Keys.Enter)
            {
                var id = textBox1.SelectionStart + textBox1.SelectionLength;
                do
                {
                    id = textBox1.Text.LastIndexOf("new Voucher", id, StringComparison.Ordinal);
                    if (id == -1)
                        return;
                } while (textBox1.Text.Substring(id, 17) == "new VoucherDetail");
                var nested = 0;
                var flag = false;
                var i = id + 11;
                for (; i < textBox1.Text.Length; i++)
                    if (textBox1.Text[i] == '{')
                    {
                        nested++;
                        flag = true;
                    }
                    else if (textBox1.Text[i] == '}')
                        if (--nested == 0 && flag)
                            break;
                if (!flag)
                    return;

                try
                {
                    var s = textBox1.Text.Substring(id, i - id + 1);
                    var result = m_Console.ExecuteUpsert(s);
                    textBox1.Text = textBox1.Text.Remove(id, i - id + 1).Insert(id, result);
                    textBox1.SelectionStart = id;
                    textBox1.SelectionLength = result.Length;
                }
                catch (Exception exception)
                {
                    textBox1.SelectionStart = id;
                    textBox1.SelectionLength = i - id + 1;
                    textBox1.Text = textBox1.Text.Insert(i, exception.ToString());
                }

                textBox1.ScrollToCaret();
                return;
            }
            if (e.Control &&
                e.KeyCode == Keys.Enter)
            {
                var sid = textBox1.SelectionStart;
                var id = textBox1.GetLineFromCharIndex(sid);
                try
                {
                    var result = m_Console.Execute(textBox1.Lines[id]);
                    textBox1.Text = textBox1.Text.Insert(sid, Environment.NewLine + result);
                }
                catch (Exception exception)
                {
                    textBox1.Text = textBox1.Text.Insert(sid, Environment.NewLine + exception.ToString());
                }
                textBox1.SelectionStart = sid;
            }
        }


        private void textBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control) &&
                e.Delta != 0)
                textBox1.Font = new Font(
                    "Microsoft YaHei Mono",
                    textBox1.Font.Size +
                    e.Delta / 18F);
        }
    }
}

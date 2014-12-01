using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
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

        private bool m_Editable;

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
            chart1.Dock = DockStyle.Fill;
            pictureBox1.Dock = DockStyle.Fill;
            textBoxResult.Dock = DockStyle.Fill;

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

            m_Console = new AccountingConsole(m_Accountant);

            var curDate = DateTime.Now.Date;
            var startDate = new DateTime(curDate.Year, curDate.Month - (curDate.Day >= 20 ? 0 : 1), 19);
            var endDate = startDate.AddMonths(1);

            m_Charts = new AccountingChart[]
                           {
                               new 投资资产(m_Accountant, startDate, endDate, curDate),
                               new 生活资产(m_Accountant, startDate, endDate, curDate),
                               new 其他资产(m_Accountant, startDate, endDate, curDate),
                               new 生活费用(m_Accountant, startDate, endDate, curDate),
                               new 其他费用(m_Accountant, startDate, endDate, curDate),
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
        
        private bool GetVoucherCode(out int begin, out int end)
        {
            end = -1;
            begin = textBoxResult.SelectionStart + textBoxResult.SelectionLength;
            do
            {
                begin = textBoxResult.Text.LastIndexOf("new Voucher", begin, StringComparison.Ordinal);
                if (begin == -1)
                    return true;
            } while (textBoxResult.Text.Substring(begin, 17) == "new VoucherDetail");
            var nested = 0;
            end = begin + 11;
            for (; end < textBoxResult.Text.Length; end++)
                if (textBoxResult.Text[end] == '{')
                    nested++;
                else if (textBoxResult.Text[end] == '}')
                    if (--nested == 0)
                        break;
            return false;
        }

        private void textBoxResult_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!m_Editable)
                return;
            if (!ModifierKeys.HasFlag(Keys.Alt))
                return;
            if ((e.KeyChar == '\r' || e.KeyChar == '\n'))
                e.Handled = true;
        }

        private void textBoxResult_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                textBoxCommand.Focus();
                textBoxCommand.SelectionStart = 0;
                textBoxCommand.SelectionLength = textBoxCommand.Text.Length;
                return;
            }
            if (!m_Editable)
                return;
            if (e.Alt &&
                e.KeyCode == Keys.Delete)
            {
                int begin;
                int end;
                if (GetVoucherCode(out begin, out end))
                    return;

                try
                {
                    var s = textBoxResult.Text.Substring(begin, end - begin + 1);
                    if (!m_Console.ExecuteRemoval(s))
                        throw new Exception();
                    textBoxResult.Text = textBoxResult.Text.Insert(end, "*/").Insert(begin, "/*");
                    textBoxResult.SelectionStart = begin;
                    textBoxResult.SelectionLength = end - begin + 5;
                }
                catch (Exception exception)
                {
                    textBoxResult.Text = textBoxResult.Text.Insert(end, exception.ToString());
                    textBoxResult.SelectionStart = begin;
                    textBoxResult.SelectionLength = end - begin + 1;
                }

                textBoxResult.ScrollToCaret();
            }
            else if (e.Alt &&
                e.KeyCode == Keys.Enter)
            {
                int begin;
                int end;
                if (GetVoucherCode(out begin, out end))
                    return;

                try
                {
                    var s = textBoxResult.Text.Substring(begin, end - begin + 1);
                    var result = m_Console.ExecuteUpsert(s);
                    textBoxResult.Text = textBoxResult.Text.Remove(begin, end - begin + 1).Insert(begin, result);
                    textBoxResult.SelectionStart = begin;
                    textBoxResult.SelectionLength = result.Length;
                }
                catch (Exception exception)
                {
                    textBoxResult.Text = textBoxResult.Text.Insert(end, exception.ToString());
                    textBoxResult.SelectionStart = begin;
                    textBoxResult.SelectionLength = end - begin + 1;
                }

                textBoxResult.ScrollToCaret();
            }
        }


        private void textBoxResult_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control) &&
                e.Delta != 0)
                textBoxResult.Font = new Font(
                    "Microsoft YaHei Mono",
                    textBoxResult.Font.Size +
                    e.Delta / 18F);
        }

        private void textBoxCommand_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r' &&
                e.KeyChar != '\n')
            {
                textBoxCommand.BackColor = Color.White;
                return;
            }
            try
            {
                var result = m_Console.Execute(textBoxCommand.Text, out m_Editable);
                textBoxResult.Text = result;
                textBoxResult.Focus();
                textBoxResult.SelectionLength = 0;
                textBoxCommand.BackColor = m_Editable ? Color.FromArgb(75, 255, 75) : Color.FromArgb(255, 255, 75);
            }
            catch (Exception exception)
            {
                textBoxResult.Text = exception.ToString();
                textBoxCommand.BackColor = Color.FromArgb(255, 70, 70);
            }
        }
    }
}

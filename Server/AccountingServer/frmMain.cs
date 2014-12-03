using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AccountingServer.BLL;
using AccountingServer.Console;

namespace AccountingServer
{
    // ReSharper disable once InconsistentNaming
    public partial class frmMain : Form
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Accountant m_Accountant;

        private readonly AccountingConsole m_Console;
        //private readonly AccountingChart[] m_Charts;

        private bool m_Editable;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public frmMain()
        {
            InitializeComponent();
            chart1.Dock = DockStyle.Fill;
            pictureBox1.Dock = DockStyle.Fill;
            textBoxResult.Dock = DockStyle.Fill;

            SetProcessDPIAware();

            m_Accountant = new Accountant();

            m_Console = new AccountingConsole(m_Accountant);
            m_Console.PresentQRCode += qrCode =>
                                       {
                                           if (qrCode == null)
                                           {
                                               pictureBox1.Visible = false;
                                               textBoxResult.Visible = true;
                                           }
                                           else
                                           {
                                               pictureBox1.Image = qrCode;
                                               pictureBox1.Visible = true;
                                               textBoxResult.Visible = false;
                                           }
                                       };

            //var curDate = DateTime.Now.Date;
            //var startDate = new DateTime(curDate.Year, curDate.Month - (curDate.Day >= 20 ? 0 : 1), 19);
            //var endDate = startDate.AddMonths(1);

            //m_Charts = new AccountingChart[]
            //               {
            //                   new 投资资产(m_Accountant, startDate, endDate, curDate),
            //                   new 生活资产(m_Accountant, startDate, endDate, curDate),
            //                   new 其他资产(m_Accountant, startDate, endDate, curDate),
            //                   new 生活费用(m_Accountant, startDate, endDate, curDate),
            //                   new 其他费用(m_Accountant, startDate, endDate, curDate),
            //                   new 负债(m_Accountant, startDate, endDate, curDate)
            //               };

            //chart1.ChartAreas.SuspendUpdates();

            //chart1.ChartAreas.Clear();
            //foreach (var chart in m_Charts)
            //    chart1.ChartAreas.Add(chart.Setup());

            //chart1.Legends[0].Font = new Font("Microsoft YaHei Mono", 12, GraphicsUnit.Pixel);

            //GatherData();

            //chart1.ChartAreas.ResumeUpdates();
        }

        //private void GatherData()
        //{
        //    chart1.Series.Clear();

        //    foreach (var series in m_Charts.SelectMany(chart => chart.Gather()))
        //        chart1.Series.Add(series);
        //}

        private bool GetVoucherCode(out int begin, out int end)
        {
            end = -1;
            begin = textBoxResult.SelectionStart + textBoxResult.SelectionLength;
            do
            {
                begin = textBoxResult.Text.LastIndexOf("new Voucher", begin, StringComparison.Ordinal);
                if (begin == -1)
                    return false;
            } while (textBoxResult.Text.Substring(begin, 17) == "new VoucherDetail");
            var nested = 0;
            end = begin + 11;
            for (; end < textBoxResult.Text.Length; end++)
                if (textBoxResult.Text[end] == '{')
                    nested++;
                else if (textBoxResult.Text[end] == '}')
                    if (--nested == 0)
                        break;

            if (end + 2 < textBoxResult.Text.Length &&
                textBoxResult.Text[end + 1] == '\r' &&
                textBoxResult.Text[end + 2] == '\n')
                end += 2;
            return true;
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

        private void textBoxCommand_TextChanged(object sender, EventArgs e)
        {
            textBoxCommand.BackColor = textBoxCommand.Text.StartsWith("+") ? Color.Black : Color.White;
            textBoxCommand.ForeColor = textBoxCommand.Text.StartsWith("+") ? Color.White : Color.Black;
        }

        private bool PerformUpsert()
        {
            int begin;
            int end;
            if (!GetVoucherCode(out begin, out end))
                return false;

            try
            {
                var s = textBoxResult.Text.Substring(begin, end - begin + 1);
                var result = m_Console.ExecuteUpsert(s);
                textBoxResult.Text = textBoxResult.Text.Remove(begin, end - begin + 1)
                                                  .Insert(begin, result);
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
            return true;
        }

        private bool PerformRemoval()
        {
            int begin;
            int end;
            if (!GetVoucherCode(out begin, out end))
                return false;

            try
            {
                var s = textBoxResult.Text.Substring(begin, end - begin + 1);
                if (!m_Console.ExecuteRemoval(s))
                    throw new Exception();
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (textBoxResult.Text[end] == '}')
                    textBoxResult.Text = textBoxResult.Text.Insert(end + 1, "*/").Insert(begin, "/*");
                else //if (textBoxResult.Text[end] == '\n')
                    textBoxResult.Text = textBoxResult.Text.Insert(end - 1, "*/").Insert(begin, "/*");
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
            return true;
        }

        private bool ExecuteCommand()
        {
            try
            {
                var text = textBoxCommand.Text;
                if (text.StartsWith("+"))
                {
                    var result = m_Console.Execute(text.Substring(1), out m_Editable);
                    if (result == null)
                        return true;

                    var lng = textBoxResult.Text.Length;
                    textBoxResult.Focus();
                    textBoxResult.AppendText(result);
                    textBoxResult.SelectionStart = lng;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = m_Editable
                                                   ? Color.FromArgb(0, 200, 0)
                                                   : Color.FromArgb(200, 220, 0);

                    if (result.StartsWith("OK"))
                        FocusTextBoxCommand();
                }
                else
                {
                    var result = m_Console.Execute(text, out m_Editable);
                    if (result == null)
                        return true;

                    textBoxResult.Focus();
                    textBoxResult.Text = result;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = m_Editable
                                                   ? Color.FromArgb(75, 255, 75)
                                                   : Color.FromArgb(250, 250, 0);

                    if (result.StartsWith("OK"))
                        FocusTextBoxCommand();
                }
                return true;
            }
            catch (Exception exception)
            {
                textBoxResult.Text = exception.ToString();
                textBoxCommand.BackColor = Color.FromArgb(255, 70, 70);
                return false;
            }
        }

        private void FocusTextBoxCommand()
        {
            textBoxCommand.Focus();
            if (textBoxCommand.Text.StartsWith("+"))
                if (textBoxCommand.SelectionLength != textBoxCommand.Text.Length - 1 ||
                    textBoxCommand.SelectionStart != 1)
                {
                    textBoxCommand.SelectionStart = 1;
                    textBoxCommand.SelectionLength = textBoxCommand.Text.Length - 1;
                }

            textBoxCommand.SelectionStart = 0;
            textBoxCommand.SelectionLength = textBoxCommand.Text.Length;
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (textBoxCommand.Focused)
            {
                if (keyData == Keys.Escape)
                {
                    FocusTextBoxCommand();
                    return true;
                }

                if (keyData == Keys.Enter)
                    if (ExecuteCommand())
                        return true;
            }
            else if (textBoxResult.Focused)
            {
                if (keyData == Keys.Escape)
                {
                    FocusTextBoxCommand();
                    return true;
                }

                if (m_Editable)
                {
                    if (keyData.HasFlag(Keys.Enter) &&
                        keyData.HasFlag(Keys.Alt))
                        if (PerformUpsert())
                            return true;
                    if (keyData.HasFlag(Keys.Delete) &&
                        keyData.HasFlag(Keys.Alt))
                        if (PerformRemoval())
                            return true;
                }
            }
            return base.ProcessDialogKey(keyData);
        }
    }
}

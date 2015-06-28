using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AccountingServer.BLL;
using AccountingServer.Shell;
using AccountingServer.Plugins.Interest;
using AccountingServer.Plugins.THUInfo;

namespace AccountingServer
{
    // ReSharper disable once InconsistentNaming
    public partial class frmMain : Form
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Accountant m_Accountant;

        /// <summary>
        ///     控制台
        /// </summary>
        private readonly AccountingShell m_Shell;

        /// <summary>
        ///     当前文本是否可编辑
        /// </summary>
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
            Width = 1280;
            Height = 860;

            m_Accountant = new Accountant();

            var thu = new THUInfo(m_Accountant);
            Task.Run(() => thu.FetchData(@"2014010914", @""));

            m_Shell = new AccountingShell(m_Accountant);
            m_Shell.AutoConnect();
            m_Shell.AddPlugin(thu);
            m_Shell.AddPlugin(new InterestRevenue(m_Accountant));
            //m_Shell.PresentQRCode += qrCode =>
            //                           {
            //                               if (qrCode == null)
            //                               {
            //                                   pictureBox1.Visible = false;
            //                                   textBoxResult.Visible = true;
            //                               }
            //                               else
            //                               {
            //                                   pictureBox1.Image = qrCode;
            //                                   pictureBox1.Visible = true;
            //                                   textBoxResult.Visible = false;
            //                               }
            //                           };
        }

        /// <summary>
        ///     获取当前正在编辑的文本
        /// </summary>
        /// <param name="begin">起位置</param>
        /// <param name="end">止位置</param>
        /// <param name="typeName">类型</param>
        /// <returns></returns>
        private bool GetEditableText(out int begin, out int end, out string typeName)
        {
            begin = textBoxResult.Text.LastIndexOf(
                                                   "@new",
                                                   textBoxResult.SelectionStart + textBoxResult.SelectionLength,
                                                   StringComparison.Ordinal);

            typeName = null;

            end = textBoxResult.Text.IndexOf("}@", begin, StringComparison.Ordinal);
            if (end < 0)
                return false;

            end += 1;
            if (end + 2 < textBoxResult.Text.Length &&
                textBoxResult.Text[end + 1] == '\r')
                end += 2;

            typeName = textBoxResult.Text.Substring(
                                                    begin + 5,
                                                    textBoxResult.Text.IndexOfAny(new[] { ' ', '{' }, begin + 5)
                                                    - begin - 5);
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

        /// <summary>
        ///     更新或添加
        /// </summary>
        /// <returns>是否成功</returns>
        private bool PerformUpsert()
        {
            int begin;
            int end;
            string typeName;
            if (!GetEditableText(out begin, out end, out typeName))
                return false;

            try
            {
                var s = textBoxResult.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                string result;
                switch (typeName)
                {
                    case "Voucher":
                        result = m_Shell.ExecuteVoucherUpsert(s);
                        break;
                    case "Asset":
                        result = m_Shell.ExecuteAssetUpsert(s);
                        break;
                    case "Amortization":
                        result = m_Shell.ExecuteAmortUpsert(s);
                        break;
                    case "NamedQueryTemplate":
                        result = m_Shell.ExecuteNamedQueryTemplateUpsert(s);
                        break;
                    default:
                        return false;
                }
                if (textBoxResult.Text[end] == '\n' &&
                    result[result.Length - 1] != '\n')
                    textBoxResult.Text = textBoxResult.Text.Remove(begin, end - begin - 1)
                                                      .Insert(begin, result);
                else
                    textBoxResult.Text = textBoxResult.Text.Remove(begin, end - begin + 1)
                                                      .Insert(begin, result);
                textBoxResult.SelectionStart = begin;
                textBoxResult.SelectionLength = result.Length;
            }
            catch (Exception exception)
            {
                textBoxResult.Text = textBoxResult.Text.Insert(end + 1, exception.ToString());
                textBoxResult.SelectionStart = begin;
                textBoxResult.SelectionLength = end - begin + 1;
            }

            textBoxResult.ScrollToCaret();
            return true;
        }

        /// <summary>
        ///     删除
        /// </summary>
        /// <returns>是否成功</returns>
        private bool PerformRemoval()
        {
            int begin;
            int end;
            string typeName;
            if (!GetEditableText(out begin, out end, out typeName))
                return false;

            try
            {
                var s = textBoxResult.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                bool result;
                switch (typeName)
                {
                    case "Voucher":
                        result = m_Shell.ExecuteVoucherRemoval(s);
                        break;
                    case "Asset":
                        result = m_Shell.ExecuteAssetRemoval(s);
                        break;
                    case "Amortization":
                        result = m_Shell.ExecuteAmortRemoval(s);
                        break;
                    case "NamedQueryTemplate":
                        result = m_Shell.ExecuteNamedQueryTemplateRemoval(s);
                        break;
                    default:
                        return false;
                }
                if (!result)
                    throw new ApplicationException("提交的内容类型未知");
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

        /// <summary>
        ///     执行表达式
        /// </summary>
        /// <returns></returns>
        private bool ExecuteCommand(bool append)
        {
            try
            {
                var res = m_Shell.Execute(textBoxCommand.Text);
                if (res == null)
                    return true;

                if (res is ChartData)
                {
                    ProcessChart(res as ChartData);
                    FocusTextBoxCommand();
                    return true;
                }

                m_Editable |= res is EditableText;

                var result = res.ToString();

                SwitchToText();

                textBoxResult.Focus();

                if (append)
                {
                    textBoxResult.AppendText(result);
                    var lng = textBoxResult.Text.Length;
                    textBoxResult.SelectionStart = lng;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = m_Editable
                                                   ? Color.FromArgb(0, 200, 0)
                                                   : Color.FromArgb(200, 220, 0);
                }
                else
                {
                    textBoxResult.Text = result;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = m_Editable
                                                   ? Color.FromArgb(75, 255, 75)
                                                   : Color.FromArgb(250, 250, 0);
                }

                if (res.AutoReturn)
                    FocusTextBoxCommand();
                return true;
            }
            catch (Exception exception)
            {
                if (append)
                {
                    textBoxResult.AppendText(exception.ToString());
                    var lng = textBoxResult.Text.Length;
                    textBoxResult.SelectionStart = lng;
                    textBoxResult.SelectionLength = 0;
                }
                else
                    textBoxResult.Text = exception.ToString();
                textBoxCommand.BackColor = Color.FromArgb(255, 70, 70);
                SwitchToText();
                return false;
            }
        }

        /// <summary>
        ///     处理图表
        /// </summary>
        /// <param name="chartArgs">图表数据</param>
        private void ProcessChart(ChartData chartArgs)
        {
            chart1.ChartAreas.SuspendUpdates();

            chart1.ChartAreas.Clear();

            foreach (var chartArea in chartArgs.ChartAreas)
                chart1.ChartAreas.Add(chartArea);

            chart1.Legends[0].Font = new Font("Microsoft YaHei Mono", 12, GraphicsUnit.Pixel);

            chart1.Series.Clear();

            foreach (var series in chartArgs.Series)
                chart1.Series.Add(series);

            chart1.ChartAreas.ResumeUpdates();

            SwitchToChart();
        }

        /// <summary>
        ///     显示文本
        /// </summary>
        private void SwitchToText()
        {
            chart1.Visible = false;
            textBoxResult.Visible = true;
            pictureBox1.Visible = false;
        }

        /// <summary>
        ///     显示图表
        /// </summary>
        private void SwitchToChart()
        {
            chart1.Visible = true;
            textBoxResult.Visible = false;
            pictureBox1.Visible = false;
        }

        /// <summary>
        ///     准备输入表达式
        /// </summary>
        private void FocusTextBoxCommand()
        {
            textBoxCommand.Focus();
            textBoxCommand.SelectionStart = 0;
            textBoxCommand.SelectionLength = textBoxCommand.Text.Length;
        }

        /// <inheritdoc />
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                FocusTextBoxCommand();
                return true;
            }
            if (textBoxCommand.Focused)
            {
                if (keyData == Keys.Enter)
                    if (ExecuteCommand(false))
                        return true;
                if (keyData == (Keys.Enter | Keys.Shift))
                    if (ExecuteCommand(true))
                        return true;
            }
            else if (textBoxResult.Focused && m_Editable)
            {
                if (keyData == (Keys.Enter | Keys.Alt))
                    if (PerformUpsert())
                        return true;
                if (keyData == (Keys.Delete | Keys.Alt))
                    if (PerformRemoval())
                        return true;
            }
            return base.ProcessDialogKey(keyData);
        }
    }
}

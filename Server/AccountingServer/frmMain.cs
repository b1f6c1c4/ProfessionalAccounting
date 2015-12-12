using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Plugins.Interest;
using AccountingServer.Plugins.THUInfo;
using AccountingServer.Plugins.Utilities;
using AccountingServer.Plugins.YieldRate;
using AccountingServer.Shell;

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
        ///     快捷编辑缩写
        /// </summary>
        private readonly List<Tuple<string, bool, VoucherDetail>> m_Abbrs;

        /// <summary>
        ///     快捷编辑是否启用
        /// </summary>
        private bool m_FastEditing;

        /// <summary>
        ///     快捷编辑下一个细目偏移
        /// </summary>
        private int m_FastInsertLocationDelta;

        /// <summary>
        ///     快捷编辑下一个字段偏移
        /// </summary>
        private int m_FastNextLocationDelta;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public frmMain()
        {
            InitializeComponent();
            chart1.Dock = DockStyle.Fill;
            textBoxResult.Dock = DockStyle.Fill;

            SetProcessDPIAware();
            Width = 1280;
            Height = 860;

            m_Accountant = new Accountant();

            var thu = new THUInfo(m_Accountant);
            Task.Run(() => thu.FetchData(@"2014010914", @""));

            try
            {
                using (
                    var stream = Assembly.GetExecutingAssembly()
                                         .GetManifestResourceStream("AccountingServer.Resources.Abbr.xml"))
                {
                    if (stream == null)
                        throw new Exception();
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(stream);

                    m_Abbrs = new List<Tuple<string, bool, VoucherDetail>>();
                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlElement sc in xmlDoc.DocumentElement.ChildNodes)
                    {
                        var str = sc.Attributes["string"].Value;
                        var edit = sc.HasAttribute("edit");
                        var vd = new VoucherDetail();
                        if (edit)
                            vd.Content = string.Empty;
                        foreach (XmlElement ele in sc.ChildNodes)
                            switch (ele.LocalName)
                            {
                                case "Title":
                                    vd.Title = Convert.ToInt32(ele.InnerText);
                                    break;
                                case "SubTitle":
                                    vd.SubTitle = Convert.ToInt32(ele.InnerText);
                                    break;
                                case "Content":
                                    vd.Content = ele.InnerText;
                                    break;
                                case "Remark":
                                    vd.Remark = ele.InnerText;
                                    break;
                            }
                        m_Abbrs.Add(
                                    new Tuple<string, bool, VoucherDetail>(
                                        str,
                                        edit,
                                        vd));
                    }
                    var col = new AutoCompleteStringCollection();
                    col.AddRange(m_Abbrs.Select(tpl => tpl.Item1).ToArray());
                    textBoxCommand.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    textBoxCommand.AutoCompleteCustomSource = col;
                }
            }
            catch (Exception)
            {
                m_Abbrs = null;
            }
            m_FastEditing = false;

            m_Shell = new AccountingShell(m_Accountant)
                          {
                              PluginManager =
                                  new PluginShell
                                      {
                                          thu,
                                          new InterestRevenue(m_Accountant),
                                          new Utilities(m_Accountant),
                                          new YieldRate(m_Accountant)
                                      }
                          };
            m_Shell.AutoConnect();
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

                var result = res.ToString();

                SwitchToText();

                textBoxResult.Focus();

                if (append)
                {
                    textBoxResult.AppendText(result);
                    var lng = textBoxResult.Text.Length;
                    textBoxResult.SelectionStart = lng;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = Color.FromArgb(0, 200, 0);
                }
                else
                {
                    textBoxResult.Text = result;
                    textBoxResult.SelectionLength = 0;
                    textBoxCommand.BackColor = Color.FromArgb(75, 255, 75);
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
        }

        /// <summary>
        ///     显示图表
        /// </summary>
        private void SwitchToChart()
        {
            chart1.Visible = true;
            textBoxResult.Visible = false;
        }

        /// <summary>
        ///     准备输入表达式
        /// </summary>
        private void FocusTextBoxCommand()
        {
            textBoxCommand.Focus();
            textBoxCommand.SelectionStart = 0;
            textBoxCommand.SelectionLength = textBoxCommand.TextLength;
        }

        /// <summary>
        ///     进入快捷编辑模式
        /// </summary>
        private void EnterFastEditing()
        {
            m_FastEditing = true;
            textBoxCommand.ForeColor = Color.White;
            textBoxCommand.BackColor = Color.Black;
            textBoxResult.ForeColor = Color.White;
            textBoxResult.BackColor = Color.Black;
            textBoxCommand.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        }

        /// <summary>
        ///     退出快捷编辑模式
        /// </summary>
        private void ExitFastEditing()
        {
            m_FastEditing = false;
            textBoxCommand.ForeColor = Color.Black;
            textBoxCommand.BackColor = Color.White;
            textBoxResult.ForeColor = Color.Black;
            textBoxResult.BackColor = Color.White;
            textBoxCommand.AutoCompleteMode = AutoCompleteMode.None;
        }

        /// <inheritdoc />
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (textBoxCommand.Focused)
            {
                if (keyData == Keys.Escape)
                {
                    FocusTextBoxCommand();
                    if (m_FastEditing)
                    {
                        ExitFastEditing();
                        textBoxResult.Focus();
                    }
                    return true;
                }
                if (keyData == Keys.Tab)
                {
                    textBoxResult.Focus();
                    textBoxResult.SelectionStart = textBoxResult.TextLength;
                    textBoxResult.ScrollToCaret();
                }
                if (keyData == Keys.Enter &&
                    !m_FastEditing)
                {
                    if (textBoxCommand.Text.Length == 0)
                    {
                        var tmp = textBoxResult.SelectionStart = textBoxResult.TextLength;
                        var txtPre = "@new Voucher {" + Environment.NewLine +
                                     $"    Date = D(\"{DateTime.Now:yyy-MM-dd}\")," + Environment.NewLine +
                                     "    Details = new List<VoucherDetail> {" + Environment.NewLine;
                        var txtApp = Environment.NewLine +
                                     "    } }@" + Environment.NewLine +
                                     ";" + Environment.NewLine;
                        textBoxResult.SelectedText = txtPre + txtApp;
                        textBoxResult.SelectionLength = 0;
                        textBoxResult.SelectionStart = tmp + txtPre.Length;
                        m_FastInsertLocationDelta = 0;
                        textBoxResult.ScrollToCaret();

                        FocusTextBoxCommand();
                        EnterFastEditing();
                        textBoxResult.ScrollToCaret();
                        return true;
                    }
                    if (ExecuteCommand(false))
                        return true;
                }
                if (keyData == (Keys.Enter | Keys.Shift) &&
                    !m_FastEditing)
                    if (ExecuteCommand(true))
                        return true;
                if (keyData == Keys.Enter && m_FastEditing)
                {
                    if (textBoxCommand.Text.Length == 0)
                        if (PerformUpsert())
                        {
                            ExitFastEditing();
                            return true;
                        }

                    var sc =
                        m_Abbrs.SingleOrDefault(
                                                tpl =>
                                                tpl.Item1.Equals(
                                                                 textBoxCommand.Text,
                                                                 StringComparison.InvariantCultureIgnoreCase));
                    if (sc == null)
                        return false;
                    var s = CSharpHelper.PresentVoucherDetail(sc.Item3);
                    var idC = sc.Item2 ? s.IndexOf("Content = \"\"", StringComparison.InvariantCulture) + 11 : -1;
                    var idF = s.IndexOf("Fund = null", StringComparison.InvariantCulture) + 7;
                    textBoxResult.Focus();
                    textBoxResult.SelectedText = s;
                    textBoxResult.SelectionLength = 0;
                    if (sc.Item2)
                    {
                        textBoxResult.SelectionStart += idC - s.Length;
                        textBoxResult.SelectionLength = 0;
                        m_FastNextLocationDelta = idF - idC;
                        m_FastInsertLocationDelta = s.Length - idC;
                    }
                    else
                    {
                        textBoxResult.SelectionStart += idF - s.Length;
                        textBoxResult.SelectionLength = 4;
                        m_FastNextLocationDelta = s.Length - idF;
                        m_FastInsertLocationDelta = s.Length - idF;
                    }
                    textBoxResult.ScrollToCaret();
                    textBoxCommand.Text = string.Empty;
                    return true;
                }
            }
            else if (textBoxResult.Focused)
            {
                if (keyData == Keys.Escape)
                {
                    FocusTextBoxCommand();
                    return true;
                }
                if (keyData == (Keys.Enter | Keys.Alt))
                    if (PerformUpsert())
                        return true;
                if (keyData == (Keys.Delete | Keys.Alt))
                    if (PerformRemoval())
                        return true;
                if ((keyData == Keys.Tab || keyData == Keys.Enter) &&
                    m_FastEditing)
                {
                    textBoxResult.SelectionStart += m_FastNextLocationDelta;
                    if (m_FastNextLocationDelta == m_FastInsertLocationDelta)
                        FocusTextBoxCommand();
                    else
                    {
                        textBoxResult.SelectionLength = 4;
                        m_FastNextLocationDelta = m_FastInsertLocationDelta -= m_FastNextLocationDelta;
                    }
                    return true;
                }
            }
            return base.ProcessDialogKey(keyData);
        }

        private void frmMain_Activated(object sender, EventArgs e) => FocusTextBoxCommand();
    }
}

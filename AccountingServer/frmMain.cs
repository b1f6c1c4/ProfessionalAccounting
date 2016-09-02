using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;
using ScintillaNET;

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
        private readonly CustomManager<Abbreviations> m_Abbrs;

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
            SetProcessDPIAware();
            InitializeComponent();
            chart1.Dock = DockStyle.Fill;
            scintilla.Dock = DockStyle.Fill;

            Width = 1280;
            Height = 860;

            SetupScintilla();

            m_Accountant = new Accountant();

            m_Abbrs = new CustomManager<Abbreviations>("Abbr.xml");
            var col = new AutoCompleteStringCollection();
            col.AddRange(m_Abbrs.Config.Abbrs.Select(tpl => tpl.Abbr).ToArray());
            textBoxCommand.AutoCompleteSource = AutoCompleteSource.CustomSource;
            textBoxCommand.AutoCompleteCustomSource = col;
            m_FastEditing = false;

            m_Shell = new AccountingShell(m_Accountant);
            m_Shell.AutoConnect();
        }

        private void SetupScintilla()
        {
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Microsoft YaHei Mono";
            scintilla.Styles[Style.Default].SizeF = textBoxCommand.Font.Size;
            scintilla.StyleClearAll();

            scintilla.Styles[Style.Cpp.Default].ForeColor = Color.Silver;
            scintilla.Styles[Style.Cpp.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Cpp.CommentLine].ForeColor = Color.Green;
            scintilla.Styles[Style.Cpp.CommentLineDoc].ForeColor = Color.FromArgb(128, 128, 128);
            scintilla.Styles[Style.Cpp.Number].ForeColor = Color.Black;
            scintilla.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
            scintilla.Styles[Style.Cpp.Identifier].ForeColor = Color.FromArgb(128, 0, 128);
            scintilla.Styles[Style.Cpp.Word2].ForeColor = Color.FromArgb(0, 139, 139);
            scintilla.Styles[Style.Cpp.GlobalClass].ForeColor = Color.FromArgb(0, 0, 139);
            scintilla.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21);
            scintilla.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21);
            scintilla.Styles[Style.Cpp.Verbatim].ForeColor = Color.FromArgb(163, 21, 21);
            scintilla.Styles[Style.Cpp.StringEol].BackColor = Color.Pink;
            scintilla.Styles[Style.Cpp.Operator].ForeColor = Color.Black;
            scintilla.Styles[Style.Cpp.Preprocessor].ForeColor = Color.Silver;

            scintilla.SetKeywords(0, "@ @new new null");
            scintilla.SetKeywords(1, "D G");
            scintilla.SetKeywords(
                                  3,
                                  "List Voucher VoucherDetail Asset AssetItem AcquisationItem DepreciationMethod DepreciateItem DevalueItem DispositionItem AmortizeInterval AmortItem Amortization");

            acMenu.TargetControlWrapper = new ScintillaWrapper(scintilla);
            //var listView = ((AutocompleteListView)acMenu.ListView);
            //listView.ItemHeight = (int)(listView.ItemHeight);
            acMenu.SetAutocompleteItems(
                                        "List Voucher VoucherDetail Asset AssetItem AcquisationItem DepreciationMethod DepreciateItem DevalueItem DispositionItem AmortizeInterval AmortItem Amortization"
                                            .Split(' '));
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
            begin = scintilla.Text.LastIndexOf(
                                               "@new",
                                               scintilla.SelectionEnd,
                                               StringComparison.Ordinal);

            typeName = null;

            end = scintilla.Text.IndexOf("}@", begin, StringComparison.Ordinal);
            if (end < 0)
                return false;

            end += 1;
            if (end + 2 < scintilla.Text.Length &&
                scintilla.Text[end + 1] == '\r')
                end += 2;

            typeName = scintilla.Text.Substring(
                                                begin + 5,
                                                scintilla.Text.IndexOfAny(new[] { ' ', '{' }, begin + 5)
                                                - begin - 5);
            return true;
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
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
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
                if (scintilla.Text[end] == '\n' &&
                    result[result.Length - 1] != '\n')
                    scintilla.Text = scintilla.Text.Remove(begin, end - begin - 1)
                                              .Insert(begin, result);
                else
                    scintilla.Text = scintilla.Text.Remove(begin, end - begin + 1)
                                              .Insert(begin, result);
                scintilla.SelectionStart = begin;
                scintilla.SelectionEnd = result.Length - begin;
            }
            catch (Exception exception)
            {
                scintilla.Text = scintilla.Text.Insert(end + 1, exception.ToString());
                scintilla.SelectionStart = begin;
                scintilla.SelectionEnd = end;
            }

            scintilla.ScrollCaret();
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
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
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
                if (scintilla.Text[end] == '}')
                    scintilla.Text = scintilla.Text.Insert(end + 1, "*/").Insert(begin, "/*");
                else //if (scintilla.Text[end] == '\n')
                    scintilla.Text = scintilla.Text.Insert(end - 1, "*/").Insert(begin, "/*");
                scintilla.SelectionStart = begin;
                scintilla.SelectionEnd = end + 4;
            }
            catch (Exception exception)
            {
                scintilla.Text = scintilla.Text.Insert(end, exception.ToString());
                scintilla.SelectionStart = begin;
                scintilla.SelectionEnd = end - begin;
            }

            scintilla.ScrollCaret();
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

                scintilla.Focus();

                if (append)
                {
                    scintilla.AppendText(result);
                    var lng = scintilla.Text.Length;
                    scintilla.SelectionStart = lng;
                    scintilla.SelectionEnd = lng;
                    textBoxCommand.BackColor = Color.FromArgb(0, 200, 0);
                }
                else
                {
                    scintilla.Text = result;
                    scintilla.SelectionEnd = scintilla.SelectionStart;
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
                    scintilla.AppendText(exception.ToString());
                    var lng = scintilla.Text.Length;
                    scintilla.SelectionStart = lng;
                    scintilla.SelectionEnd = lng;
                }
                else
                    scintilla.Text = exception.ToString();
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
            scintilla.Visible = true;
        }

        /// <summary>
        ///     显示图表
        /// </summary>
        private void SwitchToChart()
        {
            chart1.Visible = true;
            scintilla.Visible = false;
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
            var tmp = scintilla.SelectionStart = scintilla.TextLength;
            var txtPre = "@new Voucher {" + Environment.NewLine +
                         $"    Date = D(\"{DateTime.Now:yyy-MM-dd}\")," + Environment.NewLine +
                         "    Details = new List<VoucherDetail> {" + Environment.NewLine;
            var txtApp = Environment.NewLine +
                         "    } }@" + Environment.NewLine +
                         ";" + Environment.NewLine;
            scintilla.DeleteRange(
                                  scintilla.SelectionStart,
                                  scintilla.SelectionEnd - scintilla.SelectionStart);
            scintilla.InsertText(scintilla.SelectionStart, txtPre + txtApp);
            scintilla.SelectionStart = tmp + txtPre.Length;
            m_FastInsertLocationDelta = 0;
            scintilla.ScrollCaret();

            FocusTextBoxCommand();

            m_FastEditing = true;
            textBoxCommand.ForeColor = Color.White;
            textBoxCommand.BackColor = Color.Black;
            scintilla.ForeColor = Color.White;
            scintilla.BackColor = Color.Black;
            textBoxCommand.AutoCompleteMode = AutoCompleteMode.SuggestAppend;

            scintilla.ScrollCaret();
        }

        /// <summary>
        ///     退出快捷编辑模式
        /// </summary>
        private void ExitFastEditing()
        {
            m_FastEditing = false;
            textBoxCommand.ForeColor = Color.Black;
            textBoxCommand.BackColor = Color.White;
            scintilla.ForeColor = Color.Black;
            scintilla.BackColor = Color.White;
            textBoxCommand.AutoCompleteMode = AutoCompleteMode.None;

            scintilla.Focus();
        }

        /// <inheritdoc />
        protected override bool ProcessDialogKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    return true;
                case Keys.Return:
                    return true;
                case Keys.Enter | Keys.Shift:
                    return true;
                case Keys.Enter | Keys.Alt:
                    return true;
                case Keys.Delete | Keys.Alt:
                    return true;
                default:
                    return base.ProcessDialogKey(keyData);
            }
        }

        private void frmMain_Shown(object sender, EventArgs e) => FocusTextBoxCommand();

        private bool textBoxCommand_Key(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                FocusTextBoxCommand();
                if (m_FastEditing)
                    ExitFastEditing();
                return true;
            }
            if (keyData == Keys.Tab)
            {
                scintilla.Focus();
                scintilla.SelectionStart = scintilla.TextLength;
                scintilla.ScrollCaret();
                return true;
            }
            if (keyData == Keys.Enter &&
                !m_FastEditing)
                if (textBoxCommand.Text.Length == 0)
                {
                    EnterFastEditing();
                    return true;
                }
                else
                    return ExecuteCommand(false);
            if (keyData == (Keys.Enter | Keys.Shift) &&
                !m_FastEditing)
                return ExecuteCommand(true);
            if (keyData == Keys.Enter && m_FastEditing)
                if (textBoxCommand.Text.Length == 0)
                {
                    if (!PerformUpsert())
                        return false;
                    ExitFastEditing();
                    return true;
                }
                else
                {
                    FastEdit();
                    return true;
                }
            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (textBoxCommand.Focused)
                return textBoxCommand_Key(keyData) || base.ProcessCmdKey(ref msg, keyData);
            if (scintilla.Focused)
                return scintilla_Key(keyData) || base.ProcessCmdKey(ref msg, keyData);
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private bool scintilla_Key(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                FocusTextBoxCommand();
                return true;
            }
            if (keyData == (Keys.Enter | Keys.Alt))
                return PerformUpsert();
            if (keyData == (Keys.Delete | Keys.Alt))
                return PerformRemoval();
            if ((keyData == Keys.Tab || keyData == Keys.Enter) &&
                m_FastEditing)
            {
                if (m_FastNextLocationDelta == m_FastInsertLocationDelta)
                {
                    scintilla.SelectionStart += m_FastNextLocationDelta + scintilla.SelectionEnd -
                                                scintilla.SelectionStart - 4;
                    FocusTextBoxCommand();
                }
                else
                {
                    scintilla.SelectionStart += m_FastNextLocationDelta;
                    scintilla.SelectionEnd = scintilla.SelectionStart + 4;
                    m_FastNextLocationDelta = m_FastInsertLocationDelta -= m_FastNextLocationDelta;
                }
                return true;
            }
            return false;
        }

        private void FastEdit()
        {
            var sc =
                m_Abbrs.Config.Abbrs
                       .SingleOrDefault(
                                        tpl =>
                                        tpl.Abbr.Equals(
                                                        textBoxCommand.Text,
                                                        StringComparison.InvariantCultureIgnoreCase));
            if (sc == null)
                return;
            var cnt = sc.Editable ? sc.Content ?? "" : sc.Content;
            var s = CSharpHelper.PresentVoucherDetail(
                                                      new VoucherDetail
                                                          {
                                                              Title = sc.Title,
                                                              SubTitle = sc.SubTitle,
                                                              Content = cnt,
                                                              Remark = sc.Remark
                                                          });
            var idF = s.IndexOf("Fund = null", StringComparison.InvariantCulture) + 7;
            scintilla.Focus();
            scintilla.InsertText(scintilla.SelectionStart, s);
            if (sc.Editable)
            {
                var idC = s.IndexOf("Content = \"\"", StringComparison.InvariantCulture) + 11;
                scintilla.SelectionStart += idC;
                scintilla.SelectionStart = scintilla.SelectionStart + cnt.Length;
                m_FastNextLocationDelta = idF - idC;
                m_FastInsertLocationDelta = s.Length - idC;
            }
            else
            {
                scintilla.SelectionStart += idF;
                scintilla.SelectionEnd = scintilla.SelectionStart + 4;
                m_FastNextLocationDelta = s.Length - idF;
                m_FastInsertLocationDelta = s.Length - idF;
            }
            scintilla.ScrollCaret();
            textBoxCommand.Text = string.Empty;
        }
    }
}

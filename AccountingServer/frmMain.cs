using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AccountingServer.Shell;
using ScintillaNET;

namespace AccountingServer
{
    // ReSharper disable once InconsistentNaming
    public partial class frmMain : Form
    {
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
            PrepareFastEditing();
            PrepareAccounting();
        }

        /// <summary>
        ///     初始化编辑器
        /// </summary>
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (textBoxCommand.Focused)
                return textBoxCommand_Key(keyData) || base.ProcessCmdKey(ref msg, keyData);
            if (scintilla.Focused)
                return scintilla_Key(keyData) || base.ProcessCmdKey(ref msg, keyData);
            return base.ProcessCmdKey(ref msg, keyData);
        }

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
    }
}

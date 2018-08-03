using System;
using System.Drawing;
using System.Windows.Forms;
using ScintillaNET;

namespace AccountingClient
{
    // ReSharper disable once InconsistentNaming
    internal partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            scintilla.Dock = DockStyle.Fill;

            Width = 1280;
            Height = 860;

            SetupScintilla();
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
        /// <param name="position">搜索位置</param>
        /// <param name="begin">起位置</param>
        /// <param name="end">止位置</param>
        /// <param name="typeName">类型</param>
        /// <returns>是否找到</returns>
        private bool GetEditableText(int position, out int begin, out int end, out string typeName)
        {
            begin = scintilla.Text.LastIndexOf(
                "@new",
                position,
                StringComparison.Ordinal);

            if (begin < 0)
            {
                end = -1;
                typeName = null;
                return false;
            }

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
            switch (keyData)
            {
                case Keys.Escape:
                    FocusTextBoxCommand();
                    return true;
                case Keys.Tab:
                    scintilla.Focus();
                    scintilla.SelectionStart = scintilla.TextLength;
                    scintilla.ScrollCaret();
                    return true;
                case Keys.Enter:
                    if (string.IsNullOrWhiteSpace(textBoxCommand.Text))
                    {
                        var line = scintilla.CurrentLine;
                        scintilla.InsertText(scintilla.Lines[line].Position, m_Shell.EmptyVoucher);
                        scintilla.Focus();
                        scintilla.SelectionStart = scintilla.Lines[line + 1].Position;
                        scintilla.SelectionEnd = scintilla.SelectionStart;
                        scintilla.ScrollCaret();
                        return true;
                    }

                    return ExecuteCommand(false);
                case Keys.Enter | Keys.Shift:
                    return ExecuteCommand(true);
            }

            return false;
        }

        private bool scintilla_Key(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    FocusTextBoxCommand();
                    return true;
                case Keys.Enter | Keys.Alt:
                    return PerformUpsert();
                case Keys.Delete | Keys.Alt:
                    return PerformRemoval();
                case Keys.Enter | Keys.Shift | Keys.Control | Keys.Alt:
                    PerformUpserts();
                    return true;
                case Keys.Delete | Keys.Shift | Keys.Control | Keys.Alt:
                    PerformRemovals();
                    return true;
                default:
                    return false;
            }
        }
    }
}

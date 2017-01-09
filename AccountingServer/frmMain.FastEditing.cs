using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AccountingServer.BLL;
using AccountingServer.Shell;
using AccountingServer.Entities;

namespace AccountingServer
{
    // ReSharper disable once InconsistentNaming
    public partial class frmMain
    {
        /// <summary>
        ///     快捷编辑是否启用
        /// </summary>
        private bool m_FastEditing;

        /// <summary>
        ///     快捷编辑缩写
        /// </summary>
        private ConfigManager<Abbreviations> m_Abbrs;

        /// <summary>
        ///     快捷编辑下一个细目偏移
        /// </summary>
        private int m_FastInsertLocationDelta;

        /// <summary>
        ///     快捷编辑下一个字段偏移
        /// </summary>
        private int m_FastNextLocationDelta;

        /// <summary>
        ///     准备快捷编辑
        /// </summary>
        private void PrepareFastEditing()
        {
            m_Abbrs = new ConfigManager<Abbreviations>("Abbr.xml");
            var col = new AutoCompleteStringCollection();
            col.AddRange(m_Abbrs.Config.Abbrs.Select(tpl => tpl.Abbr).ToArray());
            textBoxCommand.AutoCompleteSource = AutoCompleteSource.CustomSource;
            textBoxCommand.AutoCompleteCustomSource = col;
            m_FastEditing = false;
        }

        /// <summary>
        ///     进行快捷编辑
        /// </summary>
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
    }
}

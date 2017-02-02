using System;
using System.Drawing;
using AccountingServer.Shell;

namespace AccountingServer
{
    // ReSharper disable once InconsistentNaming
    public partial class frmMain
    {
        /// <summary>
        ///     控制台
        /// </summary>
        private Facade m_Shell;

        /// <summary>
        ///     初始化
        /// </summary>
        private void PrepareAccounting() => m_Shell = new Facade();

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
                    default:
                        return false;
                }

                if (scintilla.Text[end] == '\n' &&
                    result[result.Length - 1] != '\n')
                {
                    scintilla.DeleteRange(begin, end - begin - 1);
                    scintilla.InsertText(begin, result);
                }
                else
                {
                    scintilla.DeleteRange(begin, end - begin + 1);
                    scintilla.InsertText(begin, result);
                }
            }
            catch (Exception exception)
            {
                scintilla.InsertText(end + 1, exception.ToString());
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
                    default:
                        return false;
                }

                if (!result)
                    throw new ApplicationException("提交的内容类型未知");
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (scintilla.Text[end] == '}')
                {
                    scintilla.InsertText(end + 1, "*/");
                    scintilla.InsertText(begin, "/*");
                }
                else //if (scintilla.Text[end] == '\n')
                {
                    scintilla.InsertText(end - 1, "*/");
                    scintilla.InsertText(begin, "/*");
                }
            }
            catch (Exception exception)
            {
                scintilla.InsertText(end, exception.ToString());
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

                var result = res.ToString();

                scintilla.Focus();

                if (append)
                {
                    scintilla.AppendText(result);
                    var lng = scintilla.Text.Length;
                    scintilla.SelectionStart = lng;
                    textBoxCommand.BackColor = Color.FromArgb(0, 200, 0);
                }
                else
                {
                    scintilla.Text = result;
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
                }
                else
                    scintilla.Text = exception.ToString();
                textBoxCommand.BackColor = Color.FromArgb(255, 70, 70);
                return false;
            }
        }
    }
}

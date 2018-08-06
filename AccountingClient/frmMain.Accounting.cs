using System;
using System.Drawing;
using AccountingClient.Shell;

namespace AccountingClient
{
    // ReSharper disable once InconsistentNaming
    internal partial class frmMain
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
        ///     批量更新或添加
        /// </summary>
        private void PerformUpserts()
        {
            var pos = scintilla.TextLength;
            while (PerformUpsert(out pos, pos)) { }
        }

        /// <summary>
        ///     更新或添加
        /// </summary>
        /// <param name="position">搜索位置</param>
        /// <returns>是否成功</returns>
        private bool PerformUpsert(int? position = null) => PerformUpsert(out _, position);

        /// <summary>
        ///     更新或添加
        /// </summary>
        /// <param name="newPosition">搜索位置</param>
        /// <param name="position">开始位置</param>
        /// <returns>是否成功</returns>
        private bool PerformUpsert(out int newPosition, int? position = null)
        {
            newPosition = position ?? scintilla.SelectionEnd;

            if (!GetEditableText(newPosition, out var begin, out var end, out var typeName))
                return false;

            try
            {
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                var result = ExecuteUpsert(typeName, s);
                if (result == null)
                    return false;

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

                newPosition = begin;
            }
            catch (Exception exception)
            {
                var result = exception + Environment.NewLine;
                scintilla.InsertText(end + 1, result);
                newPosition = begin;
            }

            scintilla.ScrollCaret();
            return true;
        }

        /// <summary>
        ///     执行更新或添加
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="s">表示</param>
        /// <returns>执行结果</returns>
        private string ExecuteUpsert(string typeName, string s)
        {
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
                    return null;
            }

            return result;
        }

        /// <summary>
        ///     批量删除
        /// </summary>
        private void PerformRemovals()
        {
            var pos = scintilla.TextLength;
            while (PerformRemoval(out pos, pos)) { }
        }

        /// <summary>
        ///     删除
        /// </summary>
        /// <param name="position">搜索位置</param>
        /// <returns>是否成功</returns>
        private bool PerformRemoval(int? position = null) => PerformRemoval(out _, position);

        /// <summary>
        ///     删除
        /// </summary>
        /// <param name="newPosition">搜索位置</param>
        /// <param name="position">搜索位置</param>
        /// <returns>是否成功</returns>
        private bool PerformRemoval(out int newPosition, int? position = null)
        {
            newPosition = position ?? scintilla.SelectionEnd;

            if (!GetEditableText(newPosition, out var begin, out var end, out var typeName))
                return false;

            try
            {
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                var result = ExecuteRemoval(typeName, s);

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

                newPosition = begin;
            }
            catch (Exception exception)
            {
                var result = exception + Environment.NewLine;
                scintilla.InsertText(end + 1, result);
                newPosition = begin;
            }

            scintilla.ScrollCaret();
            return true;
        }

        /// <summary>
        ///     执行删除
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="s">表示</param>
        /// <returns>执行结果</returns>
        private bool ExecuteRemoval(string typeName, string s)
        {
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

            return result;
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
                    textBoxCommand.BackColor = res.Dirty
                        ? Color.FromArgb(200, 220, 0)
                        : Color.FromArgb(0, 200, 0);
                }
                else
                {
                    scintilla.Text = result;
                    textBoxCommand.BackColor = res.Dirty
                        ? Color.FromArgb(250, 250, 0)
                        : Color.FromArgb(75, 255, 75);
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

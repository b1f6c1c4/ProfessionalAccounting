using System;
using System.Drawing;
using System.Threading.Tasks;
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
        private async Task PrepareAccounting()
        {
            m_Shell = new Facade();
            await m_Shell.Init();
        }

        /// <summary>
        ///     批量更新或添加
        /// </summary>
        private async Task PerformUpserts()
        {
            int? pos = scintilla.TextLength;
            while ((pos = await PerformUpsert(pos)).HasValue) { }
        }

        /// <summary>
        ///     更新或添加
        /// </summary>
        /// <param name="position">开始位置</param>
        /// <returns>若成功则为搜索位置，否则为<c>null</c></returns>
        private async Task<int?> PerformUpsert(int? position = null)
        {
            var newPosition = position ?? scintilla.SelectionEnd;

            if (!GetEditableText(newPosition, out var begin, out var end, out var typeName))
                return null;

            try
            {
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                var result = await ExecuteUpsert(typeName, s);
                if (result == null)
                    return null;

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
            return newPosition;
        }

        /// <summary>
        ///     执行更新或添加
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="s">表示</param>
        /// <returns>执行结果</returns>
        private async Task<string> ExecuteUpsert(string typeName, string s)
        {
            string result;
            switch (typeName)
            {
                case "Voucher":
                    result = await m_Shell.ExecuteVoucherUpsert(s);
                    break;
                case "Asset":
                    result = await m_Shell.ExecuteAssetUpsert(s);
                    break;
                case "Amortization":
                    result = await m_Shell.ExecuteAmortUpsert(s);
                    break;
                default:
                    return null;
            }

            return result;
        }

        /// <summary>
        ///     批量删除
        /// </summary>
        private async Task PerformRemovals()
        {
            int? pos = scintilla.TextLength;
            while ((pos = await PerformRemoval(pos)).HasValue) { }
        }

        /// <summary>
        ///     删除
        /// </summary>
        /// <param name="position">开始位置</param>
        /// <returns>若成功则为搜索位置，否则为<c>null</c></returns>
        private async Task<int?> PerformRemoval(int? position = null)
        {
            var newPosition = position ?? scintilla.SelectionEnd;

            if (!GetEditableText(newPosition, out var begin, out var end, out var typeName))
                return null;

            try
            {
                var s = scintilla.Text.Substring(begin, end - begin + 1).Trim().Trim('@');
                var result = await ExecuteRemoval(typeName, s);

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
            return newPosition;
        }

        /// <summary>
        ///     执行删除
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="s">表示</param>
        /// <returns>执行结果</returns>
        private async Task<bool> ExecuteRemoval(string typeName, string s)
        {
            bool result;
            switch (typeName)
            {
                case "Voucher":
                    result = await m_Shell.ExecuteVoucherRemoval(s);
                    break;
                case "Asset":
                    result = await m_Shell.ExecuteAssetRemoval(s);
                    break;
                case "Amortization":
                    result = await m_Shell.ExecuteAmortRemoval(s);
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
        private async Task ExecuteCommand(bool append)
        {
            try
            {
                var res = await m_Shell.Execute(textBoxCommand.Text);
                if (res == null)
                    return;

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
                FocusTextBoxCommand();
            }
        }
    }
}

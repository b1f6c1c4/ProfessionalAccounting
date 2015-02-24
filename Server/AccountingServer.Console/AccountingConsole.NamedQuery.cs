using System;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using Antlr4.Runtime;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     更新或添加命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板</param>
        /// <returns>命名查询模板</returns>
        public string ExecuteNamedQueryTemplateUpsert(string code)
        {
            var template = ConsoleParser.From(code).namedQueryTemplate();

            var s = template.GetChild(0).GetText();
            var name = s.Substring(1, s.Length - 2).Replace("$$", "$");

            if (!m_Accountant.Upsert(name, code))
                throw new Exception();

            return String.Format("@new NamedQueryTemplate {{{0}}}@", code);
        }

        /// <summary>
        ///     删除命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板</param>
        /// <returns>是否成功</returns>
        public bool ExecuteNamedQueryTemplateRemoval(string code)
        {
            var template = ConsoleParser.From(code).namedQueryTemplate();

            var s = template.GetChild(0).GetText();
            var name = s.Substring(1, s.Length - 2).Replace("$$", "$");

            return m_Accountant.DeleteNamedQueryTemplate(name);
        }

        /// <summary>
        ///     显示所有命名查询模板
        /// </summary>
        /// <returns>格式化的信息</returns>
        private IQueryResult ListNamedQueryTemplates()
        {
            var sb = new StringBuilder();

            foreach (var kvp in m_Accountant.SelectNamedQueryTemplates())
            {
                sb.Append("@new NamedQueryTemplate {");
                sb.Append(kvp.Value);
                sb.AppendLine("}@");
            }

            return new EditableText(sb.ToString());
        }
    }
}

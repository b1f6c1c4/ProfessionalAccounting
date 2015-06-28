using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     解析命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <param name="name">名称</param>
        /// <returns>命名查询模板</returns>
        private static string ParseNamedQueryTemplate(string code, out string name)
        {
            var regex =
                new Regex(
                    @"^new\s+NamedQueryTemplate\s*\{(?<all>(?<name>\$(?:[^\$]|\$\$)*\$)(?:\*F[+-]?\d*.?\d*(?:[Ee][+-]?\d+)?|\*P[+-]?\d*.?\d*)?(?:""(?:[^""]|"""")*"")?::?[\s\S]*)\}$");
            var m = regex.Match(code);
            if (m.Length == 0)
                throw new ArgumentException("语法错误", "code");
            name = m.Groups["name"].Value.Dequotation();
            return m.Groups["all"].Value;
        }

        /// <summary>
        ///     更新或添加命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <returns>命名查询模板表达式</returns>
        public string ExecuteNamedQueryTemplateUpsert(string code)
        {
            string name;
            var all = ParseNamedQueryTemplate(code, out name);

            if (!m_Accountant.Upsert(name, all))
                throw new ApplicationException("更新或添加失败");

            return String.Format("@new NamedQueryTemplate {{{0}}}@", all);
        }

        /// <summary>
        ///     删除命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <returns>是否成功</returns>
        public bool ExecuteNamedQueryTemplateRemoval(string code)
        {
            string name;
            ParseNamedQueryTemplate(code, out name);

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

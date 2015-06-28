using System;
using System.Text;
using System.Text.RegularExpressions;
using AccountingServer.BLL;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     命名查询表达式解释器
    /// </summary>
    internal class NamedQueryShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public NamedQueryShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     解析命名查询模板
        /// </summary>
        /// <param name="code">命名查询模板表达式</param>
        /// <param name="name">名称</param>
        /// <returns>命名查询模板</returns>
        public string ParseNamedQueryTemplate(string code, out string name)
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
        ///     显示所有命名查询模板
        /// </summary>
        /// <returns>格式化的信息</returns>
        public IQueryResult ListNamedQueryTemplates()
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

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
            var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream(code))));
            var template = parser.namedQueryTemplate();

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
            var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream(code))));
            var template = parser.namedQueryTemplate();

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
            AutoConnect();

            var sb = new StringBuilder();

            foreach (var kvp in m_Accountant.SelectNamedQueryTemplates())
            {
                sb.Append("@new NamedQueryTemplate {");
                sb.Append(kvp.Value);
                sb.AppendLine("}@");
            }

            return new EditableText(sb.ToString());
        }

        /// <summary>
        ///     对命名查询模板的名称解引用
        /// </summary>
        /// <param name="reference">名称</param>
        /// <param name="rng">用于赋值的日期过滤器</param>
        /// <returns>命名查询模板</returns>
        private INamedQuery Dereference(string reference, DateFilter rng)
        {
            string range, leftExtendedRange;
            if (rng.NullOnly)
                range = leftExtendedRange = "[null]";
            else
            {
                if (rng.StartDate.HasValue)
                    range = rng.EndDate.HasValue
                                ? String.Format(
                                                "[{0:yyyyMMdd}{2}{1:yyyyMMdd}]",
                                                rng.StartDate,
                                                rng.EndDate,
                                                rng.Nullable ? "=" : "~")
                                : String.Format("[{0:yyyyMMdd}{1}]", rng.StartDate, rng.Nullable ? "=" : "~");
                else if (rng.Nullable)
                    range = rng.EndDate.HasValue ? String.Format("[~{0:yyyyMMdd}]", rng.EndDate) : "[]";
                else
                    range = rng.EndDate.HasValue ? String.Format("[={0:yyyyMMdd}]", rng.EndDate) : "[~null]";
                leftExtendedRange = !rng.EndDate.HasValue ? "[]" : String.Format("[~{0:yyyyMMdd}]", rng.EndDate);
            }

            var templateStr = m_Accountant.SelectNamedQueryTemplate(reference)
                                          .Replace("[&RANGE&]", range)
                                          .Replace("[&LEFTEXTENDEDRANGE&]", leftExtendedRange);

            var parser = new ConsoleParser(new CommonTokenStream(new ConsoleLexer(new AntlrInputStream(templateStr))));
            var template = parser.namedQuery();
            return template;
        }

        /// <summary>
        ///     对命名查询模板引用解引用
        /// </summary>
        /// <param name="reference">命名查询模板引用</param>
        /// <param name="rng">用于赋值的日期过滤器</param>
        /// <returns>命名查询模板</returns>
        private INamedQuery Dereference(INamedQueryReference reference, DateFilter rng)
        {
            return Dereference(reference.Name, rng);
        }
    }
}

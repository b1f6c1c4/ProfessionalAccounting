using System;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据库查询
    /// </summary>
    internal static class MongoDbJavascript
    {
        /// <summary>
        ///     细目检索式的Javascript表示
        /// </summary>
        /// <param name="query">细目检索式</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(IQueryCompunded<IDetailQueryAtom> query) =>
            GetJavascriptFilter(query, GetJavascriptFilter);

        /// <summary>
        ///     原子细目检索式的Javascript表示
        /// </summary>
        /// <param name="f">细目检索式</param>
        /// <returns>Javascript表示</returns>
        private static string GetJavascriptFilter(IDetailQueryAtom f)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(d) {");
            if (f != null)
            {
                if (f.Dir != 0)
                    sb.AppendLine(
                        f.Dir > 0
                            ? "    if (d.fund <= 0) return false;"
                            : "    if (d.fund >= 0) return false;");
                if (f.Filter?.Currency != null)
                    if (f.Filter.Currency == VoucherDetail.BaseCurrency)
                        sb.AppendLine("    if (d.currency != null) return false;");
                    else
                        sb.AppendLine(
                            $"    if (d.currency != '{f.Filter.Currency.Replace("\'", "\\\'")}') return false;");
                if (f.Filter?.Title != null)
                    sb.AppendLine($"    if (d.title != {f.Filter.Title}) return false;");
                if (f.Filter?.SubTitle != null)
                    sb.AppendLine(
                        f.Filter.SubTitle == 00
                            ? "    if (d.subtitle != null) return false;"
                            : $"    if (d.subtitle != {f.Filter.SubTitle}) return false;");
                if (f.Filter?.Content != null)
                    sb.AppendLine(
                        f.Filter.Content == string.Empty
                            ? "    if (d.content != null) return false;"
                            : $"    if (d.content != '{f.Filter.Content.Replace("\'", "\\\'")}') return false;");
                if (f.Filter?.Remark != null)
                    sb.AppendLine(
                        f.Filter.Remark == string.Empty
                            ? "    if (d.remark != null) return false;"
                            : $"    if (d.remark != '{f.Filter.Remark.Replace("\'", "\\\'")}') return false;");
                if (f.Filter?.Fund != null)
                    sb.AppendLine(
                        $"    if (Math.abs(d.fund - {f.Filter.Fund:r}) > {VoucherDetail.Tolerance:R}) return false;");
            }
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     一般检索式的Javascript表示
        /// </summary>
        /// <param name="query">检索式</param>
        /// <param name="atomFilter">原子检索式的Javascript表示</param>
        /// <returns>Javascript表示</returns>
        private static string GetJavascriptFilter<TAtom>(IQueryCompunded<TAtom> query, Func<TAtom, string> atomFilter)
            where TAtom : class => query?.Accept(new MongoDbJavascriptVisitor<TAtom>(atomFilter)) ?? atomFilter(null);

        private sealed class MongoDbJavascriptVisitor<TAtom> : IQueryVisitor<TAtom, string>
            where TAtom : class
        {
            private readonly Func<TAtom, string> m_AtomFilter;

            public MongoDbJavascriptVisitor(Func<TAtom, string> atomFilter) => m_AtomFilter = atomFilter;

            public string Visit(TAtom query) => m_AtomFilter(query);

            public string Visit(IQueryAry<TAtom> query)
            {
                var sb = new StringBuilder();
                sb.AppendLine("function (entity) {");
                sb.AppendLine("    return ");
                switch (query.Operator)
                {
                    case OperatorType.None:
                    case OperatorType.Identity:
                        sb.AppendLine($"({query.Filter1.Accept(this)})(entity)");
                        break;
                    case OperatorType.Complement:
                        sb.AppendLine($"!({query.Filter1.Accept(this)})(entity)");
                        break;
                    case OperatorType.Union:
                        sb.AppendLine($"({query.Filter1.Accept(this)})(entity)");
                        sb.AppendLine("||");
                        sb.AppendLine($"({query.Filter2.Accept(this)})(entity)");
                        break;
                    case OperatorType.Intersect:
                        sb.AppendLine($"({query.Filter1.Accept(this)})(entity)");
                        sb.AppendLine("&&");
                        sb.AppendLine($"({query.Filter2.Accept(this)})(entity)");
                        break;
                    case OperatorType.Substract:
                        sb.AppendLine($"({query.Filter1.Accept(this)})(entity)");
                        sb.AppendLine("&& !");
                        sb.AppendLine($"({query.Filter2.Accept(this)})(entity)");
                        break;
                    default:
                        throw new ArgumentException("运算类型未知", nameof(query));
                }

                sb.AppendLine(";");
                sb.AppendLine("}");
                return sb.ToString();
            }
        }
    }
}

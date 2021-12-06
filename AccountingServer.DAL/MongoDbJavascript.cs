using System;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb���ݿ��ѯ
    /// </summary>
    internal static class MongoDbJavascript
    {
        /// <summary>
        ///     ϸĿ����ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="query">ϸĿ����ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        public static string GetJavascriptFilter(IQueryCompunded<IDetailQueryAtom> query) =>
            GetJavascriptFilter(query, GetJavascriptFilter);

        /// <summary>
        ///     ԭ��ϸĿ����ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="f">ϸĿ����ʽ</param>
        /// <returns>Javascript��ʾ</returns>
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
                if (f.Filter != null)
                {
                    if (f.Filter.Title != null)
                        sb.AppendLine($"    if (d.title != {f.Filter.Title}) return false;");
                    if (f.Filter.SubTitle != null)
                        sb.AppendLine(
                            f.Filter.SubTitle == 00
                                ? "    if (d.subtitle != null) return false;"
                                : $"    if (d.subtitle != {f.Filter.SubTitle}) return false;");
                    if (f.Filter.Content != null)
                        sb.AppendLine(
                            f.Filter.Content == string.Empty
                                ? "    if (d.content != null) return false;"
                                : $"    if (d.content != '{(f.Filter.Content.Replace("\'", "\\\'"))}') return false;");
                    if (f.Filter.Remark != null)
                        sb.AppendLine(
                            f.Filter.Remark == string.Empty
                                ? "    if (d.remark != null) return false;"
                                : $"    if (d.remark != '{(f.Filter.Remark.Replace("\'", "\\\'"))}') return false;");
                    if (f.Filter.Fund != null)
                        sb.AppendLine(
                            $"    if (Math.abs(d.fund - {f.Filter.Fund:r}) > {VoucherDetail.Tolerance:R})) return false;");
                }
            }
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     ���ڼ���ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="query">���ڼ���ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static string GetJavascriptFilter(IQueryCompunded<IDistributedQueryAtom> query) =>
            GetJavascriptFilter(query, GetJavascriptFilter);

        /// <summary>
        ///     ԭ�ӷ��ڼ���ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="f">���ڼ���ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter(IDistributedQueryAtom f)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(a) {");
            if (f?.Filter == null)
                sb.AppendLine("    return true;");
            else
            {
                if (f.Filter.ID.HasValue)
                    sb.AppendLine(
                        $"    if (a._id.toString() != BinData(3,'{Convert.ToBase64String(f.Filter.ID.Value.ToByteArray())}')) return false;");
                if (f.Filter.Name != null)
                    sb.AppendLine(
                        f.Filter.Name == string.Empty
                            ? "    if (a.name != null) return false;"
                            : $"    if (a.name != '{(f.Filter.Name.Replace("\'", "\\\'"))}') return false;");
                if (f.Filter.Remark != null)
                    sb.AppendLine(
                        f.Filter.Remark == string.Empty
                            ? "    if (a.remark != null) return false;"
                            : $"    if (a.remark != '{(f.Filter.Remark.Replace("\'", "\\\'"))}') return false;");
                sb.AppendLine("    return true;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     һ�����ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <param name="atomFilter">ԭ�Ӽ���ʽ��Javascript��ʾ</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter<TAtom>(IQueryCompunded<TAtom> query, Func<TAtom, string> atomFilter)
            where TAtom : class
        {
            if (query == null ||
                query is TAtom)
                return atomFilter((TAtom)query);

            var f = query as IQueryAry<TAtom>;
            if (f == null)
                throw new ArgumentException("����ʽ����δ֪", nameof(query));

            var sb = new StringBuilder();
            sb.AppendLine("function (entity) {");
            sb.AppendLine("    return ");
            switch (f.Operator)
            {
                case OperatorType.None:
                case OperatorType.Identity:
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter1, atomFilter)})(entity)");
                    break;
                case OperatorType.Complement:
                    sb.AppendLine($"!({GetJavascriptFilter(f.Filter1, atomFilter)})(entity)");
                    break;
                case OperatorType.Union:
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter1, atomFilter)})(entity)");
                    sb.AppendLine("||");
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter2, atomFilter)})(entity)");
                    break;
                case OperatorType.Intersect:
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter1, atomFilter)})(entity)");
                    sb.AppendLine("&&");
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter2, atomFilter)})(entity)");
                    break;
                case OperatorType.Substract:
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter1, atomFilter)})(entity)");
                    sb.AppendLine("&& !");
                    sb.AppendLine($"({GetJavascriptFilter(f.Filter2, atomFilter)})(entity)");
                    break;
                default:
                    throw new ArgumentException("��������δ֪", nameof(query));
            }

            sb.AppendLine(";");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}

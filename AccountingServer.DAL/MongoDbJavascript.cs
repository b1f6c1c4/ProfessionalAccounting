using System;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb���ݿ��ѯ
    /// </summary>
    internal static class MongoDbJavascript
    {
        /// <summary>
        ///     ����ƾ֤��������Javascript��ʾ
        /// </summary>
        /// <param name="vfilter">����ƾ֤������</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter(Voucher vfilter)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(v) {");
            if (vfilter != null)
            {
                if (vfilter.ID != null)
                    sb.AppendLine($"    if (v._id != '{vfilter.ID}') return false;");
                if (vfilter.Date != null)
                    sb.AppendLine(
                                  $"    if (v.date != new ISODate('{vfilter.Date:yyyy-MM-ddTHH:mm:sszzz}')) return false;");
                if (vfilter.Type != null)
                    switch (vfilter.Type)
                    {
                        case VoucherType.Ordinary:
                            sb.AppendLine("    if (v.special != undefined) return false;");
                            break;
                        case VoucherType.General:
                            sb.AppendLine("    if (v.special != undefined)");
                            sb.AppendLine("    {");
                            sb.AppendLine("        if (v.special == 'acarry') return false;");
                            sb.AppendLine("        if (v.special == 'carry') return false;");
                            sb.AppendLine("    }");
                            break;
                        case VoucherType.Amortization:
                            sb.AppendLine("    if (v.special != 'amorz') return false;");
                            break;
                        case VoucherType.AnnualCarry:
                            sb.AppendLine("    if (v.special != 'acarry') return false;");
                            break;
                        case VoucherType.Carry:
                            sb.AppendLine("    if (v.special != 'carry') return false;");
                            break;
                        case VoucherType.Depreciation:
                            sb.AppendLine("    if (v.special != 'dep') return false;");
                            break;
                        case VoucherType.Devalue:
                            sb.AppendLine("    if (v.special != 'dev') return false;");
                            break;
                        case VoucherType.Uncertain:
                            sb.AppendLine("    if (v.special != 'unc') return false;");
                            break;
                    }
                if (vfilter.Remark != null)
                    sb.AppendLine(
                                  vfilter.Remark == string.Empty
                                      ? "    if (v.remark != null) return false;"
                                      : $"    if (v.remark != '{(vfilter.Remark.Replace("\'", "\\\'"))}') return false;");
            }
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     ���ڹ�������Javascript��ʾ
        /// </summary>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter(DateFilter? rng)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(v) {");
            if (rng == null)
                sb.AppendLine("    return true;");
            else if (rng.Value.NullOnly)
                sb.AppendLine("    return v.date == null;");
            else
            {
                sb.Append("    if (v.date == null) return ");
                sb.AppendLine(rng.Value.Nullable ? "true;" : "false;");

                if (rng.Value.StartDate.HasValue)
                    sb.Append(
                              $"    if (v.date < new ISODate('{rng.Value.StartDate:yyyy-MM-ddTHH:mm:sszzz}')) return false;");
                if (rng.Value.EndDate.HasValue)
                    sb.Append(
                              $"    if (v.date > new ISODate('{rng.Value.EndDate:yyyy-MM-ddTHH:mm:sszzz}')) return false;");

                sb.AppendLine("    return true;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

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
                        sb.AppendLine($"    if (Math.abs(d.fund - {f.Filter.Fund:r}) > 1e-8)) return false;");
                }
            }
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     ����ƾ֤����ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="query">����ƾ֤����ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        public static string GetJavascriptFilter(IQueryCompunded<IVoucherQueryAtom> query) =>
            GetJavascriptFilter(query, GetJavascriptFilter);

        /// <summary>
        ///     ����ƾ֤����ʽ�Ĳ�ѯ
        /// </summary>
        /// <param name="query">����ƾ֤����ʽ</param>
        /// <returns>��ѯ</returns>
        public static FilterDefinition<Voucher> GetJQuery(IQueryCompunded<IVoucherQueryAtom> query) =>
            ToWhere<Voucher>(GetJavascriptFilter(query));

        /// <summary>
        ///     ԭ�Ӽ���ƾ֤����ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="f">����ƾ֤����ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter(IVoucherQueryAtom f)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(v) {");
            if (f != null)
            {
                sb.AppendLine($"    var vfilter = {GetJavascriptFilter(f.VoucherFilter)};");
                sb.AppendLine("    if (!vfilter(v)) return false;");

                sb.AppendLine($"    var rng = {GetJavascriptFilter(f.Range)};");
                sb.AppendLine("    if (!rng(v)) return false;");

                if (f.ForAll)
                {
                    sb.AppendLine("    var i = 0;");
                    sb.AppendLine("    for (i = 0;i < v.detail.length;i++)");
                    sb.AppendLine(
                                  $"        if (!({GetJavascriptFilter(f.DetailFilter, GetJavascriptFilter)})(v.detail[i]))");
                    sb.AppendLine("            break;");
                    sb.AppendLine("    if (i < v.detail.length)");
                    sb.AppendLine("        return false;");
                }
                else
                {
                    sb.AppendLine("    var i = 0;");
                    sb.AppendLine("    for (i = 0;i < v.detail.length;i++)");
                    sb.AppendLine(
                                  $"        if (({GetJavascriptFilter(f.DetailFilter, GetJavascriptFilter)})(v.detail[i]))");
                    sb.AppendLine("            break;");
                    sb.AppendLine("    if (i >= v.detail.length)");
                    sb.AppendLine("        return false;");
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

        /// <summary>
        ///     ������ʽ��Javascript��ʾת��Ϊ��ѯ
        /// </summary>
        /// <param name="js">Javascript��ʾ</param>
        /// <returns>��ѯ</returns>
        private static FilterDefinition<T> ToWhere<T>(string js)
        {
            var code = $"function() {{ return ({js})(this); }}";
            return new BsonDocument { { "$where", code.Replace(Environment.NewLine, string.Empty) } };
        }
    }
}

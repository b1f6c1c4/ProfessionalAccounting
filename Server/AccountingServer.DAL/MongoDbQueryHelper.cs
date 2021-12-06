using System;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb���ݿ��ѯ
    /// </summary>
    internal static class MongoDbQueryHelper
    {
        /// <summary>
        ///     ����Ų�ѯ<c>ObjectId</c>
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetQuery(string id)
        {
            return Query.EQ("_id", ObjectId.Parse(id));
        }

        /// <summary>
        ///     ����Ų�ѯ<c>Guid</c>
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);
        }

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
                {
                    sb.AppendFormat("    if (v._id != '{0}') return false;", vfilter.ID);
                    sb.AppendLine();
                }
                if (vfilter.Date != null)
                {
                    sb.AppendFormat(
                                    "    if (v.date != new ISODate('{0:yyyy-MM-ddTHH:mm:sszzz}')) return false;",
                                    vfilter.Date);
                    sb.AppendLine();
                }
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
                    if (vfilter.Remark == String.Empty)
                        sb.AppendLine("    if (v.remark != null) return false;");
                    else
                        sb.AppendFormat(
                                        "    if (v.remark != '{0}') return false;",
                                        vfilter.Remark.Replace("\'", "\\\'"));
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
                    sb.AppendFormat(
                                    "    if (v.date < new ISODate('{0:yyyy-MM-ddTHH:mm:sszzz}')) return false;",
                                    rng.Value.StartDate);
                if (rng.Value.EndDate.HasValue)
                    sb.AppendFormat(
                                    "    if (v.date > new ISODate('{0:yyyy-MM-ddTHH:mm:sszzz}')) return false;",
                                    rng.Value.EndDate);

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
        public static string GetJavascriptFilter(this IQueryCompunded<IDetailQueryAtom> query)
        {
            return GetJavascriptFilter(query, GetJavascriptFilter);
        }

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
                    {
                        sb.AppendFormat("    if (d.title != {0}) return false;", f.Filter.Title);
                        sb.AppendLine();
                    }
                    if (f.Filter.SubTitle != null)
                        if (f.Filter.SubTitle == 00)
                            sb.AppendLine("    if (d.subtitle != null) return false;");
                        else
                        {
                            sb.AppendLine();
                            sb.AppendFormat("    if (d.subtitle != {0}) return false;", f.Filter.SubTitle);
                        }
                    if (f.Filter.Content != null)
                        if (f.Filter.Content == String.Empty)
                            sb.AppendLine("    if (d.content != null) return false;");
                        else
                            sb.AppendFormat(
                                            "    if (d.content != '{0}') return false;",
                                            f.Filter.Content.Replace("\'", "\\\'"));
                    if (f.Filter.Remark != null)
                        if (f.Filter.Remark == String.Empty)
                            sb.AppendLine("    if (d.remark != null) return false;");
                        else
                            sb.AppendFormat(
                                            "    if (d.remark != '{0}') return false;",
                                            f.Filter.Remark.Replace("\'", "\\\'"));
                    if (f.Filter.Fund != null)
                    {
                        sb.AppendFormat("    if (Math.abs(d.fund - {0:r}) > 1e-8)) return false;", f.Filter.Fund);
                        sb.AppendLine();
                    }
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
        public static string GetJavascriptFilter(this IQueryCompunded<IVoucherQueryAtom> query)
        {
            return GetJavascriptFilter(query, GetJavascriptFilter);
        }

        /// <summary>
        ///     ����ƾ֤����ʽ�Ĳ�ѯ
        /// </summary>
        /// <param name="query">����ƾ֤����ʽ</param>
        /// <returns>��ѯ</returns>
        public static IMongoQuery GetQuery(this IQueryCompunded<IVoucherQueryAtom> query)
        {
            return ToWhere(GetJavascriptFilter(query));
        }

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
                sb.Append("    var vfilter = ");
                sb.Append(GetJavascriptFilter(f.VoucherFilter));
                sb.AppendLine(";");
                sb.AppendLine("    if (!vfilter(v)) return false;");

                sb.Append("    var rng = ");
                sb.Append(GetJavascriptFilter(f.Range));
                sb.AppendLine(";");
                sb.AppendLine("    if (!rng(v)) return false;");

                if (f.ForAll)
                {
                    sb.AppendLine("    var i = 0;");
                    sb.AppendLine("    for (i = 0;i < v.detail.length;i++)");
                    sb.AppendLine("        if (!(");
                    sb.Append(GetJavascriptFilter(f.DetailFilter, GetJavascriptFilter));
                    sb.AppendLine(")(v.detail[i]))");
                    sb.AppendLine("            break;");
                    sb.AppendLine("    if (i < v.detail.length)");
                    sb.AppendLine("        return false;");
                }
                else
                {
                    sb.AppendLine("    var i = 0;");
                    sb.AppendLine("    for (i = 0;i < v.detail.length;i++)");
                    sb.AppendLine("        if ((");
                    sb.Append(GetJavascriptFilter(f.DetailFilter, GetJavascriptFilter));
                    sb.AppendLine(")(v.detail[i]))");
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
        public static string GetJavascriptFilter(this IQueryCompunded<IDistributedQueryAtom> query)
        {
            return GetJavascriptFilter(query, GetJavascriptFilter);
        }

        /// <summary>
        ///     ���ڼ���ʽ�Ĳ�ѯ
        /// </summary>
        /// <param name="query">���ڼ���ʽ</param>
        /// <returns>��ѯ</returns>
        public static IMongoQuery GetQuery(this IQueryCompunded<IDistributedQueryAtom> query)
        {
            return ToWhere(GetJavascriptFilter(query));
        }

        /// <summary>
        ///     ԭ�ӷ��ڼ���ʽ��Javascript��ʾ
        /// </summary>
        /// <param name="f">���ڼ���ʽ</param>
        /// <returns>Javascript��ʾ</returns>
        private static string GetJavascriptFilter(IDistributedQueryAtom f)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(a) {");
            if (f == null ||
                f.Filter == null)
                sb.AppendLine("    return true;");
            else
            {
                if (f.Filter.ID.HasValue)
                {
                    sb.AppendFormat(
                                    "    if (a._id.toString() != BinData(3,'{0}')) return false;",
                                    Convert.ToBase64String(f.Filter.ID.Value.ToByteArray()));
                    sb.AppendLine();
                }
                if (f.Filter.Name != null)
                    if (f.Filter.Name == String.Empty)
                        sb.AppendLine("    if (a.name != null) return false;");
                    else
                        sb.AppendFormat(
                                        "    if (a.name != '{0}') return false;",
                                        f.Filter.Name.Replace("\'", "\\\'"));
                if (f.Filter.Remark != null)
                    if (f.Filter.Remark == String.Empty)
                        sb.AppendLine("    if (a.remark != null) return false;");
                    else
                        sb.AppendFormat(
                                        "    if (a.remark != '{0}') return false;",
                                        f.Filter.Remark.Replace("\'", "\\\'"));
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
                return atomFilter(query as TAtom);
            if (query is IQueryAry<TAtom>)
            {
                var f = query as IQueryAry<TAtom>;

                var sb = new StringBuilder();
                sb.AppendLine("function (entity) {");
                sb.AppendLine("    return ");
                switch (f.Operator)
                {
                    case OperatorType.None:
                    case OperatorType.Identity:
                        sb.AppendLine("(");
                        sb.Append(GetJavascriptFilter(f.Filter1, atomFilter));
                        sb.AppendLine(")(entity)");
                        break;
                    case OperatorType.Complement:
                        sb.AppendLine("!(");
                        sb.Append(GetJavascriptFilter(f.Filter1, atomFilter));
                        sb.AppendLine(")(entity)");
                        break;
                    case OperatorType.Union:
                        sb.AppendLine("(");
                        sb.Append(GetJavascriptFilter(f.Filter1, atomFilter));
                        sb.AppendLine(")(entity) ");
                        sb.AppendLine("||");
                        sb.AppendLine(" (");
                        sb.Append(GetJavascriptFilter(f.Filter2, atomFilter));
                        sb.AppendLine(")(entity)");
                        break;
                    case OperatorType.Intersect:
                        sb.AppendLine("(");
                        sb.Append(GetJavascriptFilter(f.Filter1, atomFilter));
                        sb.AppendLine(")(entity) ");
                        sb.AppendLine("&&");
                        sb.AppendLine(" (");
                        sb.Append(GetJavascriptFilter(f.Filter2, atomFilter));
                        sb.AppendLine(")(entity)");
                        break;
                    case OperatorType.Substract:
                        sb.AppendLine("(");
                        sb.Append(GetJavascriptFilter(f.Filter1, atomFilter));
                        sb.AppendLine(")(entity) ");
                        sb.AppendLine("&& !");
                        sb.AppendLine(" (");
                        sb.Append(GetJavascriptFilter(f.Filter2, atomFilter));
                        sb.AppendLine(")(entity)");
                        break;
                    default:
                        throw new ArgumentException("��������δ֪", "query");
                }
                sb.AppendLine(";");
                sb.AppendLine("}");
                return sb.ToString();
            }
            throw new ArgumentException("����ʽ����δ֪", "query");
        }

        /// <summary>
        ///     ������ʽ��Javascript��ʾת��Ϊ��ѯ
        /// </summary>
        /// <param name="js">Javascript��ʾ</param>
        /// <returns>��ѯ</returns>
        private static IMongoQuery ToWhere(string js)
        {
            var code = String.Format("function() {{ return ({0})(this); }}", js);
            return new QueryDocument("$where", code.Replace(Environment.NewLine, String.Empty));
        }
    }
}

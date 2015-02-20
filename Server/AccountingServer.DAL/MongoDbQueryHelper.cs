using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据库查询
    /// </summary>
    internal static class MongoDbQueryHelper
    {
        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetUniqueQuery(string id)
        {
            return Query.EQ("_id", ObjectId.Parse(id));
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetUniqueQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);
        }

        /// <summary>
        ///     按资产过滤器查询
        /// </summary>
        /// <param name="filter">资产过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Asset filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.ID != null)
                lst.Add(Query.EQ("_id", filter.ID.Value.ToBsonValue()));
            if (filter.Name != null)
                lst.Add(
                        filter.Name == String.Empty
                            ? Query.EQ("name", BsonNull.Value)
                            : Query.EQ("name", filter.Name));
            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.Value != null)
                lst.Add(Query.EQ("value", filter.Value));
            if (filter.Salvge != null)
                lst.Add(Query.EQ("salvge", filter.Salvge));
            if (filter.Life != null)
                lst.Add(Query.EQ("life", filter.Life));
            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.DepreciationTitle != null)
                lst.Add(Query.EQ("deptitle", filter.DepreciationTitle));
            if (filter.DevaluationTitle != null)
                lst.Add(Query.EQ("devtitle", filter.DevaluationTitle));
            if (filter.DepreciationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exptitle",
                                 filter.DepreciationExpenseSubTitle != null
                                     ? filter.DepreciationExpenseTitle * 100 + filter.DepreciationExpenseSubTitle
                                     : filter.DepreciationExpenseTitle));
            if (filter.DevaluationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exvtitle",
                                 filter.DevaluationExpenseSubTitle != null
                                     ? filter.DevaluationExpenseTitle * 100 + filter.DevaluationExpenseSubTitle
                                     : filter.DevaluationExpenseTitle));
            if (filter.Method.HasValue)
                switch (filter.Method.Value)
                {
                    case DepreciationMethod.StraightLine:
                        lst.Add(Query.EQ("method", "sl"));
                        break;
                    case DepreciationMethod.SumOfTheYear:
                        lst.Add(Query.EQ("method", "sy"));
                        break;
                    case DepreciationMethod.DoubleDeclineMethod:
                        lst.Add(Query.EQ("method", "dd"));
                        break;
                }
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        /// <summary>
        ///     按摊销过滤器查询
        /// </summary>
        /// <param name="filter">资产过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Amortization filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.ID != null)
                lst.Add(Query.EQ("_id", filter.ID.Value.ToBsonValue()));
            if (filter.Name != null)
                lst.Add(
                        filter.Name == String.Empty
                            ? Query.EQ("name", BsonNull.Value)
                            : Query.EQ("name", filter.Name));
            if (filter.Value != null)
                lst.Add(Query.EQ("value", filter.Value));
            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.TotalDays != null)
                lst.Add(Query.EQ("tday", filter.TotalDays));
            //if (query.Template != null)
            //    throw new NotImplementedException(); TODO: query according to Template
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        /// <summary>
        ///     合取
        /// </summary>
        /// <param name="queries">查询</param>
        /// <returns>合取</returns>
        private static IMongoQuery And(ICollection<IMongoQuery> queries)
        {
            while (queries.Remove(null)) { }
            return queries.Any() ? Query.And(queries) : null;
        }

        /// <summary>
        ///     过滤器的Javascript表示
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(Voucher vfilter)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(v) {");
            if (vfilter == null)
                sb.AppendLine("    return true;");
            else
            {
                if (vfilter.ID != null)
                {
                    sb.AppendFormat("    if (v._id.toString() != '{0}')) return false;", vfilter.ID);
                    sb.AppendLine();
                }
                if (vfilter.Date != null)
                {
                    sb.AppendFormat("    if (v.date != new Date('{0:yyyy-MM-dd}')) return false;", vfilter.Date);
                    sb.AppendLine();
                }
                if (vfilter.Type != null)
                    switch (vfilter.Type)
                    {
                        case VoucherType.Ordinal:
                            sb.AppendLine("    if (v.special != undefined) return false;");
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
                sb.AppendLine("    return true;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     细目过滤器的Javascript表示
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="dir">+1表示只考虑借方，-1表示只考虑贷方，0表示同时考虑借方和贷方</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(VoucherDetail filter, int dir)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(d) {");
            if (dir != 0)
                sb.AppendLine(dir > 0 ? "    if (d.fund <= 0) return false;" : "    if (d.fund >= 0) return false;");
            if (filter == null)
                sb.AppendLine("    return true;");
            else
            {
                if (filter.Title != null)
                {
                    sb.AppendFormat("    if (d.title != {0})) return false;", filter.Title);
                    sb.AppendLine();
                }
                if (filter.SubTitle != null)
                {
                    sb.AppendFormat("    if (d.subtitle != {0})) return false;", filter.SubTitle);
                    sb.AppendLine();
                }
                if (filter.Content != null)
                    if (filter.Content == String.Empty)
                        sb.AppendLine("    if (d.content != null) return false;");
                    else
                        sb.AppendFormat(
                                        "    if (d.content != '{0}') return false;",
                                        filter.Content.Replace("\'", "\\\'"));
                if (filter.Remark != null)
                    if (filter.Remark == String.Empty)
                        sb.AppendLine("    if (d.remark != null) return false;");
                    else
                        sb.AppendFormat(
                                        "    if (d.remark != '{0}') return false;",
                                        filter.Remark.Replace("\'", "\\\'"));
                if (filter.Fund != null)
                {
                    sb.AppendFormat("    if (Math.abs(d.fund - {0:r}) > 1e-8)) return false;", filter.Fund);
                    sb.AppendLine();
                }
                sb.AppendLine("    return true;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     日期过滤器的Javascript表示
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(DateFilter? rng)
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
                    sb.AppendFormat("    if (v.date < new Date('{0:yyyy-MM-dd}')) return false;", rng.Value.StartDate);
                if (rng.Value.EndDate.HasValue)
                    sb.AppendFormat("    if (v.date > new Date('{0:yyyy-MM-dd}')) return false;", rng.Value.EndDate);

                sb.AppendLine("    return true;");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        ///     细目检索式的Javascript表示
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(IDetailQueryCompounded query)
        {
            if (query is IDetailQueryAtom)
            {
                var f = query as IDetailQueryAtom;
                return GetJavascriptFilter(f.Filter, f.Dir);
            }
            if (query is IDetailQueryUnary)
            {
                var f = query as IDetailQueryUnary;

                var sb = new StringBuilder();
                sb.AppendLine("function (d) {");
                sb.AppendLine("    return ");
                switch (f.Operator)
                {
                    case UnaryOperatorType.Identity:
                        break;
                    case UnaryOperatorType.Complement:
                        sb.AppendLine("!");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                sb.AppendLine("(");
                sb.Append(GetJavascriptFilter(f.Filter1));
                sb.AppendLine(")(d);");
                sb.AppendLine("}");
                return sb.ToString();
            }
            if (query is IDetailQueryBinary)
            {
                var f = query as IDetailQueryBinary;

                var sb = new StringBuilder();
                sb.AppendLine("function (d) {");
                sb.AppendLine("    return (");
                sb.Append(GetJavascriptFilter(f.Filter1));
                sb.AppendLine(")(d) ");

                switch (f.Operator)
                {
                    case BinaryOperatorType.Union:
                        sb.AppendLine("&&");
                        break;
                    case BinaryOperatorType.Interect:
                        sb.AppendLine("||");
                        break;
                    case BinaryOperatorType.Substract:
                        sb.AppendLine("&& !");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                sb.AppendLine(" (");
                sb.Append(GetJavascriptFilter(f.Filter2));
                sb.AppendLine(")(d);");
                sb.AppendLine("}");
                return sb.ToString();
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     检索式的Javascript表示
        /// </summary>
        /// <param name="query">检索式</param>
        /// <returns>Javascript表示</returns>
        public static string GetJavascriptFilter(IVoucherQueryCompounded query)
        {
            if (query is IVoucherQueryAtom)
            {
                var f = query as IVoucherQueryAtom;

                var sb = new StringBuilder();
                sb.AppendLine("function (v) {");

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
                    sb.Append(GetJavascriptFilter(f.DetailFilter));
                    sb.AppendLine(")(v.detail[i]))");
                    sb.AppendLine("            break;");
                    sb.AppendLine("    if (i < v.detail.length");
                    sb.AppendLine("        return false;");
                }
                else
                {
                    sb.AppendLine("    var i = 0;");
                    sb.AppendLine("    for (i = 0;i < v.detail.length;i++)");
                    sb.AppendLine("        if ((");
                    sb.Append(GetJavascriptFilter(f.DetailFilter));
                    sb.AppendLine(")(this.detail[i]))");
                    sb.AppendLine("            break;");
                    sb.AppendLine("    if (i >= v.detail.length");
                    sb.AppendLine("        return false;");
                }
                sb.AppendLine("    return true;");
                sb.AppendLine("}");
                return sb.ToString();

            }
            if (query is IVoucherQueryUnary)
            {
                var f = query as IVoucherQueryUnary;

                var sb = new StringBuilder();
                sb.AppendLine("function (v) {");
                sb.AppendLine("    return ");
                switch (f.Operator)
                {
                    case UnaryOperatorType.Identity:
                        break;
                    case UnaryOperatorType.Complement:
                        sb.AppendLine("!");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                sb.AppendLine("(");
                sb.Append(GetJavascriptFilter(f.Filter1));
                sb.AppendLine(")(v);");
                sb.AppendLine("}");
                return sb.ToString();
            }
            if (query is IVoucherQueryBinary)
            {
                var f = query as IVoucherQueryBinary;

                var sb = new StringBuilder();
                sb.AppendLine("function (v) {");
                sb.AppendLine("    return (");
                sb.Append(GetJavascriptFilter(f.Filter1));
                sb.AppendLine(")(v) ");

                switch (f.Operator)
                {
                    case BinaryOperatorType.Union:
                        sb.AppendLine("&&");
                        break;
                    case BinaryOperatorType.Interect:
                        sb.AppendLine("||");
                        break;
                    case BinaryOperatorType.Substract:
                        sb.AppendLine("&& !");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                sb.AppendLine(" (");
                sb.Append(GetJavascriptFilter(f.Filter2));
                sb.AppendLine(")(v);");
                sb.AppendLine("}");
                return sb.ToString();
            }
            throw new InvalidOperationException();
        }
    }
}

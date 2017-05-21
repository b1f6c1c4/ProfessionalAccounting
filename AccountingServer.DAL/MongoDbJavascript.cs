using System;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    internal abstract class MongoDbJavascriptVisitor<TAtom> : IQueryVisitor<TAtom, string>
        where TAtom : class
    {
        public abstract string Visit(TAtom query);

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

    internal class MongoDbJavascriptDetail : MongoDbJavascriptVisitor<IDetailQueryAtom>
    {
        public override string Visit(IDetailQueryAtom query)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function(d) {");
            if (query != null)
            {
                if (query.Dir != 0)
                    sb.AppendLine(
                        query.Dir > 0
                            ? "    if (d.fund <= 0) return false;"
                            : "    if (d.fund >= 0) return false;");
                if (query.Filter?.Currency != null)
                    if (query.Filter.Currency == VoucherDetail.BaseCurrency)
                        sb.AppendLine("    if (d.currency != null) return false;");
                    else
                        sb.AppendLine(
                            $"    if (d.currency != '{query.Filter.Currency.Replace("\'", "\\\'")}') return false;");
                if (query.Filter?.Title != null)
                    sb.AppendLine($"    if (d.title != {query.Filter.Title}) return false;");
                if (query.Filter?.SubTitle != null)
                    sb.AppendLine(
                        query.Filter.SubTitle == 00
                            ? "    if (d.subtitle != null) return false;"
                            : $"    if (d.subtitle != {query.Filter.SubTitle}) return false;");
                if (query.Filter?.Content != null)
                    sb.AppendLine(
                        query.Filter.Content == string.Empty
                            ? "    if (d.content != null) return false;"
                            : $"    if (d.content != '{query.Filter.Content.Replace("\'", "\\\'")}') return false;");
                if (query.Filter?.Remark != null)
                    sb.AppendLine(
                        query.Filter.Remark == string.Empty
                            ? "    if (d.remark != null) return false;"
                            : $"    if (d.remark != '{query.Filter.Remark.Replace("\'", "\\\'")}') return false;");
                if (query.Filter?.Fund != null)
                    sb.AppendLine(
                        $"    if (Math.abs(d.fund - {query.Filter.Fund:r}) > {VoucherDetail.Tolerance:R}) return false;");
            }
            sb.AppendLine("    return true;");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}

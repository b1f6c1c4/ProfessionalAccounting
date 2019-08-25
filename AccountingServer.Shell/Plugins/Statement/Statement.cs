using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement
{
    /// <summary>
    ///     自动对账
    /// </summary>
    internal class Statement : PluginBase
    {
        public Statement(Accountant accountant) : base(accountant) { }

        private IVoucherDetailQuery Regularize(IVoucherDetailQuery filt)
        {
            if (filt.DetailEmitFilter != null)
                return filt;

            if (filt.VoucherQuery is IVoucherQueryAtom dQuery)
                return new StmtVoucherDetailQuery(filt.VoucherQuery, dQuery.DetailFilter);

            throw new ArgumentException("不指定细目映射检索式时记账凭证检索式为复合检索式", nameof(filt));
        }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var csv = expr;
            expr = ParsingF.Line(ref csv);
            var parsed = new CsvParser(ParsingF.Optional(ref expr, "-"));

            var sb = new StringBuilder();
            if (ParsingF.Optional(ref expr, "mark"))
            {
                var filt = Regularize(ParsingF.DetailQuery(ref expr));
                ParsingF.Optional(ref expr, "as");
                var marker = ParsingF.Token(ref expr);
                ParsingF.Eof(expr);
                parsed.Parse(csv);
                sb.AppendLine($"{parsed.Items.Count} parsed");
                RunMark(filt, parsed, marker, sb);
            }
            else if (ParsingF.Optional(ref expr, "unmark"))
            {
                var filt = Regularize(ParsingF.DetailQuery(ref expr));
                ParsingF.Eof(expr);
                RunUnmark(filt, sb);
            }
            else if (ParsingF.Optional(ref expr, "check"))
            {
                var filt = Regularize(ParsingF.DetailQuery(ref expr));
                ParsingF.Eof(expr);
                parsed.Parse(csv);
                sb.AppendLine($"{parsed.Items.Count} parsed");
                RunCheck(filt, parsed, sb);
            }
            else
            {
                ParsingF.Optional(ref expr, "auto");
                var filt = Regularize(ParsingF.DetailQuery(ref expr));
                ParsingF.Optional(ref expr, "as");
                var marker = ParsingF.Token(ref expr);
                ParsingF.Eof(expr);
                parsed.Parse(csv);
                sb.AppendLine($"{parsed.Items.Count} parsed");
                var markerFilt = new StmtVoucherDetailQuery(
                    filt.VoucherQuery,
                    new IntersectQueries<IDetailQueryAtom>(
                        filt.DetailEmitFilter.DetailFilter,
                        new StmtDetailQuery(marker)));
                var nullFilt = new StmtVoucherDetailQuery(
                    filt.VoucherQuery,
                    new IntersectQueries<IDetailQueryAtom>(
                        filt.DetailEmitFilter.DetailFilter,
                        new StmtDetailQuery("")));
                RunUnmark(markerFilt, sb);
                RunMark(nullFilt, parsed, marker, sb);
                RunCheck(markerFilt, parsed, sb);
            }

            return new PlainText(sb.ToString());
        }

        private void RunMark(IVoucherDetailQuery filt, CsvParser parsed, string marker, StringBuilder sb)
        {
            if (filt.IsDangerous())
                throw new SecurityException("检测到弱检索式");

            var marked = 0;
            var remarked = 0;
            var converted = 0;
            var res = Accountant.SelectVouchers(filt.VoucherQuery).ToList();
            foreach (var b in parsed.Items)
            {
                bool Trial(bool date)
                {
                    var resx = date
                        ? res.Where(v => v.Date == b.Date)
                        : res.OrderBy(v => Math.Abs((v.Date.Value - b.Date).TotalDays));
                    var voucher = resx
                        .Where(v => v.Details.Any(d => (d.Fund.Value - b.Fund).IsZero() && d.IsMatch(filt.DetailEmitFilter.DetailFilter)))
                        .FirstOrDefault();
                    if (voucher == null)
                        return false;

                    var o = voucher.Details.First(d => (d.Fund.Value - b.Fund).IsZero() && d.IsMatch(filt.DetailEmitFilter.DetailFilter));
                    if (o.Remark == null)
                        marked++;
                    else if (o.Remark == marker)
                        remarked++;
                    else
                        converted++;

                    o.Remark = marker;
                    Accountant.Upsert(voucher);
                    return true;
                }

                if (Trial(true) || Trial(false))
                    continue;

                sb.AppendLine(b.Raw);
            }

            sb.AppendLine($"{marked} marked");
            sb.AppendLine($"{remarked} remarked");
            sb.AppendLine($"{converted} converted");
        }

        private void RunUnmark(IVoucherDetailQuery filt, StringBuilder sb)
        {
            if (filt.IsDangerous())
                throw new SecurityException("检测到弱检索式");

            var cnt = 0;
            var cntAll = 0;
            var res = Accountant.SelectVouchers(filt.VoucherQuery);
            foreach (var v in res)
            {
                foreach (var d in v.Details)
                {
                    if (!d.IsMatch(filt.DetailEmitFilter.DetailFilter))
                        continue;

                    cntAll++;
                    if (d.Remark == null)
                        continue;

                    d.Remark = null;
                    cnt++;
                }

                Accountant.Upsert(v);
            }

            sb.AppendLine($"{cntAll} selected");
            sb.AppendLine($"{cnt} unmarked");
        }

        private void RunCheck(IVoucherDetailQuery filt, CsvParser parsed, StringBuilder sb)
        {
            if (filt.IsDangerous())
                throw new SecurityException("检测到弱检索式");

            var res = Accountant.SelectVouchers(filt.VoucherQuery);
            var lst = new List<BankItem>(parsed.Items);
            foreach (var v in res)
                foreach (var d in v.Details)
                {
                    if (!d.IsMatch(filt.DetailEmitFilter.DetailFilter))
                        continue;

                    var obj1 = lst.FirstOrDefault(b => (b.Fund - d.Fund.Value).IsZero() && b.Date == v.Date);
                    if (obj1 != null)
                    {
                        lst.Remove(obj1);
                        continue;
                    }

                    var obj2 = lst
                        .Where(b => (b.Fund - d.Fund.Value).IsZero())
                        .OrderBy(b => Math.Abs((b.Date - v.Date.Value).TotalDays))
                        .FirstOrDefault();
                    if (obj2 != null)
                    {
                        lst.Remove(obj2);
                        continue;
                    }

                    sb.AppendLine($"{v.ID.Quotation('^')} {v.Date.AsDate()} {d.Content} {d.Fund.AsCurrency(d.Currency)}");
                }

            foreach (var b in lst)
                sb.AppendLine(b.Raw);
        }

        private sealed class StmtVoucherDetailQuery : IVoucherDetailQuery
        {
            public StmtVoucherDetailQuery(IQueryCompunded<IVoucherQueryAtom> v, IQueryCompunded<IDetailQueryAtom> d)
            {
                VoucherQuery = v;
                DetailEmitFilter = new StmtEmit { DetailFilter = d };
            }

            public IQueryCompunded<IVoucherQueryAtom> VoucherQuery { get; }

            public IEmit DetailEmitFilter { get; }
        }

        private class StmtEmit : IEmit
        {
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get; set; }
        }

        private sealed class StmtVoucherQuery : IVoucherQueryAtom
        {
            public bool ForAll { get; set; }

            public Voucher VoucherFilter { get; set; }

            public DateFilter Range { get; set; }

            public bool IsDangerous() =>
                VoucherFilter.IsDangerous() && Range.IsDangerous() && DetailFilter.IsDangerous();

            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get; set; }

            public T Accept<T>(IQueryVisitor<IVoucherQueryAtom, T> visitor) => visitor.Visit(this);
        }

        private sealed class StmtDetailQuery : IDetailQueryAtom
        {
            public StmtDetailQuery(string marker)
            {
                Filter = new VoucherDetail { Remark = marker };
            }

            public TitleKind? Kind => null;

            public VoucherDetail Filter { get; }

            public int Dir => 0;

            public bool IsDangerous() => Filter.IsDangerous();

            public T Accept<T>(IQueryVisitor<IDetailQueryAtom, T> visitor) => visitor.Visit(this);
        }
    }
}

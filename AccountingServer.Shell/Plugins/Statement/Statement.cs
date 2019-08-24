using System;
using System.Collections.Generic;
using System.Linq;
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
	///		自动对账
	///	</summary>
	internal class Statement : PluginBase
	{
		public Statement(Accountant accountant) : base(accountant) { }

		/// <inheritdoc />
		public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
		{
			var id = expr.IndexOf('\n');
			var csv = id < 0 ? string.Empty : expr.Substring(id + 1);
			if (id >= 0)
				expr = expr.Substring(0, id);

			var parsed = new CsvParser();

			var sb = new StringBuilder();
			if (ParsingF.Optional(ref expr, "mark"))
			{
				var filt = ParsingF.DetailQuery(ref expr);
				ParsingF.Optional(ref expr, "as");
				var marker = ParsingF.Token(ref expr);
				ParsingF.Eof(ref expr);
				parsed.Parse(csv);
				RunMark(filt, parsed, marker, sb);
			}
			else if (ParsingF.Optional(ref expr, "unmark"))
			{
				var filt = ParsingF.DetailQuery(ref expr);
				ParsingF.Eof(ref expr);
				RunUnmark(filt, sb);
			}
			else if (ParsingF.Optional(ref expr, "check"))
			{
				var filt = ParsingF.DetailQuery(ref expr);
				ParsingF.Eof(ref expr);
				parsed.Parse(csv);
				RunCheck(filt, parsed, sb);
			}
			else // TODO: is this rationale?
			{
				ParsingF.Optional(ref expr, "auto");
				var filt = ParsingF.DetailQuery(ref expr);
				ParsingF.Optional(ref expr, "as");
				var marker = ParsingF.Token(ref expr);
				ParsingF.Eof(ref expr);
				parsed.Parse(csv);
				var combFilt = new IntersectQueries<IDetailQueryAtom>(
					filt,
					new StmtDetailQuery(marker));
				RunUnmark(combFilt, sb);
				RunMark(filt, parsed, marker, sb);
				RunCheck(combFilt, parsed, sb);
			}

			return new PlainText(sb.ToString());
		}

		private void RunMark(IQueryCompunded<IDetailQueryAtom> filt, string marker, CsvParser parsed, StringBuilder sb)
		{
			var marked = 0;
			var remarked = 0;
			var converted = 0;
			var res = Accountant.SelectVouchers(new StmtVoucherQuery(filt)).ToList();
			foreach (var b in parsed.Items)
			{
				bool Trial(bool date)
				{
					var resx = date
						? res.Where(v => v.Date == b.Date)
						: res.OrderBy(b => Math.Abs((b.Date - v.Date).TotalDays));
					var v = resx
						.Where(v => v.Details.Any(d => d.IsMatch(filt)))
						.FirstOrDefault();
					if (v == null)
						return false;

					var o = v.Details.First(d => d.IsMatch(filt));
					if (o.Remark == null)
						marked++;
					else if (o.Remark == marker)
						remarked++;
					else
						converted++;

					o.Remark = marker;
					Accountant.Upsert(v);
					return true;
				}

				if (Trail(true) || Trail(false))
					continue;

				sb.AppendLine(b.Raw);
			}
		}

		private void RunUnmark(IQueryCompunded<IDetailQueryAtom> filt, CsvParser parsed, StringBuilder sb)
		{
			var cnt = 0;
			var res = Accountant.SelectVouchers(new StmtVoucherQuery(filt));
			foreach (var v in res)
			{
				foreach (var d in v.Details)
				{
					if (!d.IsMatch(filt) || d.Remark != null)
						continue;

					d.Remark = null;
					cnt++;
				}

				Accountant.Upsert(v);
			}

			sb.AppendLine($"{cnt} unmarked");
		}

		private void RunCheck(IQueryCompunded<IDetailQueryAtom> filt, CsvParser parsed, StringBuilder sb)
		{
			var res = Accountant.SelectVouchers(new StmtVoucherQuery(filt));
			var lst = new List<BankItem>(parsed.Items);
			foreach (var v in res)
				foreach (var d in v.Details)
				{
					if (!d.IsMatch(filt))
						continue;

					var obj1 = lst.FirstOrDefault(b => (b.Fund - d.Fund.Value).IsZero() && b.Date == v.Date);
					if (obj1 != null)
					{
						lst.Remove(obj1);
						continue;
					}

					var obj2 = lst
						.Where(b => (b.Fund - d.Fund.Value).IsZero())
						.OrderBy(b => Math.Abs((b.Date - v.Date).TotalDays))
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

		private sealed class StmtVoucherQuery : IVoucherQueryAtom
		{
			public StmtVoucherQuery(IQueryCompunded<IDetailQueryAtom> filt)
			{
				DetailFilter = filt;
			}

			public bool ForAll => false;

			public Voucher VoucherFilter { get; } = new Voucher();

			public DateFilter Range => DateFilter.Unconstrained;

			public IQueryCompunded<IDetailQueryAtom> DetailFilter { get; }
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
		}
	}
}

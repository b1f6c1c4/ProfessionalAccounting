using System;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Carry;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Reimburse
{
    /// <summary>
    ///     报销
    /// </summary>
    internal class Reimburse : PluginBase
    {
        public static IConfigManager<ReimTemplates> Templates { private get; set; } =
            new ConfigManager<ReimTemplates>("Reim.xml");

        public Reimburse(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            DateFilter the;
            if (string.IsNullOrWhiteSpace(expr))
                the = DateRange;
            else
            {
                the = ParsingF.Range(ref expr) ?? throw new ArgumentException("语法错误", nameof(expr));
                ParsingF.Eof(expr);
            }

            return DoReimbursement(the, out var _);
        }

        public static DateFilter DateRange
        {
            get
            {
                DateFilter the;
                var now = DateTime.Today;
                if (now.Day > Templates.Config.Day)
                    the = new DateFilter(
                        new DateTime(now.Year, now.Month, Templates.Config.Day + 1).CastUtc(),
                        new DateTime(now.Year, now.Month, Templates.Config.Day).AddMonths(1).CastUtc());
                else
                    the = new DateFilter(
                        new DateTime(now.Year, now.Month, Templates.Config.Day + 1).AddMonths(-1).CastUtc(),
                        new DateTime(now.Year, now.Month, Templates.Config.Day).CastUtc());
                return the;
            }
        }

        /// <inheritdoc />
        public override string ListHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ListHelp());
            foreach (var reim in Templates.Config.Templates)
                sb.AppendLine(
                    $"{reim.Name.CPadRight(18)}{reim.Query} {(reim.IsLeftExtended ? "[~X]" : "[X]")} `{(reim.ByTitle ? "t" : "")}{(reim.BySubTitle ? "s" : "")}{(reim.ByCountent ? "c" : "")}{(reim.ByRemark ? "r" : "")}{(reim.ByTitle || reim.BySubTitle || reim.ByCountent || reim.ByRemark ? "" : "v")}");

            return sb.ToString();
        }

        /// <summary>
        ///     执行报销
        /// </summary>
        /// <param name="rng">财月</param>
        /// <param name="val">金额</param>
        /// <returns>执行结果</returns>
        public IQueryResult DoReimbursement(DateFilter rng, out double val)
        {
            val = 0;
            var sb = new StringBuilder();
            foreach (var reim in Templates.Config.Templates)
            {
                var gq = Accountant
                    .RunGroupedQuery(
                        $"{reim.Query} [{(reim.IsLeftExtended ? "" : rng.StartDate.AsDate())}~{rng.EndDate.AsDate()}] G `C{(reim.ByTitle ? "t" : "")}{(reim.BySubTitle ? "s" : "")}{(reim.ByCountent ? "c" : "")}{(reim.ByRemark ? "r" : "")}");
                foreach (var grp in gq.GroupByCurrency())
                {
                    var curr = grp.Key;
                    // ReSharper disable once PossibleInvalidOperationException
                    var ratio = curr == VoucherDetail.BaseCurrency
                        ? 1
                        : ExchangeFactory.Instance.From(rng.EndDate.Value, curr);
                    foreach (var b in grp)
                    {
                        sb.Append(reim.Name);
                        if (reim.ByTitle)
                            sb.Append($"/{TitleManager.GetTitleName(b.Title)}");
                        if (reim.BySubTitle)
                            sb.Append($"/{TitleManager.GetTitleName(b.Title, b.SubTitle)}");
                        if (reim.ByCountent &&
                            !reim.HideCountent)
                            sb.Append($"/{b.Content}");
                        if (reim.ByRemark)
                            sb.Append($"/{b.Remark}");
                        sb.AppendLine($"\t{curr}\t{b.Fund}\t{ratio}\t{reim.Ratio}");

                        val += ratio * b.Fund * reim.Ratio;
                    }
                }
            }

            return new UnEditableText(sb.ToString());
        }
    }
}

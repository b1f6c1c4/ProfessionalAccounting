using System;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Subtotal;
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
                var query = ParsingF.GroupedQuery(
                    $"{reim.Query} [{(reim.IsLeftExtended ? "" : rng.StartDate.AsDate())}~{rng.EndDate.AsDate()}] G `C{(reim.ByTitle ? "t" : "")}{(reim.BySubTitle ? "s" : "")}{(reim.ByCountent ? "c" : "")}{(reim.ByRemark ? "r" : "")}");
                var gq = Accountant.SelectVoucherDetailsGrouped(query);
                // ReSharper disable once PossibleInvalidOperationException
                foreach (var grp in gq.Items.Cast<ISubtotalCurrency>())
                {
                    var curr = grp.Currency;
                    // ReSharper disable once PossibleInvalidOperationException
                    var ratio = curr == VoucherDetail.BaseCurrency
                        ? 1
                        : ExchangeFactory.Instance.From(rng.EndDate.Value, curr);

                    val += ratio * grp.Fund * reim.Ratio;

                    sb.Append(
                        new ReimburseVisitor(curr, ratio, reim.Ratio, reim.Name).PresentSubtotal(
                            grp,
                            query.Subtotal));
                }
            }

            return new UnEditableText(sb.ToString());
        }
    }

    internal class ReimburseVisitor : StringSubtotalVisitor
    {
        private readonly string m_Curr;
        private readonly double m_ExgRatio;
        private readonly double m_Ratio;

        public ReimburseVisitor(string curr, double exgRatio, double ratio, string path)
        {
            m_Curr = curr;
            m_ExgRatio = exgRatio;
            m_Ratio = ratio;
            m_Path = path;
        }

        /// <summary>
        ///     使用分隔符连接字符串
        /// </summary>
        /// <param name="path">原字符串</param>
        /// <param name="token">要连接上的字符串</param>
        /// <param name="interval">分隔符</param>
        /// <returns>新字符串</returns>
        private static string Merge(string path, string token, string interval = "/")
        {
            if (path.Length == 0)
                return token;

            return path + interval + token;
        }

        private string m_Path;
        private int? m_Title;

        private void ShowSubtotal(ISubtotalResult sub)
        {
            if (sub.Items == null)
                Sb.AppendLine($"{m_Path}\t{m_Curr}\t{sub.Fund:R}\t{m_ExgRatio}\t{m_Ratio}");
            else
                foreach (var item in sub.Items)
                    item.Accept(this);
        }

        public override void Visit(ISubtotalRoot sub) => ShowSubtotal(sub);

        public override void Visit(ISubtotalDate sub)
        {
            var prev = m_Path;
            m_Path = Merge(m_Path, sub.Date.AsDate(sub.Level));
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalCurrency sub) => ShowSubtotal(sub);

        public override void Visit(ISubtotalTitle sub)
        {
            var prev = m_Path;
            m_Path = Merge(m_Path, TitleManager.GetTitleName(sub.Title));
            m_Title = sub.Title;
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalSubTitle sub)
        {
            var prev = m_Path;
            m_Path = Merge(m_Path, TitleManager.GetTitleName(m_Title, sub.SubTitle));
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalContent sub)
        {
            var prev = m_Path;
            m_Path = Merge(m_Path, sub.Content);
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalRemark sub)
        {
            var prev = m_Path;
            m_Path = Merge(m_Path, sub.Remark);
            ShowSubtotal(sub);
            m_Path = prev;
        }
    }
}

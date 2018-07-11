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

namespace AccountingServer.Shell.Plugins.Composite
{
    /// <summary>
    ///     复合查询
    /// </summary>
    internal class Composite : PluginBase
    {
        public static IConfigManager<CompositeTemplates> Templates { private get; set; } =
            new ConfigManager<CompositeTemplates>("Composite.xml");

        public Composite(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            Template temp = null;
            if (ParsingF.Token(ref expr, true, t => (temp = GetTemplate(t)) != null) == null)
                throw new ArgumentException("找不到模板", nameof(expr));

            DateFilter the;
            if (string.IsNullOrWhiteSpace(expr))
                the = DateRange(temp.Day);
            else
            {
                the = ParsingF.Range(ref expr) ?? throw new ArgumentException("语法错误", nameof(expr));
                ParsingF.Eof(expr);
            }

            return DoInquiry(the, temp, out var _);
        }

        public static Template GetTemplate(string name) => Templates.Config.Templates.SingleOrDefault(
            p => p.Name == name);

        public static DateFilter DateRange(int day)
        {
            DateFilter the;
            var now = DateTime.Today;
            if (day == 0)
                return new DateFilter(
                    new DateTime(now.Year, now.Month, 1).CastUtc(),
                    new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1).CastUtc());

            if (now.Day > day)
                the = new DateFilter(
                    new DateTime(now.Year, now.Month, day + 1).CastUtc(),
                    new DateTime(now.Year, now.Month, day).AddMonths(1).CastUtc());
            else
                the = new DateFilter(
                    new DateTime(now.Year, now.Month, day + 1).AddMonths(-1).CastUtc(),
                    new DateTime(now.Year, now.Month, day).CastUtc());
            return the;
        }

        /// <inheritdoc />
        public override string ListHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ListHelp());
            foreach (var tmp in Templates.Config.Templates)
                sb.AppendLine(tmp.Name);

            return sb.ToString();
        }

        /// <summary>
        ///     执行查询
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <param name="inq">查询</param>
        /// <param name="val">金额</param>
        /// <returns>执行结果</returns>
        public IQueryResult DoInquiry(DateFilter rng, BaseInquiry inq, out double val)
        {
            var visitor = new InquiriesVisitor(Accountant, rng);
            val = inq.Accept(visitor);
            return new UnEditableText(visitor.Result);
        }

        /// <summary>
        ///     使用分隔符连接字符串
        /// </summary>
        /// <param name="path">原字符串</param>
        /// <param name="token">要连接上的字符串</param>
        /// <param name="interval">分隔符</param>
        /// <returns>新字符串</returns>
        public static string Merge(string path, string token, string interval = "/")
        {
            if (path.Length == 0)
                return token;

            return path + interval + token;
        }
    }

    internal class InquiriesVisitor : IInquiryVisitor<double>
    {
        private readonly StringBuilder m_Sb = new StringBuilder();
        private readonly DateFilter m_Rng;
        private readonly Accountant m_Accountant;

        private string m_Path = "";

        public InquiriesVisitor(Accountant accountant, DateFilter rng)
        {
            m_Accountant = accountant;
            m_Rng = rng;
        }

        public string Result => m_Sb.ToString();

        public double Visit(InquiriesHub inq)
        {
            var tmp = m_Path;
            m_Path = Composite.Merge(m_Path, inq.Name);
            var res = inq.Inquiries.Sum(q => q.Accept(this));
            m_Path = tmp;
            return res;
        }

        public double Visit(Inquiry inq)
        {
            var val = 0D;
            IFundFormatter fmt = new BaseFundFormatter();
            if (!double.IsNaN(inq.Ratio))
                fmt = new RatioDecorator(fmt, inq.Ratio);

            var query = ParsingF.GroupedQuery(
                $"{inq.Query} [{(inq.IsLeftExtended ? "" : m_Rng.StartDate.AsDate())}~{m_Rng.EndDate.AsDate()}] {(inq.General ? "G" : "")} `{(inq.ByCurrency ? "C" : "")}{(inq.ByTitle ? "t" : "")}{(inq.BySubTitle ? "s" : "")}{(inq.ByContent ? "c" : "")}{(inq.ByRemark ? "r" : "")}{(inq.ByCurrency || inq.ByTitle || inq.BySubTitle || inq.ByContent || inq.ByRemark ? "" : "v")}{(!inq.ByCurrency ? "X" : "")}");
            var gq = m_Accountant.SelectVoucherDetailsGrouped(query);
            if (inq.ByCurrency)
                foreach (var grp in gq.Items.Cast<ISubtotalCurrency>())
                {
                    var curr = grp.Currency;
                    // ReSharper disable once PossibleInvalidOperationException
                    var ratio = curr == BaseCurrency.Now
                        ? 1
                        : ExchangeFactory.Instance.From(m_Rng.EndDate.Value, curr);

                    var theFmt = new CurrencyDecorator(fmt, curr, ratio);

                    val += theFmt.GetFund(grp.Fund);

                    m_Sb.Append(
                        new SubtotalVisitor(Composite.Merge(m_Path, inq.Name), theFmt, inq.HideContent)
                            .PresentSubtotal(grp, query.Subtotal));
                }
            else
            {
                val += fmt.GetFund(gq.Fund);

                m_Sb.Append(
                    new SubtotalVisitor(Composite.Merge(m_Path, inq.Name), fmt, inq.HideContent)
                        .PresentSubtotal(gq, query.Subtotal));
            }

            return val;
        }
    }

    internal interface IFundFormatter
    {
        double GetFund(double value);

        string GetFundString(double value);
    }

    internal class BaseFundFormatter : IFundFormatter
    {
        public double GetFund(double value) => value;
        public string GetFundString(double value) => $"{value:R}";
    }

    internal class BaseFundFormatterDecorator : IFundFormatter
    {
        protected readonly IFundFormatter Internal;

        protected BaseFundFormatterDecorator(IFundFormatter fmt) => Internal = fmt;

        public virtual double GetFund(double value) => Internal.GetFund(value);
        public virtual string GetFundString(double value) => Internal.GetFundString(value);
    }

    internal class CurrencyDecorator : BaseFundFormatterDecorator
    {
        private readonly string m_Curr;
        private readonly double m_ExgRatio;

        public CurrencyDecorator(IFundFormatter fmt, string curr, double exgRatio) : base(fmt)
        {
            m_Curr = curr;
            m_ExgRatio = exgRatio;
        }

        public override double GetFund(double value) => Internal.GetFund(value) * m_ExgRatio;
        public override string GetFundString(double value) => $"{m_Curr}\t{m_ExgRatio}\t{Internal.GetFundString(value)}";
    }

    internal class RatioDecorator : BaseFundFormatterDecorator
    {
        private readonly double m_Ratio;

        public RatioDecorator(IFundFormatter fmt, double ratio) : base(fmt) => m_Ratio = ratio;

        public override double GetFund(double value) => Internal.GetFund(value) * m_Ratio;
        public override string GetFundString(double value) => $"{Internal.GetFundString(value)}\t{m_Ratio}";
    }

    internal class SubtotalVisitor : StringSubtotalVisitor
    {
        private readonly IFundFormatter m_Formatter;
        private readonly bool m_HideContent;

        public SubtotalVisitor(string path, IFundFormatter formatter, bool hideContent)
        {
            m_Path = path;
            m_Formatter = formatter;
            m_HideContent = hideContent;
        }

        private string m_Path;
        private int? m_Title;

        private void ShowSubtotal(ISubtotalResult sub)
        {
            if (sub.Items == null)
                Sb.AppendLine($"{m_Path}\t{m_Formatter.GetFundString(sub.Fund)}");
            VisitChildren(sub);
        }

        public override void Visit(ISubtotalRoot sub) => ShowSubtotal(sub);

        public override void Visit(ISubtotalDate sub)
        {
            var prev = m_Path;
            m_Path = Composite.Merge(m_Path, sub.Date.AsDate(sub.Level));
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalCurrency sub)
        {
            Depth++; // Hack
            ShowSubtotal(sub);
        }

        public override void Visit(ISubtotalTitle sub)
        {
            var prev = m_Path;
            m_Path = Composite.Merge(m_Path, TitleManager.GetTitleName(sub.Title));
            m_Title = sub.Title;
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalSubTitle sub)
        {
            var prev = m_Path;
            m_Path = Composite.Merge(m_Path, TitleManager.GetTitleName(m_Title, sub.SubTitle));
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalContent sub)
        {
            var prev = m_Path;
            if (!m_HideContent)
                m_Path = Composite.Merge(m_Path, sub.Content);
            ShowSubtotal(sub);
            m_Path = prev;
        }

        public override void Visit(ISubtotalRemark sub)
        {
            var prev = m_Path;
            m_Path = Composite.Merge(m_Path, sub.Remark);
            ShowSubtotal(sub);
            m_Path = prev;
        }
    }
}

using System;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;
using static AccountingServer.BLL.Parsing.ParsingHelperF;

namespace AccountingServer.Plugins.Utilities
{
    /// <summary>
    ///     常见记账凭证自动填写
    /// </summary>
    public class Utilities : PluginBase
    {
        private static readonly CustomManager<UtilTemplates> Templates;

        static Utilities()
        {
            Templates = new CustomManager<UtilTemplates>("Util.xml");
        }

        public Utilities(Accountant accountant, AccountingShell shell) : base(accountant, shell) { }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            var count = 0;
            foreach (var par in pars)
            {
                var voucher = GenerateVoucher(par);
                if (voucher != null)
                    if (Accountant.Upsert(voucher))
                        count++;
            }
            return new NumberAffected(count);
        }

        /// <inheritdoc />
        public override string ListHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ListHelp());
            foreach (var util in Templates.Config.Templates)
                sb.AppendLine($"{util.Name}\t\t{util.Description}");
            return sb.ToString();
        }

        /// <summary>
        ///     根据参数生成记账凭证
        /// </summary>
        /// <param name="par">参数</param>
        /// <returns>记账凭证</returns>
        private Voucher GenerateVoucher(string par)
        {
            DateTime? time;
            try
            {
                time = ParsingF.UniqueTime(ref par);
            }
            catch (Exception)
            {
                time = DateTime.Today;
            }
            var sp = par.Trim(' ').Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var template = Templates.Config.Templates.FirstOrDefault(t => t.Name == sp[0]);
            if (template == null)
                return null;
            var num = 1D;
            if (template.TemplateType == UtilTemplateType.Value ||
                template.TemplateType == UtilTemplateType.Fill)
            {
                double val;
                if (sp.Length == 2)
                    val = Convert.ToDouble(sp[1]);
                else
                {
                    if (!template.Default.HasValue)
                        return null;
                    val = template.Default.Value;
                }

                if (template.TemplateType == UtilTemplateType.Value)
                    num = val;
                else
                {
                    var grp =
                        ParsingF.GroupedQuery(
                                              time.HasValue
                                                  ? $"{template.Query} [~{time:yyyyMMdd}] ``v"
                                                  : $"{template.Query} [null] ``v");
                    var arr = Accountant.SelectVoucherDetailsGrouped(grp).ToArray();
                    if (arr.Length == 0)
                        num = val;
                    else
                        num = arr[0].Fund - val;
                }
            }
            if (num.IsZero())
                return null;

            return MakeVoucher(template, num, time);
        }

        /// <summary>
        ///     从模板生成记账凭证
        /// </summary>
        /// <param name="template">记账凭证模板</param>
        /// <param name="num">倍乘</param>
        /// <param name="time">日期</param>
        /// <returns></returns>
        private static Voucher MakeVoucher(Voucher template, double num, DateTime? time)
        {
            var lst =
                template.Details
                        .Select(
                                d =>
                                new VoucherDetail
                                    {
                                        Title = d.Title,
                                        SubTitle = d.SubTitle,
                                        Content = d.Content,
                                        Fund = num * d.Fund,
                                        Remark = d.Remark
                                    }).ToList();
            var voucher = new Voucher
                              {
                                  Date = time,
                                  Remark = template.Remark,
                                  Type = template.Type,
                                  Currency = template.Currency,
                                  Details = lst
                              };
            return voucher;
        }
    }
}

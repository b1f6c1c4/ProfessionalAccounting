using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Utilities
{
    /// <summary>
    ///     常见记账凭证自动填写
    /// </summary>
    internal class Utilities : PluginBase
    {
        private static readonly ConfigManager<UtilTemplates> Templates =
            new ConfigManager<UtilTemplates>("Util.xml");

        public Utilities(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            var count = 0;
            while (!string.IsNullOrWhiteSpace(expr))
            {
                var voucher = GenerateVoucher(ref expr);
                if (voucher == null)
                    continue;

                if (Accountant.Upsert(voucher))
                    count++;
            }

            return new NumberAffected(count);
        }

        /// <inheritdoc />
        public override string ListHelp()
        {
            var sb = new StringBuilder();
            sb.Append(base.ListHelp());
            foreach (var util in Templates.Config.Templates)
                sb.AppendLine($"{util.Name,20}{util.Description}");

            return sb.ToString();
        }

        /// <summary>
        ///     根据表达式生成记账凭证
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>记账凭证</returns>
        private Voucher GenerateVoucher(ref string expr)
        {
            var time = Parsing.UniqueTime(ref expr) ?? DateTime.Today;
            var abbr = Parsing.Token(ref expr);

            var template = Templates.Config.Templates.FirstOrDefault(t => t.Name == abbr) ??
                throw new KeyNotFoundException($"找不到常见记账凭证{abbr}");

            var num = 1D;
            if (template.TemplateType == UtilTemplateType.Value ||
                template.TemplateType == UtilTemplateType.Fill)
            {
                var valt = Parsing.Double(ref expr);
                var val = valt.HasValue
                    ? valt.Value
                    : (template.Default ?? throw new ApplicationException($"常见记账凭证{template.Name}没有默认值"));

                if (template.TemplateType == UtilTemplateType.Value)
                    num = val;
                else
                {
                    var arr = Accountant
                        .RunGroupedQuery($"{template.Query} [~{time:yyyyMMdd}] ``v")
                        .ToArray();
                    if (arr.Length == 0)
                        num = val;
                    else
                        num = arr[0].Fund - val;
                }
            }

            return num.IsZero() ? null : MakeVoucher(template, num, time);
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
                                })
                    .ToList();
            var voucher = new Voucher
                {
                    Date = time,
                    Remark = template.Remark,
                    Type = template.Type,
                    Details = lst
                };
            return voucher;
        }
    }
}

using System;
using System.Collections.Generic;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Subtotal;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.VoucherCount
{
    /// <summary>
    ///     统计记账凭证数量
    /// </summary>
    internal class Counting : PluginBase
    {
        public Counting(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var query = Parsing.VoucherQuery(ref expr);
            var subtotal = Parsing.Token(ref expr, false);
            Parsing.Eof(expr);

            SubtotalLevel level;
            switch (subtotal)
            {
                case "`d":
                    level = SubtotalLevel.Day;
                    break;
                case "`m":
                    level = SubtotalLevel.Month;
                    break;
                case "`y":
                    level = SubtotalLevel.Year;
                    break;
                case "`w":
                    level = SubtotalLevel.Week;
                    break;
                default:
                    throw new InvalidOperationException("分类汇总层次错误");
            }

            var par = new SubtotalPar(level);

            var res = Accountant.CountVouchersGrouped(query, level);

            return new PlainText(new RichSubtotalPre().PresentSubtotal(res, par));
        }

        private sealed class SubtotalPar : ISubtotal
        {
            private readonly SubtotalLevel m_Level;
            public SubtotalPar(SubtotalLevel level) => m_Level = level;
            public GatheringType GatherType => GatheringType.Count;

            public IReadOnlyList<SubtotalLevel> Levels => new[] { m_Level };

            public AggregationType AggrType => AggregationType.None;

            public IDateRange EveryDayRange => null;

            public string EquivalentCurrency => null;

            public DateTime? EquivalentDate => null;
        }
    }
}

/* Copyright (C) 2020-2025 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing;

internal partial class SubtotalParser
{
    public partial class SubtotalContext : IClientDependable, ISubtotal
    {
        public Client Client { private get; set; }

        /// <inheritdoc />
        public GatheringType GatherType
            => Mark.Text switch
                {
                    "``" => GatheringType.Sum,
                    "`" => GatheringType.Sum,
                    "!" => GatheringType.Count,
                    "!!" => GatheringType.VoucherCount,
                    _ => throw new MemberAccessException("表达式错误"),
                };

        /// <inheritdoc />
        public IReadOnlyList<SubtotalLevel> Levels
        {
            get
            {
                var z = Mark.Text == "`" ? SubtotalLevel.NonZero : SubtotalLevel.None;
                if (subtotalFields() == null && subtotalAggr() == null)
                    if (subtotalEqui() == null)
                        return new[]
                            {
                                z | SubtotalLevel.Currency,
                                z | SubtotalLevel.Title,
                                z | SubtotalLevel.SubTitle,
                                z | SubtotalLevel.User,
                                z | SubtotalLevel.Content,
                            };
                    else
                        return new[]
                            {
                                z | SubtotalLevel.Title,
                                z | SubtotalLevel.SubTitle,
                                z | SubtotalLevel.User,
                                z | SubtotalLevel.Content,
                            };

                if (subtotalFields() == null || subtotalFields().SubtotalNoField() != null)
                    return Array.Empty<SubtotalLevel>();

                return subtotalFields().subtotalField()
                    .Select(
                        f =>
                            {
                                var ch = f.SubtotalField().GetText()[0];
                                var zz = z | (f.SubtotalFieldZ() == null
                                    ? SubtotalLevel.None
                                    : SubtotalLevel.NonZero);
                                return ch switch
                                    {
                                        'R' => zz | SubtotalLevel.VoucherRemark,
                                        'K' => zz | SubtotalLevel.TitleKind,
                                        't' => zz | SubtotalLevel.Title,
                                        's' => zz | SubtotalLevel.SubTitle,
                                        'c' => zz | SubtotalLevel.Content,
                                        'r' => zz | SubtotalLevel.Remark,
                                        'C' => zz | SubtotalLevel.Currency,
                                        'U' => zz | SubtotalLevel.User,
                                        'd' => zz | SubtotalLevel.Day,
                                        'w' => zz | SubtotalLevel.Week,
                                        'm' => zz | SubtotalLevel.Month,
                                        'q' => zz | SubtotalLevel.Quarter,
                                        'y' => zz | SubtotalLevel.Year,
                                        'V' => zz | SubtotalLevel.Value,
                                        _ => throw new MemberAccessException("表达式错误"),
                                    };
                            })
                    .ToList();
            }
        }

        /// <inheritdoc />
        public AggregationType AggrType
            => subtotalAggr() switch
                {
                    null => AggregationType.None,
                    var x when x.AllDate() == null && x.rangeCore() == null => AggregationType.ChangedDay,
                    _ => AggregationType.EveryDay,
                };

        /// <inheritdoc />
        public SubtotalLevel AggrInterval
            => subtotalAggr() switch
                {
                    null => SubtotalLevel.None,
                    var x => x.AggrMark().GetText() switch
                        {
                            "D" => SubtotalLevel.Day,
                            "W" => SubtotalLevel.Week,
                            "M" => SubtotalLevel.Month,
                            "Q" => SubtotalLevel.Quarter,
                            "Y" => SubtotalLevel.Year,
                            _ => throw new MemberAccessException("表达式错误"),
                        },
                };

        /// <inheritdoc />
        public DateFilter EveryDayRange
            => subtotalAggr() switch
                {
                    var x when x.AllDate() != null => DateFilter.Unconstrained,
                    var x => x.rangeCore().Assign(Client).Range,
                };

        /// <inheritdoc />
        public string EquivalentCurrency
            => subtotalEqui() switch
                {
                    null => null,
                    var x => x.VoucherCurrency()?.GetText().ParseCurrency() ?? BaseCurrency.Now,
                };

        /// <inheritdoc />
        public DateTime? EquivalentDate
            => subtotalEqui() switch
                {
                    null => null,
                    var x => x.rangeDay().Assign(Client)?.AsDate() ?? Client.Today,
                };
    }
}

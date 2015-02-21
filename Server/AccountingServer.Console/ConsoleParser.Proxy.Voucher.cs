using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class VoucherQueryContext : IVoucherQueryAtom
        {
            public bool ForAll { get { return Op != null && Op.Text == "A"; } }

            public Voucher VoucherFilter
            {
                get
                {
                    var vfilter = new Voucher();
                    if (DollarQuotedString() != null)
                    {
                        var s = DollarQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        vfilter.ID = s.Replace("$$", "$");
                    }
                    if (PercentQuotedString() != null)
                    {
                        var s = PercentQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        vfilter.Remark = s.Replace("%%", "%");
                    }
                    if (VoucherType() != null)
                    {
                        var s = VoucherType().GetText();
                        VoucherType type;
                        Enum.TryParse(s, out type);
                        vfilter.Type = type;
                    }
                    return vfilter;
                }
            }

            public DateFilter Range { get { return range().Range; } }
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VouchersContext : IQueryAry<IVoucherQueryAtom>
        {
            public OperatorType Operator { get { return OperatorType.None; } }

            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchersB();
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { throw new NotImplementedException(); } }
        }

        public partial class VouchersBContext : IQueryAry<IVoucherQueryAtom>
        {
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;
                    if (vouchersB().Count == 1)
                    {
                        if (Op.Text == "+")
                            return OperatorType.Identity;
                        if (Op.Text == "-")
                            return OperatorType.Complement;
                        throw new InvalidOperationException();
                    }
                    if (Op.Text == "+")
                        return OperatorType.Union;
                    if (Op.Text == "-")
                        return OperatorType.Substract;
                    if (Op.Text == "*")
                        return OperatorType.Intersect;
                    throw new InvalidOperationException();
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter1
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    //if (Op == null)
                    //    return vouchers(0);
                    return vouchersB(0);
                }
            }

            public IQueryCompunded<IVoucherQueryAtom> Filter2 { get { return vouchersB(1); } }
        }

        public partial class SubtotalContext : ISubtotal
        {
            public bool NonZero { get { return SubtotalMark.Text.Length < 2; } }

            public IList<SubtotalLevel> Levels
            {
                get
                {
                    if (SubtotalFields == null)
                        return new[] { SubtotalLevel.Title, SubtotalLevel.SubTitle, SubtotalLevel.Content };
                    return SubtotalFields.Text.Select(
                                                      ch =>
                                                      {
                                                          switch (ch)
                                                          {
                                                              case 't':
                                                                  return SubtotalLevel.Title;
                                                              case 's':
                                                                  return SubtotalLevel.SubTitle;
                                                              case 'c':
                                                                  return SubtotalLevel.Content;
                                                              case 'r':
                                                                  return SubtotalLevel.Remark;
                                                              case 'd':
                                                                  return SubtotalLevel.Day;
                                                              case 'w':
                                                                  return SubtotalLevel.Week;
                                                              case 'm':
                                                                  return SubtotalLevel.Month;
                                                              case 'f':
                                                                  return SubtotalLevel.FinancialMonth;
                                                              case 'b':
                                                                  return SubtotalLevel.BillingMonth;
                                                              case 'y':
                                                                  return SubtotalLevel.Year;
                                                          }
                                                          throw new InvalidOperationException();
                                                      }).ToList();
                }
            }

            public bool AggrEnabled
            {
                get { return Aggregation != null; }
            }

            public IDateRange AggrRange
            {
                get
                {
                    if (rangeCore() != null)
                        return rangeCore();
                    if (Ranged == null)
                        return null;
                    return DateFilter.Unconstrained;
                }
            }
        }

        public partial class EmitContext : IEmit
        {
            public IQueryCompunded<IDetailQueryAtom> DetailFilter { get { return details(); } }
        }

        public partial class VoucherDetailQueryContext : IVoucherDetailQuery
        {
            public IQueryCompunded<IVoucherQueryAtom> VoucherQuery
            {
                get
                {
                    if (voucherQuery() != null)
                        return voucherQuery();
                    return vouchers();
                }
            }

            public IEmit DetailEmitFilter { get { return emit(); } }
        }


        public partial class GroupedQueryContext : IGroupedQuery
        {
            public IVoucherDetailQuery VoucherEmitQuery { get { return voucherDetailQuery(); } }
            public ISubtotal Subtotal { get { return subtotal(); } }
        }
    }
}

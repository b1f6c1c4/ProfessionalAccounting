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
                    if (VoucherID() != null)
                    {
                        var s = VoucherID().GetText();
                        s = s.Substring(1, s.Length - 2);
                        vfilter.ID = s.Replace("$$", "$");
                    }
                    if (VoucherRemark() != null)
                    {
                        var s = VoucherRemark().GetText();
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
            public IDetailQueryCompounded DetailFilter { get { return details(); } }
        }

        public partial class VoucherAtomContext : IVoucherQueryCompounded { }

        public partial class VouchersXContext : IVoucherQueryBinary
        {
            public BinaryOperatorType Operator { get { return BinaryOperatorType.Interect; } }
            public IVoucherQueryCompounded Filter1 { get { return voucherAtom(0); } }
            public IVoucherQueryCompounded Filter2 { get { return voucherAtom(1); } }
        }

        public partial class VouchersContext : IVoucherQueryBinary
        {
            public BinaryOperatorType Operator
            {
                get
                {
                    if (Op.Text == "+")
                        return BinaryOperatorType.Union;
                    if (Op.Text == "-")
                        return BinaryOperatorType.Substract;
                    throw new InvalidOperationException();
                }
            }

            public IVoucherQueryCompounded Filter1 { get { return vouchersX(0); } }
            public IVoucherQueryCompounded Filter2 { get { return vouchersX(1); } }
        }

        public partial class VoucherUnaryContext : IVoucherQueryUnary
        {
            public UnaryOperatorType Operator
            {
                get
                {
                    if (Op.Text == "-")
                        return UnaryOperatorType.Complement;
                    if (Op.Text == "+")
                        return UnaryOperatorType.Identity;
                    throw new InvalidOperationException();
                }
            }

            public IVoucherQueryCompounded Filter1 { get { return voucherQuery(); } }
        }

        public partial class SubtotalContext : ISubtotal
        {
            public bool NonZero { get { return SubtotalMark.Text.Length < 2; } }

            public IList<SubtotalLevel> Levels
            {
                get
                {
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

            public AggregationType AggrType
            {
                get
                {
                    switch (AggregationMethod.Text)
                    {
                        case "X":
                            return AggregationType.ExtendedEveryDay;
                        case "x":
                            return AggregationType.EveryDay;
                        case "D":
                            return AggregationType.ChangedDay;
                    }
                    throw new InvalidOperationException();
                }
            }
        }

        public partial class EmitContext : IEmit
        {
            public IDetailQueryCompounded DetailFilter { get { return details(); } }
        }

        public partial class VoucherDetailQueryContext : IVoucherDetailQuery
        {
            public IVoucherQueryCompounded VoucherQuery
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

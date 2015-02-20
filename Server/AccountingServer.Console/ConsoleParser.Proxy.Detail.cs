using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class ConsoleParser
    {
        public partial class DetailQueryContext : IDetailQueryAtom
        {
            public VoucherDetail Filter
            {
                get
                {
                    var filter = new VoucherDetail();
                    if (DetailTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitle().GetText().TrimStart('T'));
                        filter.Title = t / 100;
                        filter.SubTitle = t % 100;
                    }
                    else if (DetailTitleSubTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitleSubTitle().GetText().TrimStart('T'));
                        filter.Title = t / 100;
                        filter.SubTitle = t % 100;
                    }
                    if (DoubleQuotedString() != null)
                    {
                        var s = DoubleQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        filter.Content = s.Replace("\"\"", "\"");
                    }
                    if (SingleQuotedString() != null)
                    {
                        var s = SingleQuotedString().GetText();
                        s = s.Substring(1, s.Length - 2);
                        filter.Remark = s.Replace("''", "'");
                    }
                    return filter;
                }
            }

            public int Dir
            {
                get
                {
                    if (Direction.Text == "d")
                        return 1;
                    if (Direction.Text == "c")
                        return -1;
                    return 0;
                }
            }
        }

        public partial class DetailAtomContext : IDetailQueryCompounded { }

        public partial class DetailsXContext : IDetailQueryBinary
        {
            public BinaryOperatorType Operator { get { return BinaryOperatorType.Interect; } }
            public IDetailQueryCompounded Filter1 { get { return detailAtom(0); } }
            public IDetailQueryCompounded Filter2 { get { return detailAtom(1); } }
        }

        public partial class DetailsContext : IDetailQueryBinary
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

            public IDetailQueryCompounded Filter1 { get { return detailsX(0); } }
            public IDetailQueryCompounded Filter2 { get { return detailsX(1); } }
        }

        public partial class DetailUnaryContext : IDetailQueryUnary
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

            public IDetailQueryCompounded Filter1 { get { return detailQuery(); } }
        }

    }
}

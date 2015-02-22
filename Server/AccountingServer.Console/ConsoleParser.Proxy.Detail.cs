using System;
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
                    var filter = new VoucherDetail
                                     {
                                         Content = SingleQuotedString().Dequotation(),
                                         Remark = DoubleQuotedString().Dequotation()
                                     };
                    if (DetailTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitle().GetText().TrimStart('T'));
                        filter.Title = t;
                    }
                    else if (DetailTitleSubTitle() != null)
                    {
                        var t = Int32.Parse(DetailTitleSubTitle().GetText().TrimStart('T'));
                        filter.Title = t / 100;
                        filter.SubTitle = t % 100;
                    }
                    return filter;
                }
            }

            public int Dir
            {
                get
                {
                    if (Direction == null)
                        return 0;
                    if (Direction.Text == ">")
                        return 1;
                    if (Direction.Text == "<")
                        return -1;
                    throw new InvalidOperationException();
                }
            }
        }

        public partial class DetailsContext : IQueryAry<IDetailQueryAtom>
        {
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;
                    if (details().Count == 1)
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

            public IQueryCompunded<IDetailQueryAtom> Filter1
            {
                get
                {
                    if (detailQuery() != null)
                        return detailQuery();
                    return details(0);
                }
            }

            public IQueryCompunded<IDetailQueryAtom> Filter2 { get { return details(1); } }
        }
    }
}

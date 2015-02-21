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

        public partial class DetailsContext : IDetailQueryAry
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
                        return OperatorType.Interect;
                    throw new InvalidOperationException();
                }
            }

            public IDetailQueryCompounded Filter1
            {
                get
                {
                    if (detailQuery() != null)
                        return detailQuery();
                    //if (Op == null)
                    //    return details(0);
                    return details(0);
                }
            }

            public IDetailQueryCompounded Filter2 { get { return details(1); } }
        }
    }
}

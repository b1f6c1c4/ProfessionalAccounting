using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement
{
    /// <summary>
    ///     CSV解析
    /// </summary>
    internal class CsvParser
    {
        private readonly int m_Dir;
        public List<BankItem> Items;

        public CsvParser(bool reversed)
        {
            m_Dir = reversed ? -1 : +1;
        }

        private static string Next(ref string expr)
        {
            if (expr[0] == '"')
            {
                var tmp = ParsingF.Quoted(ref expr);
                if (string.IsNullOrWhiteSpace(expr))
                {
                    expr = null;
                    return tmp;
                }

                if (expr[0] == ',')
                {
                    expr = expr.Substring(1);
                    return tmp;
                }

                throw new ApplicationException("Csv格式错误");
            }

            var id = expr.IndexOf(',');
            if (id < 0)
            {
                var tmp = expr;
                expr = null;
                return tmp;
            }

            var f = expr.Substring(0, id);
            expr = expr.Substring(id + 1);
            return f;
        }

        public void Parse(string expr)
        {
            var headerB = ParsingF.Line(ref expr);
            var header = headerB;

            var dateId = -1;
            var fundId = -1;

            var dateReg = new Regex(@"^trans(?:\.|action)\s+date$", RegexOptions.IgnoreCase);
            var fundReg = new Regex(@"^(?:amount|fund|value)$", RegexOptions.IgnoreCase);
            for (var i = 0; !string.IsNullOrWhiteSpace(header); i++)
            {
                var f = Next(ref header);
                if (dateReg.IsMatch(f))
                    dateId = i;
                else if (fundReg.IsMatch(f))
                    fundId = i;
            }

            if (dateId < 0)
            {
                header = headerB;
                var dateReg2 = new Regex(@"^date$", RegexOptions.IgnoreCase);
                for (var i = 0; !string.IsNullOrWhiteSpace(header); i++)
                {
                    var f = Next(ref header);
                    if (i == fundId)
                        continue;
                    if (dateReg2.IsMatch(f))
                        dateId = i;
                }

                if (dateId < 0)
                    throw new ApplicationException("找不到日期字段");
            }

            if (fundId < 0)
                throw new ApplicationException("找不到金额字段");

            Items = new List<BankItem>();
            while (!string.IsNullOrWhiteSpace(expr))
            {
                var l = ParsingF.Line(ref expr);
                var item = new BankItem { Raw = l };
                for (var i = 0; !string.IsNullOrWhiteSpace(l); i++)
                {
                    var f = Next(ref l);
                    if (i == dateId)
                        item.Date = ClientDateTime.Parse(f);
                    else if (i == fundId)
                        item.Fund = m_Dir * Convert.ToDouble(f);
                }

                Items.Add(item);
            }
        }
    }

    internal class BankItem
    {
        public DateTime Date;
        public double Fund;
        public string Raw;
    }
}

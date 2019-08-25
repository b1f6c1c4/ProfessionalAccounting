using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static AccountingServer.BLL.Parsing.Facade;
using static AccountingServer.BLL.Parsing.FacadeF;

namespace AccountingServer.Shell.Plugins.Statement
{
    /// <summary>
    ///     CSV解析
    /// </summary>
    internal class CsvParser
    {
        public List<BankItem> Items;

        public void Parse(string expr)
        {
            var header = ParsingF.Line(ref expr).Split(',');

            var dateId = -1;
            var fundId = -1;

            var dateReg = new Regex(@"^transaction\s+date$", RegexOptions.IgnoreCase);
            var fundReg = new Regex(@"^(?:amount|fund|value)$", RegexOptions.IgnoreCase);
            for (var i = 0; i < sp.Length; i++)
            {
                if (header[i].IsMatch(dateReg))
                    dateId = i;
                else if (header[i].IsMatch(fundReg))
                    fundId = i;
            }

            if (dateId < 0)
                throw new ApplicationException("找不到日期字段");
            if (fundId < 0)
                throw new ApplicationException("找不到金额字段");

            while (expr != null)
            {
                var l = ParsingF.Line(ref expr);
                var sp = l.Split(',');
                Items.Add(new BankItem
                    {
                        Date = ClientDateTime.Parse(sp[dateId]),
                        Fund = Convert.ToDouble(sp[fundId]),
                        Raw = l
                    });
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

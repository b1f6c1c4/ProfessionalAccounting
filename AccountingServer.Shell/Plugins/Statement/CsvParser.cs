using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AccountingServer.Entities;
using AccountingServer.Shell.Util;
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
            for (var i = 0; i < header.Length; i++)
            {
                if (dateReg.IsMatch(header[i]))
                    dateId = i;
                else if (fundReg.IsMatch(header[i]))
                    fundId = i;
            }

            if (dateId < 0)
                throw new ApplicationException("找不到日期字段");
            if (fundId < 0)
                throw new ApplicationException("找不到金额字段");

            Items = new List<BankItem>();
            while (!string.IsNullOrWhiteSpace(expr))
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

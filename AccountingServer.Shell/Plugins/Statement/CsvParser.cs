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
	///		CSV解析
	///	</summary>
	internal class CsvParser
	{
        public List<BankItem> Items;

		public void Parse(string expr)
		{
			Dictionary<string, int> ids;
		}
	}

    internal class BankItem
    {
        public DateTime Date;
        public double Fund;
        public string Raw;
    }
}

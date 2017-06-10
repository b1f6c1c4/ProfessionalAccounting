using System;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Carry
{
    /// <summary>
    ///     汇率
    /// </summary>
    internal class ExchangeShell : IShellComponent
    {
        public IQueryResult Execute(string expr)
        {
            expr = expr.Rest();
            var rev = true;
            var val = Parsing.Double(ref expr);
            var curr = Parsing.Token(ref expr).ToUpperInvariant();
            if (!val.HasValue)
            {
                rev = false;
                val = Parsing.DoubleF(ref expr);
            }
            var date = Parsing.UniqueTime(ref expr) ?? DateTime.Today.CastUtc();
            Parsing.Eof(expr);
            var res = rev ? ExchangeFactory.Instance.To(date, curr) : ExchangeFactory.Instance.From(date, curr);

            return new UnEditableText((res * val.Value).ToString("R"));
        }

        public bool IsExecutable(string expr) => expr.Initital() == "?e";
    }
}

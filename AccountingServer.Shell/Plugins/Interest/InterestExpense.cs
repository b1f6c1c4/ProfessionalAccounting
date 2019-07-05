using AccountingServer.BLL;

namespace AccountingServer.Shell.Plugins.Interest
{
    /// <summary>
    ///     自动计算利息支出和还款
    /// </summary>
    internal class InterestExpense : InterestBase
    {
        public InterestExpense(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        protected override string MajorFilter() => "T2202+T2203+T224100";

        /// <inheritdoc />
        protected override int Dir() => -1;

        /// <inheritdoc />
        protected override int MinorSubTitle() => 01;
    }
}

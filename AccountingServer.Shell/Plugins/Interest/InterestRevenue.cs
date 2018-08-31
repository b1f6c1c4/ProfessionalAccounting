using AccountingServer.BLL;

namespace AccountingServer.Shell.Plugins.Interest
{
    /// <summary>
    ///     自动计算利息收入和还款
    /// </summary>
    internal class InterestRevenue : InterestBase
    {
        public InterestRevenue(Accountant accountant) : base(accountant) { }

        /// <inheritdoc />
        protected override int MajorTitle() => 1221;

        /// <inheritdoc />
        protected override int Dir() => +1;

        /// <inheritdoc />
        protected override int MinorSubTitle() => 02;
    }
}

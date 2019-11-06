using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal static class NullDateRangeHelper
    {
        public static DateFilter TheRange(this IDateRange range) => range?.Range ?? DateFilter.Unconstrained;
    }
}

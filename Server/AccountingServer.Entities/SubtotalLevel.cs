using System;

namespace AccountingServer.Entities
{
    [Flags]
    public enum SubtotalLevel
    {
        None = 0x0,
        Title = 0x1,
        SubTitle = 0x2,
        Content = 0x4,
        Remark = 0x8,
        Day = 0x10,
        Week = 0x30,
        Month = 0x70,
        FinancialMonth = 0xB0,
        BillingMonth = 0xF0,
        Year = 0x110
    }
}

using System;

namespace AccountingServer.Entities
{
    public class DbTitle
    {
        public decimal? ID { get; set; }
        public string Name { get; set; }
        public decimal? H_ID { get; set; }
        public int? TLevel { get; set; }

        public decimal? Balance { get; set; }
        public decimal? ABalance { get; set; }
    }

    public class DbItem
    {
        public int? ID { get; set; }
        public DateTime? DT { get; set; }
        public string Remark { get; set; }
    }

    public class DbDetail
    {
        public int? Item { get; set; }
        public decimal? Title { get; set; }
        public decimal? Fund { get; set; }
        public string Remark { get; set; }
    }

    public class DbFixedAsset
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public DateTime? DT { get; set; }
        public decimal? Value { get; set; }
        public decimal? XValue { get; set; }
        public int? DepreciableLife { get; set; }
        public decimal? Salvge { get; set; }
        public decimal? Title { get; set; }

        public decimal? DepreciatedValue1 { get; set; }
        public decimal? DepreciatedValue2 { get; set; }
    }

    public class DbShortcut
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public decimal? Balance { get; set; }
    }

    public class DailyBalance
    {
        public DateTime? DT { get; set; }
        public decimal Title { get; set; }
        public string Remark { get; set; }
        public decimal Balance { get; set; }
    }
}

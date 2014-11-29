using System;

namespace AccountingServer.Entities
{
    public enum VoucherType
    {
        Ordinal,
        Carry,
        Amortization,
        Depreciation,
        Devalue,
        AnnualCarry,
        Uncertain
    }

    public interface IObjectID
    {
        string ToString();
        IObjectID Parse(string str);
    }

    public class Voucher
    {
        public IObjectID ID { get; set; }
        public DateTime? Date { get; set; }
        public string Remark { get; set; }
        public VoucherDetail[] Details { get; set; }
        public VoucherType? Type { get; set; }
    }

    public class VoucherDetail
    {
        public IObjectID Item { get; set; }
        public int? Title { get; set; }
        public int? SubTitle { get; set; }
        public string Content { get; set; }
        public double? Fund { get; set; }
        public string Remark { get; set; }
    }

    //public interface IAssetItem
    //{
        
    //}

    //public struct Depreciate : IAssetItem
    //{
    //    public DateTime? Date { get; set; }
    //    public double? Fund { get; set; }
    //}
    //public struct Devalue : IAssetItem
    //{
    //    public DateTime? Date { get; set; }
    //    public double? Fund { get; set; }
    //}

    //public class DbAsset
    //{
    //    public Guid ID { get; set; }
    //    public string Name { get; set; }
    //    public DateTime? Date { get; set; }
    //    public double? Value { get; set; }
    //    public int? Life { get; set; }
    //    public double? Salvge { get; set; }
    //    public int? Title { get; set; }
    //    public IAssetItem[] Schedule { get; set; }
    //}

    public class Balance
    {
        public DateTime? Date { get; set; }
        public int? Title { get; set; }
        public int? SubTitle { get; set; }
        public string Content { get; set; }
        public double Fund { get; set; }
    }
}

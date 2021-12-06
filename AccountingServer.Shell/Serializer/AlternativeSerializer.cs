using System;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Serializer
{
    internal class AlternativeSerializer : IEntitySerializer
    {
        /// <summary>
        ///     主要表示器
        /// </summary>
        private readonly IEntitySerializer m_Primary;

        /// <summary>
        ///     次要表示器
        /// </summary>
        private readonly IEntitySerializer m_Secondary;

        private AlternativeSerializer(IEntitySerializer primary, IEntitySerializer secondary)
        {
            m_Primary = primary;
            m_Secondary = secondary;
        }

        public string PresentVoucher(Voucher voucher) => Run(s => s.PresentVoucher(voucher));
        public Voucher ParseVoucher(string str) => Run(s => s.ParseVoucher(str));
        public string PresentVoucherDetail(VoucherDetail detail) => Run(s => s.PresentVoucherDetail(detail));
        public string PresentVoucherDetail(VoucherDetailR detail) => Run(s => s.PresentVoucherDetail(detail));
        public VoucherDetail ParseVoucherDetail(string str) => Run(s => s.ParseVoucherDetail(str));
        public string PresentAsset(Asset asset) => Run(s => s.PresentAsset(asset));
        public Asset ParseAsset(string str) => Run(s => s.ParseAsset(str));
        public string PresentAmort(Amortization amort) => Run(s => s.PresentAmort(amort));
        public Amortization ParseAmort(string str) => Run(s => s.ParseAmort(str));

        public static IEntitySerializer Compose(params IEntitySerializer[] serializers)
            => serializers.Aggregate((p, s) => new AlternativeSerializer(p, s));

        private TOut Run<TOut>(Func<IEntitySerializer, TOut> func)
        {
            try
            {
                return func(m_Primary);
            }
            catch (NotImplementedException)
            {
                return func(m_Secondary);
            }
        }
    }
}

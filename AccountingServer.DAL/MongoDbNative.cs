using System;
using System.Collections.Generic;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb���ݿ��ѯ
    /// </summary>
    internal static class MongoDbNative
    {
        /// <summary>
        ///     ����Ų�ѯ<c>ObjectId</c>
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static FilterDefinition<T> GetNQuery<T>(string id) => Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));

        /// <summary>
        ///     ����Ų�ѯ<c>Guid</c>
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static FilterDefinition<T> GetNQuery<T>(Guid? id) =>
            Builders<T>.Filter.Eq("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);

        /// <summary>
        ///     ����ƾ֤��������Native��ʾ
        /// </summary>
        /// <param name="vfilter">����ƾ֤������</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<Voucher> GetNativeFilter(Voucher vfilter)
        {
            if (vfilter == null)
                return Builders<Voucher>.Filter.Empty;

            var lst = new List<FilterDefinition<Voucher>>();

            if (vfilter.ID != null)
                lst.Add(Builders<Voucher>.Filter.Eq("_id", vfilter.ID));
            if (vfilter.Date != null)
                lst.Add(Builders<Voucher>.Filter.Eq("date", vfilter.Date));
            if (vfilter.Type != null)
                switch (vfilter.Type)
                {
                    case VoucherType.Ordinary:
                        lst.Add(Builders<Voucher>.Filter.Exists("special", false));
                        break;
                    case VoucherType.General:
                        lst.Add(Builders<Voucher>.Filter.Nin("special", new[] { "acarry", "carry" }));
                        break;
                    case VoucherType.Amortization:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "amorz"));
                        break;
                    case VoucherType.AnnualCarry:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "acarry"));
                        break;
                    case VoucherType.Carry:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "carry"));
                        break;
                    case VoucherType.Depreciation:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "dep"));
                        break;
                    case VoucherType.Devalue:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "dev"));
                        break;
                    case VoucherType.Uncertain:
                        lst.Add(Builders<Voucher>.Filter.Eq("special", "unc"));
                        break;
                }
            if (vfilter.Remark != null)
                lst.Add(
                        vfilter.Remark == string.Empty
                            ? Builders<Voucher>.Filter.Exists("remark", false)
                            : Builders<Voucher>.Filter.Eq("remark", vfilter.Remark));
            if (vfilter.Currency == Voucher.BaseCurrency)
                lst.Add(Builders<Voucher>.Filter.Exists("currency", false));
            else if (vfilter.Currency != null)
                lst.Add(Builders<Voucher>.Filter.Eq("currency", vfilter.Currency));

            return Builders<Voucher>.Filter.And(lst);
        }

        /// <summary>
        ///     ���ڹ�������Native��ʾ
        /// </summary>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<Voucher> GetNativeFilter(DateFilter? rng)
        {
            if (rng == null)
                return Builders<Voucher>.Filter.Empty;

            if (rng.Value.NullOnly)
                return Builders<Voucher>.Filter.Eq<DateTime?>("date", null);

            var lst = new List<FilterDefinition<Voucher>>();

            if (rng.Value.StartDate.HasValue)
                lst.Add(Builders<Voucher>.Filter.Gte("date", rng.Value.StartDate));
            if (rng.Value.EndDate.HasValue)
                lst.Add(Builders<Voucher>.Filter.Lte("date", rng.Value.EndDate));

            var gather = Builders<Voucher>.Filter.And(lst);
            return rng.Value.Nullable
                       ? Builders<Voucher>.Filter.Eq<DateTime?>("date", null) | gather
                       : gather;
        }

        /// <summary>
        ///     ԭ��ϸĿ����ʽ��Native��ʾ
        /// </summary>
        /// <param name="f">ϸĿ����ʽ</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<VoucherDetail> GetNativeFilter(IDetailQueryAtom f)
        {
            if (f == null)
                return Builders<VoucherDetail>.Filter.Empty;

            var lst = new List<FilterDefinition<VoucherDetail>>();
            if (f.Dir != 0)
                lst.Add(
                        f.Dir > 0
                            ? Builders<VoucherDetail>.Filter.Gt("fund", 0)
                            : Builders<VoucherDetail>.Filter.Lt("fund", 0));
            if (f.Filter != null)
            {
                if (f.Filter.Title != null)
                    lst.Add(Builders<VoucherDetail>.Filter.Eq("title", f.Filter.Title.Value));
                if (f.Filter.SubTitle != null)
                    lst.Add(
                            f.Filter.SubTitle == 00
                                ? Builders<VoucherDetail>.Filter.Exists("subtitle", false)
                                : Builders<VoucherDetail>.Filter.Eq("subtitle", f.Filter.SubTitle.Value));
                if (f.Filter.Content != null)
                    lst.Add(
                            f.Filter.Content == string.Empty
                                ? Builders<VoucherDetail>.Filter.Exists("content", false)
                                : Builders<VoucherDetail>.Filter.Eq("content", f.Filter.Content));
                if (f.Filter.Remark != null)
                    lst.Add(
                            f.Filter.Remark == string.Empty
                                ? Builders<VoucherDetail>.Filter.Exists("remark", false)
                                : Builders<VoucherDetail>.Filter.Eq("remark", f.Filter.Remark));
                if (f.Filter.Fund != null)
                    lst.Add(
                            Builders<VoucherDetail>.Filter.Gte("fund", f.Filter.Fund.Value - 1e-8) &
                            Builders<VoucherDetail>.Filter.Lte("fund", f.Filter.Fund.Value + 1e-8));
            }
            return Builders<VoucherDetail>.Filter.And(lst);
        }

        /// <summary>
        ///     ����ƾ֤����ʽ��Native��ʾ
        /// </summary>
        /// <param name="query">����ƾ֤����ʽ</param>
        /// <returns>Native��ʾ</returns>
        public static FilterDefinition<Voucher> GetNativeFilter(IQueryCompunded<IVoucherQueryAtom> query) =>
            GetNativeFilter(query, GetNativeFilter);

        /// <summary>
        ///     ����ƾ֤����ʽ�Ĳ�ѯ
        /// </summary>
        /// <param name="query">����ƾ֤����ʽ</param>
        /// <returns>��ѯ</returns>
        public static FilterDefinition<Voucher> GetNQuery(IQueryCompunded<IVoucherQueryAtom> query) =>
            GetNativeFilter(query);

        /// <summary>
        ///     ԭ�Ӽ���ƾ֤����ʽ��Native��ʾ
        /// </summary>
        /// <param name="f">����ƾ֤����ʽ</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<Voucher> GetNativeFilter(IVoucherQueryAtom f)
        {
            if (f == null)
                return Builders<Voucher>.Filter.Empty;

            var lst = new List<FilterDefinition<Voucher>> { GetNativeFilter(f.VoucherFilter), GetNativeFilter(f.Range) };
            var v = GetNativeFilter(f.DetailFilter, GetNativeFilter);
            if (f.ForAll)
                lst.Add(!Builders<Voucher>.Filter.ElemMatch("detail", !v));
            else
                lst.Add(Builders<Voucher>.Filter.ElemMatch("detail", v));

            return Builders<Voucher>.Filter.And(lst);
        }

        /// <summary>
        ///     ���ڼ���ʽ�Ĳ�ѯ
        /// </summary>
        /// <param name="query">���ڼ���ʽ</param>
        /// <returns>��ѯ</returns>
        public static FilterDefinition<T> GetNQuery<T>(IQueryCompunded<IDistributedQueryAtom> query)
            where T : IDistributed =>
                GetNativeFilter(query, GetNativeFilter<T>);

        /// <summary>
        ///     ԭ�ӷ��ڼ���ʽ��Native��ʾ
        /// </summary>
        /// <param name="f">���ڼ���ʽ</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<T> GetNativeFilter<T>(IDistributedQueryAtom f)
            where T : IDistributed
        {
            if (f?.Filter == null)
                return Builders<T>.Filter.Empty;

            var lst = new List<FilterDefinition<T>>();
            if (f.Filter.ID.HasValue)
                lst.Add(Builders<T>.Filter.Eq("_id", f.Filter.ID.Value));
            if (f.Filter.Name != null)
                lst.Add(
                        f.Filter.Name == string.Empty
                            ? Builders<T>.Filter.Exists("name", false)
                            : Builders<T>.Filter.Eq("name", f.Filter.Name));
            if (f.Filter.Remark != null)
                lst.Add(
                        f.Filter.Remark == string.Empty
                            ? Builders<T>.Filter.Exists("remark", false)
                            : Builders<T>.Filter.Eq("remark", f.Filter.Remark));

            return Builders<T>.Filter.And(lst);
        }

        /// <summary>
        ///     һ�����ʽ��Native��ʾ
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <param name="atomFilter">ԭ�Ӽ���ʽ��Native��ʾ</param>
        /// <returns>Native��ʾ</returns>
        private static FilterDefinition<T> GetNativeFilter<T, TAtom>(IQueryCompunded<TAtom> query,
                                                                     Func<TAtom, FilterDefinition<T>> atomFilter)
            where TAtom : class
        {
            if (query == null ||
                query is TAtom)
                return atomFilter((TAtom)query);

            var f = query as IQueryAry<TAtom>;
            if (f == null)
                throw new ArgumentException("����ʽ����δ֪", nameof(query));
            switch (f.Operator)
            {
                case OperatorType.None:
                case OperatorType.Identity:
                    return GetNativeFilter(f.Filter1, atomFilter);
                case OperatorType.Complement:
                    return !GetNativeFilter(f.Filter1, atomFilter);
                case OperatorType.Union:
                    return GetNativeFilter(f.Filter1, atomFilter) | GetNativeFilter(f.Filter2, atomFilter);
                case OperatorType.Intersect:
                    return GetNativeFilter(f.Filter1, atomFilter) & GetNativeFilter(f.Filter2, atomFilter);
                case OperatorType.Substract:
                    return GetNativeFilter(f.Filter1, atomFilter) & !GetNativeFilter(f.Filter2, atomFilter);
                default:
                    throw new ArgumentException("��������δ֪", nameof(query));
            }
        }
    }
}

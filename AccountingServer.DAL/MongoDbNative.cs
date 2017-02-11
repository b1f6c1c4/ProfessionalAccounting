using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.DAL.Serializer;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据库查询
    /// </summary>
    internal static class MongoDbNative
    {
        private static FilterDefinition<T> And<T>(IReadOnlyCollection<FilterDefinition<T>> lst) =>
            lst.Count == 0
                ? Builders<T>.Filter.Empty
                : (lst.Count == 1 ? lst.First() : Builders<T>.Filter.And(lst));

        /// <summary>
        ///     按编号查询<c>ObjectId</c>
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static FilterDefinition<T> GetNQuery<T>(string id) => Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));

        /// <summary>
        ///     按编号查询<c>Guid</c>
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static FilterDefinition<T> GetNQuery<T>(Guid? id) =>
            Builders<T>.Filter.Eq("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);

        /// <summary>
        ///     记账凭证过滤器的Native表示
        /// </summary>
        /// <param name="vfilter">记账凭证过滤器</param>
        /// <returns>Native表示</returns>
        private static FilterDefinition<Voucher> GetNativeFilter(Voucher vfilter)
        {
            if (vfilter == null)
                return Builders<Voucher>.Filter.Empty;

            var lst = new List<FilterDefinition<Voucher>>();

            if (vfilter.ID != null)
                lst.Add(Builders<Voucher>.Filter.Eq("_id", new ObjectId(vfilter.ID)));
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

            return And(lst);
        }

        /// <summary>
        ///     日期过滤器的Native表示
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Native表示</returns>
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

            var gather = And(lst);
            return rng.Value.Nullable
                ? Builders<Voucher>.Filter.Eq<DateTime?>("date", null) | gather
                : gather;
        }

        /// <summary>
        ///     原子细目检索式的Native表示
        /// </summary>
        /// <param name="f">细目检索式</param>
        /// <returns>Native表示</returns>
        private static FilterDefinition<VoucherDetail> GetNativeFilter(IDetailQueryAtom f)
        {
            if (f == null)
                return Builders<VoucherDetail>.Filter.Empty;

            var lst = new List<FilterDefinition<VoucherDetail>>();
            if (f.Dir != 0)
                lst.Add(
                    f.Dir > 0
                        ? Builders<VoucherDetail>.Filter.Gt("fund", -VoucherDetail.Tolerance)
                        : Builders<VoucherDetail>.Filter.Lt("fund", +VoucherDetail.Tolerance));
            if (f.Filter?.Title != null)
                lst.Add(Builders<VoucherDetail>.Filter.Eq("title", f.Filter.Title.Value));
            if (f.Filter?.SubTitle != null)
                lst.Add(
                    f.Filter.SubTitle == 00
                        ? Builders<VoucherDetail>.Filter.Exists("subtitle", false)
                        : Builders<VoucherDetail>.Filter.Eq("subtitle", f.Filter.SubTitle.Value));
            if (f.Filter?.Content != null)
                lst.Add(
                    f.Filter.Content == string.Empty
                        ? Builders<VoucherDetail>.Filter.Exists("content", false)
                        : Builders<VoucherDetail>.Filter.Eq("content", f.Filter.Content));
            if (f.Filter?.Remark != null)
                lst.Add(
                    f.Filter.Remark == string.Empty
                        ? Builders<VoucherDetail>.Filter.Exists("remark", false)
                        : Builders<VoucherDetail>.Filter.Eq("remark", f.Filter.Remark));
            if (f.Filter?.Fund != null)
                lst.Add(
                    Builders<VoucherDetail>.Filter.Gte("fund", f.Filter.Fund.Value - VoucherDetail.Tolerance) &
                    Builders<VoucherDetail>.Filter.Lte("fund", f.Filter.Fund.Value + VoucherDetail.Tolerance));
            return And(lst);
        }

        /// <summary>
        ///     记账凭证检索式的Native表示
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>Native表示</returns>
        public static FilterDefinition<Voucher> GetNativeFilter(IQueryCompunded<IVoucherQueryAtom> query) =>
            GetNativeFilter(query, GetNativeFilter);

        /// <summary>
        ///     记账凭证检索式的查询
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <returns>查询</returns>
        public static FilterDefinition<Voucher> GetNQuery(IQueryCompunded<IVoucherQueryAtom> query) =>
            GetNativeFilter(query);

        /// <summary>
        ///     原子记账凭证检索式的Native表示
        /// </summary>
        /// <param name="f">记账凭证检索式</param>
        /// <returns>Native表示</returns>
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

            return And(lst);
        }

        /// <summary>
        ///     分期检索式的查询
        /// </summary>
        /// <param name="query">分期检索式</param>
        /// <returns>查询</returns>
        public static FilterDefinition<T> GetNQuery<T>(IQueryCompunded<IDistributedQueryAtom> query)
            where T : IDistributed =>
            GetNativeFilter(query, GetNativeFilter<T>);

        /// <summary>
        ///     原子分期检索式的Native表示
        /// </summary>
        /// <param name="f">分期检索式</param>
        /// <returns>Native表示</returns>
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

            return And(lst);
        }

        /// <summary>
        ///     一般检索式的Native表示
        /// </summary>
        /// <param name="query">检索式</param>
        /// <param name="atomFilter">原子检索式的Native表示</param>
        /// <returns>Native表示</returns>
        private static FilterDefinition<T> GetNativeFilter<T, TAtom>(IQueryCompunded<TAtom> query,
            Func<TAtom, FilterDefinition<T>> atomFilter)
            where TAtom : class
        {
            if (query == null ||
                query is TAtom)
                return atomFilter((TAtom)query);

            var f = query as IQueryAry<TAtom>;
            if (f == null)
                throw new ArgumentException("检索式类型未知", nameof(query));

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
                    throw new ArgumentException("运算类型未知", nameof(query));
            }
        }
    }
}

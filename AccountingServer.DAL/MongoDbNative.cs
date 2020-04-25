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
    }

    internal abstract class MongoDbNativeVisitor<T, TAtom> : IQueryVisitor<TAtom, FilterDefinition<T>>
        where TAtom : class
    {
        public abstract FilterDefinition<T> Visit(TAtom query);

        public FilterDefinition<T> Visit(IQueryAry<TAtom> query)
        {
            switch (query.Operator)
            {
                case OperatorType.None:
                case OperatorType.Identity:
                    return query.Filter1.Accept(this);
                case OperatorType.Complement:
                    return !query.Filter1.Accept(this);
                case OperatorType.Union:
                    return query.Filter1.Accept(this) | query.Filter2.Accept(this);
                case OperatorType.Intersect:
                    return query.Filter1.Accept(this) & query.Filter2.Accept(this);
                case OperatorType.Substract:
                    return query.Filter1.Accept(this) & !query.Filter2.Accept(this);
                default:
                    throw new ArgumentException("运算类型未知", nameof(query));
            }
        }

        protected static FilterDefinition<TX> And<TX>(IReadOnlyCollection<FilterDefinition<TX>> lst) =>
            lst.Count == 0
                ? Builders<TX>.Filter.Empty
                : lst.Count == 1
                    ? lst.First()
                    : Builders<TX>.Filter.And(lst);

        /// <summary>
        ///     日期过滤器的Native表示
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Native表示</returns>
        protected static FilterDefinition<T> GetNativeFilter(DateFilter rng)
        {
            if (rng == null)
                return Builders<T>.Filter.Empty;

            if (rng.NullOnly)
                return Builders<T>.Filter.Exists("date", false);

            var lst = new List<FilterDefinition<T>>();

            if (rng.StartDate.HasValue)
                lst.Add(Builders<T>.Filter.Gte("date", rng.StartDate));
            if (rng.EndDate.HasValue)
                lst.Add(Builders<T>.Filter.Lte("date", rng.EndDate));

            if (lst.Count == 0)
                return rng.Nullable
                    ? Builders<T>.Filter.Empty
                    : Builders<T>.Filter.Exists("date");

            var gather = And(lst);
            return rng.Nullable
                ? Builders<T>.Filter.Exists("date", false) | gather
                : Builders<T>.Filter.Exists("date") & gather;
        }
    }

    internal abstract class MongoDbNativeDetail<T> : MongoDbNativeVisitor<T, IDetailQueryAtom>
    {
        protected MongoDbNativeDetail(bool unwinded = false) => Unwinded = unwinded;
        private bool Unwinded { get; }

        public override FilterDefinition<T> Visit(IDetailQueryAtom query)
        {
            var p = "";
            if (Unwinded)
                p = "detail.";

            var lst = new List<FilterDefinition<T>>();
            switch (query.Kind)
            {
                case TitleKind.Asset:
                    lst.Add(Builders<T>.Filter.Gte(p + "title", 1000));
                    lst.Add(Builders<T>.Filter.Lt(p + "title", 2000));
                    break;
                case TitleKind.Liability:
                    lst.Add(Builders<T>.Filter.Gte(p + "title", 2000));
                    lst.Add(Builders<T>.Filter.Lt(p + "title", 3000));
                    break;
                case TitleKind.Equity:
                    lst.Add(Builders<T>.Filter.Gte(p + "title", 4000));
                    lst.Add(Builders<T>.Filter.Lt(p + "title", 5000));
                    break;
                case TitleKind.Revenue:
                    lst.Add(Builders<T>.Filter.Gte(p + "title", 6000));
                    lst.Add(Builders<T>.Filter.Lt(p + "title", 6400));
                    break;
                case TitleKind.Expense:
                    lst.Add(Builders<T>.Filter.Gte(p + "title", 6400));
                    lst.Add(Builders<T>.Filter.Lt(p + "title", 7000));
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (query.Dir != 0)
                lst.Add(
                    query.Dir > 0
                        ? Builders<T>.Filter.Gt(p + "fund", -VoucherDetail.Tolerance)
                        : Builders<T>.Filter.Lt(p + "fund", +VoucherDetail.Tolerance));
            if (query.Filter?.User != null)
                lst.Add(Builders<T>.Filter.Eq(p + "user", query.Filter?.User));
            if (query.Filter?.Currency != null)
                lst.Add(Builders<T>.Filter.Eq(p + "currency", query.Filter?.Currency));
            if (query.Filter?.Title != null)
                lst.Add(Builders<T>.Filter.Eq(p + "title", query.Filter.Title.Value));
            if (query.Filter?.SubTitle != null)
                lst.Add(
                    query.Filter.SubTitle == 00
                        ? Builders<T>.Filter.Exists(p + "subtitle", false)
                        : Builders<T>.Filter.Eq(p + "subtitle", query.Filter.SubTitle.Value));
            if (query.Filter?.Content != null)
                lst.Add(
                    query.Filter.Content == string.Empty
                        ? Builders<T>.Filter.Exists(p + "content", false)
                        : Builders<T>.Filter.Eq(p + "content", query.Filter.Content));
            if (query.Filter?.Remark != null)
                lst.Add(
                    query.Filter.Remark == string.Empty
                        ? Builders<T>.Filter.Exists(p + "remark", false)
                        : Builders<T>.Filter.Eq(p + "remark", query.Filter.Remark));
            if (query.Filter?.Fund != null)
                lst.Add(
                    Builders<T>.Filter.Gte(p + "fund", query.Filter.Fund.Value - VoucherDetail.Tolerance) &
                    Builders<T>.Filter.Lte(p + "fund", query.Filter.Fund.Value + VoucherDetail.Tolerance));
            return And(lst);
        }
    }

    internal class MongoDbNativeDetail : MongoDbNativeDetail<VoucherDetail> { }

    internal class MongoDbNativeDetailUnwinded : MongoDbNativeDetail<BsonDocument>
    {
        public MongoDbNativeDetailUnwinded() : base(true) { }
    }

    internal class MongoDbNativeVoucher : MongoDbNativeVisitor<Voucher, IVoucherQueryAtom>
    {
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

            return And(lst);
        }

        public override FilterDefinition<Voucher> Visit(IVoucherQueryAtom query)
        {
            var lst = new List<FilterDefinition<Voucher>>
                {
                    GetNativeFilter(query.VoucherFilter), GetNativeFilter(query.Range)
                };
            var v = query.DetailFilter.Accept(new MongoDbNativeDetail());
            if (query.ForAll)
                lst.Add(!Builders<Voucher>.Filter.ElemMatch("detail", !v));
            else
                lst.Add(Builders<Voucher>.Filter.ElemMatch("detail", v));

            return And(lst);
        }
    }

    internal class MongoDbNativeDistributed<T> : MongoDbNativeVisitor<T, IDistributedQueryAtom>
        where T : IDistributed
    {
        public override FilterDefinition<T> Visit(IDistributedQueryAtom query)
        {
            if (query?.Filter == null)
                return Builders<T>.Filter.Empty;

            var lst = new List<FilterDefinition<T>>();
            if (query.Filter.ID.HasValue)
                lst.Add(Builders<T>.Filter.Eq("_id", query.Filter.ID.Value));
            if (query.Filter.User != null)
                lst.Add(Builders<T>.Filter.Eq("user", query.Filter.User));
            if (query.Filter.Name != null)
                lst.Add(
                    query.Filter.Name == string.Empty
                        ? Builders<T>.Filter.Exists("name", false)
                        : Builders<T>.Filter.Regex("name", query.Filter.Name));
            if (query.Filter.Remark != null)
                lst.Add(
                    query.Filter.Remark == string.Empty
                        ? Builders<T>.Filter.Exists("remark", false)
                        : Builders<T>.Filter.Eq("remark", query.Filter.Remark));
            if (query.Filter.User != null)
                lst.Add(Builders<T>.Filter.Eq("user", query.Filter.User));

            lst.Add(GetNativeFilter(query.Range));

            return And(lst);
        }
    }
}

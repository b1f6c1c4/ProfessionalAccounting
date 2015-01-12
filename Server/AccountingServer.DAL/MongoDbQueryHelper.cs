using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据库查询
    /// </summary>
    internal static class MongoDbQueryHelper
    {
        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetUniqueQuery(string id)
        {
            return Query.EQ("_id", ObjectId.Parse(id));
        }

        /// <summary>
        ///     按编号唯一查询
        /// </summary>
        /// <param name="id">编号</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetUniqueQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);
        }

        /// <summary>
        ///     按过滤器查询
        /// </summary>
        /// <param name="filter">过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Voucher filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.Type != null)
                switch (filter.Type)
                {
                    case VoucherType.Amortization:
                        lst.Add(Query.EQ("special", "amorz"));
                        break;
                    case VoucherType.AnnualCarry:
                        lst.Add(Query.EQ("special", "acarry"));
                        break;
                    case VoucherType.Carry:
                        lst.Add(Query.EQ("special", "carry"));
                        break;
                    case VoucherType.Depreciation:
                        lst.Add(Query.EQ("special", "dep"));
                        break;
                    case VoucherType.Devalue:
                        lst.Add(Query.EQ("special", "dev"));
                        break;
                    case VoucherType.Uncertain:
                        lst.Add(Query.EQ("special", "unc"));
                        break;
                }
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        /// <summary>
        ///     按日期查询
        /// </summary>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(DateFilter rng)
        {
            if (rng.NullOnly)
                return Query.EQ("date", BsonNull.Value);

            IMongoQuery q;
            if (rng.Constrained)
                q = Query.And(Query.GTE("date", rng.StartDate), Query.LTE("date", rng.EndDate));
            else if (rng.StartDate.HasValue)
                q = Query.GTE("date", rng.StartDate);
            else if (rng.EndDate.HasValue)
                q = Query.LTE("date", rng.EndDate);
            else
                return rng.Nullable ? null : Query.NE("date", BsonNull.Value);

            return rng.Nullable ? Query.Or(q, Query.EQ("date", BsonNull.Value)) : q;
        }

        /// <summary>
        ///     按细目过滤器查询
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(VoucherDetail filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.SubTitle != null)
                lst.Add(
                        filter.SubTitle == 0
                            ? Query.EQ("subtitle", BsonNull.Value)
                            : Query.EQ("subtitle", filter.SubTitle));
            if (filter.Content != null)
                lst.Add(
                        filter.Content == String.Empty
                            ? Query.EQ("content", BsonNull.Value)
                            : Query.EQ("content", filter.Content));
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));
            if (filter.Fund != null)
                lst.Add(Query.EQ("fund", filter.Fund));

            return And(lst);
        }

        /// <summary>
        ///     按过滤器和细目过滤器查询
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Voucher vfilter, VoucherDetail filter)
        {
            var queryVoucher = GetQuery(vfilter);
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按细目过滤器和日期查询
        /// </summary>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(VoucherDetail filter, DateFilter rng)
        {
            var queryVoucher = GetQuery(rng);
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按过滤器、细目过滤器和日期查询
        /// </summary>
        /// <param name="vfilter">过滤器</param>
        /// <param name="filter">细目过滤器</param>
        /// <param name="rng">日期过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Voucher vfilter, VoucherDetail filter, DateFilter rng)
        {
            var queryVoucher = And(GetQuery(vfilter), GetQuery(rng));
            var queryFilter = GetQuery(filter);
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     按资产过滤器查询
        /// </summary>
        /// <param name="filter">资产过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Asset filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.ID != null)
                lst.Add(Query.EQ("_id", filter.ID.Value.ToBsonValue()));
            if (filter.Name != null)
                lst.Add(
                        filter.Name == String.Empty
                            ? Query.EQ("name", BsonNull.Value)
                            : Query.EQ("name", filter.Name));
            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.Value != null)
                lst.Add(Query.EQ("value", filter.Value));
            if (filter.Salvge != null)
                lst.Add(Query.EQ("salvge", filter.Salvge));
            if (filter.Life != null)
                lst.Add(Query.EQ("life", filter.Life));
            if (filter.Title != null)
                lst.Add(Query.EQ("title", filter.Title));
            if (filter.DepreciationTitle != null)
                lst.Add(Query.EQ("deptitle", filter.DepreciationTitle));
            if (filter.DevaluationTitle != null)
                lst.Add(Query.EQ("devtitle", filter.DevaluationTitle));
            if (filter.DepreciationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exptitle",
                                 filter.DepreciationExpenseSubTitle != null
                                     ? filter.DepreciationExpenseTitle * 100 + filter.DepreciationExpenseSubTitle
                                     : filter.DepreciationExpenseTitle));
            if (filter.DevaluationExpenseTitle != null)
                lst.Add(
                        Query.EQ(
                                 "exvtitle",
                                 filter.DevaluationExpenseSubTitle != null
                                     ? filter.DevaluationExpenseTitle * 100 + filter.DevaluationExpenseSubTitle
                                     : filter.DevaluationExpenseTitle));
            if (filter.Method.HasValue)
                switch (filter.Method.Value)
                {
                    case DepreciationMethod.StraightLine:
                        lst.Add(Query.EQ("method", "sl"));
                        break;
                    case DepreciationMethod.SumOfTheYear:
                        lst.Add(Query.EQ("method", "sy"));
                        break;
                    case DepreciationMethod.DoubleDeclineMethod:
                        lst.Add(Query.EQ("method", "dd"));
                        break;
                }
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        /// <summary>
        ///     按摊销过滤器查询
        /// </summary>
        /// <param name="filter">资产过滤器</param>
        /// <returns>Bson查询</returns>
        public static IMongoQuery GetQuery(Amortization filter)
        {
            if (filter == null)
                return null;

            var lst = new List<IMongoQuery>();

            if (filter.ID != null)
                lst.Add(Query.EQ("_id", filter.ID.Value.ToBsonValue()));
            if (filter.Name != null)
                lst.Add(
                        filter.Name == String.Empty
                            ? Query.EQ("name", BsonNull.Value)
                            : Query.EQ("name", filter.Name));
            if (filter.Value != null)
                lst.Add(Query.EQ("value", filter.Value));
            if (filter.Date != null)
                lst.Add(Query.EQ("date", filter.Date));
            if (filter.TotalDays != null)
                lst.Add(Query.EQ("tday", filter.TotalDays));
            if (filter.Template != null)
                lst.Add(GetQuery(filter.Template));
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        public static IMongoQuery And(params IMongoQuery[] queries)
        {
            var lst = queries.ToList();
            return And(lst);
        }

        private static IMongoQuery And(ICollection<IMongoQuery> queries)
        {
            while (queries.Remove(null)) { }
            return queries.Any() ? Query.And(queries) : null;
        }
    }
}

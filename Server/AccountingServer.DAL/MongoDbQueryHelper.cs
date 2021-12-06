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
    ///     MongoDb���ݿ��ѯ
    /// </summary>
    internal static class MongoDbQueryHelper
    {
        /// <summary>
        ///     �����Ψһ��ѯ
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetUniqueQuery(string id)
        {
            return Query.EQ("_id", ObjectId.Parse(id));
        }

        /// <summary>
        ///     �����Ψһ��ѯ
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetUniqueQuery(Guid? id)
        {
            return Query.EQ("_id", id.HasValue ? id.Value.ToBsonValue() as BsonValue : BsonNull.Value);
        }

        /// <summary>
        ///     ����������ѯ
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>Bson��ѯ</returns>
        private static IMongoQuery GetAtomQuery(Voucher filter)
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
        ///     �����ڲ�ѯ
        /// </summary>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>Bson��ѯ</returns>
        private static IMongoQuery GetAtomQuery(DateFilter? rng)
        {
            if (!rng.HasValue)
                return null;

            if (rng.Value.NullOnly)
                return Query.EQ("date", BsonNull.Value);

            IMongoQuery q;
            if (rng.Value.Constrained)
                q = Query.And(Query.GTE("date", rng.Value.StartDate), Query.LTE("date", rng.Value.EndDate));
            else if (rng.Value.StartDate.HasValue)
                q = Query.GTE("date", rng.Value.StartDate);
            else if (rng.Value.EndDate.HasValue)
                q = Query.LTE("date", rng.Value.EndDate);
            else
                return rng.Value.Nullable ? null : Query.NE("date", BsonNull.Value);

            return rng.Value.Nullable ? Query.Or(q, Query.EQ("date", BsonNull.Value)) : q;
        }

        /// <summary>
        ///     ��ϸĿ��������ѯ
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>Bson��ѯ</returns>
        private static IMongoQuery GetAtomQuery(VoucherDetail filter)
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
        ///     ����������ϸĿ�����������ڲ�ѯ
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <param name="dir">+1��ʾֻ���ǽ跽��-1��ʾֻ���Ǵ�����0��ʾͬʱ���ǽ跽�ʹ���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetQuery(Voucher vfilter = null,
                                           VoucherDetail filter = null,
                                           DateFilter? rng = null,
                                           int dir = 0)
        {
            var queryVoucher = And(GetAtomQuery(vfilter), GetAtomQuery(rng));
            var queryFilter = GetAtomQuery(filter);
            if (dir != 0)
                queryFilter = And(queryFilter, dir > 0 ? Query.GT("fund", 0) : Query.LT("fund", 0));
            return queryFilter != null ? And(queryVoucher, Query.ElemMatch("detail", queryFilter)) : queryVoucher;
        }

        /// <summary>
        ///     ������������ϸĿ�����������ڲ�ѯ
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filters">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <param name="useAnd">��ϸĿ������֮��Ĺ�ϵΪ��ȡ</param>
        /// <param name="dir">+1��ʾֻ���ǽ跽��-1��ʾֻ���Ǵ�����0��ʾͬʱ���ǽ跽�ʹ���</param>
        /// <returns>Bson��ѯ</returns>
        public static IMongoQuery GetQuery(Voucher vfilter = null,
                                           IEnumerable<VoucherDetail> filters = null,
                                           DateFilter? rng = null,
                                           int dir = 0,
                                           bool useAnd = false)
        {
            var queryFilters = filters != null
                                   ? filters.Select(
                                                    f =>
                                                    {
                                                        var q = GetAtomQuery(f);
                                                        if (dir != 0)
                                                            q = And(
                                                                    q,
                                                                    dir > 0 ? Query.GT("fund", 0) : Query.LT("fund", 0));
                                                        return Query.ElemMatch("detail", q);
                                                    }).ToList()
                                   : null;
            var queryFilter = useAnd ? And(queryFilters) : Or(queryFilters);
            return And(GetAtomQuery(vfilter), queryFilter, GetAtomQuery(rng));
        }

        /// <summary>
        ///     ���ʲ���������ѯ
        /// </summary>
        /// <param name="filter">�ʲ�������</param>
        /// <returns>Bson��ѯ</returns>
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
        ///     ��̯����������ѯ
        /// </summary>
        /// <param name="filter">�ʲ�������</param>
        /// <returns>Bson��ѯ</returns>
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
            //if (filter.Template != null)
            //    throw new NotImplementedException(); TODO: query according to Template
            if (filter.Remark != null)
                lst.Add(
                        filter.Remark == String.Empty
                            ? Query.EQ("remark", BsonNull.Value)
                            : Query.EQ("remark", filter.Remark));

            return And(lst);
        }

        /// <summary>
        ///     ��ȡ
        /// </summary>
        /// <param name="queries">��ѯ</param>
        /// <returns>��ȡ</returns>
        private static IMongoQuery And(params IMongoQuery[] queries)
        {
            var lst = queries.ToList();
            return And(lst);
        }

        /// <summary>
        ///     ��ȡ
        /// </summary>
        /// <param name="queries">��ѯ</param>
        /// <returns>��ȡ</returns>
        private static IMongoQuery And(ICollection<IMongoQuery> queries)
        {
            while (queries.Remove(null)) { }
            return queries.Any() ? Query.And(queries) : null;
        }

        /// <summary>
        ///     ��ȡ
        /// </summary>
        /// <param name="queries">��ѯ</param>
        /// <returns>��ȡ</returns>
        // ReSharper disable once UnusedMember.Global
        public static IMongoQuery Or(params IMongoQuery[] queries)
        {
            var lst = queries.ToList();
            return Or(lst);
        }

        /// <summary>
        ///     ��ȡ
        /// </summary>
        /// <param name="queries">��ѯ</param>
        /// <returns>��ȡ</returns>
        private static IMongoQuery Or(ICollection<IMongoQuery> queries)
        {
            while (queries.Remove(null)) { }
            return queries.Any() ? Query.Or(queries) : null;
        }
    }
}

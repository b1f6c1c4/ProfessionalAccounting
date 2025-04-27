/* Copyright (C) 2020-2025 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AccountingServer.DAL.Serializer;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccountingServer.DAL;

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
    ///     按编号查询<c>ObjectId</c>
    /// </summary>
    /// <param name="ids">编号</param>
    /// <returns>Bson查询</returns>
    public static FilterDefinition<T> GetNQuery<T>(IEnumerable<string> ids)
        => Builders<T>.Filter.In("_id", ids.Select(ObjectId.Parse));

    /// <summary>
    ///     按编号查询<c>ObjectId</c>
    /// </summary>
    /// <param name="ids">编号</param>
    /// <returns>Bson查询</returns>
    public static FilterDefinition<T> GetXQuery<T>(IEnumerable<string> ids)
        => ids == null
            ? Builders<T>.Filter.Empty
            : Builders<T>.Filter.Nin("_id", ids.Select(ObjectId.Parse));

    /// <summary>
    ///     按编号查询<c>Guid</c>
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>Bson查询</returns>
    public static FilterDefinition<T> GetNQuery<T>(Guid? id) =>
        Builders<T>.Filter.Eq<BsonValue>("_id", id.HasValue ? id.Value.ToBsonValue() : BsonNull.Value);

    /// <summary>
    ///     按编号查询<c>BinData</c>
    /// </summary>
    /// <param name="id">编号</param>
    /// <returns>Bson查询</returns>
    public static FilterDefinition<T> GetNQuery<T>(byte[] id) =>
        Builders<T>.Filter.Eq<BsonValue>("_id", id != null ? new BsonBinaryData(id) : BsonNull.Value);
}

internal abstract class MongoDbNativeVisitor<T, TAtom> : IQueryVisitor<TAtom, FilterDefinition<T>>
    where TAtom : class
{
    public abstract FilterDefinition<T> Visit(TAtom query);

    public FilterDefinition<T> Visit(IQueryAry<TAtom> query)
        => query.Operator switch
            {
                OperatorType.None => query.Filter1.Accept(this),
                OperatorType.Identity => query.Filter1.Accept(this),
                OperatorType.Complement => !query.Filter1.Accept(this),
                OperatorType.Union => query.Filter1.Accept(this) | query.Filter2.Accept(this),
                OperatorType.Intersect => query.Filter1.Accept(this) & query.Filter2.Accept(this),
                OperatorType.Subtract => query.Filter1.Accept(this) & !query.Filter2.Accept(this),
                _ => throw new ArgumentException("运算类型未知", nameof(query)),
            };

    protected static FilterDefinition<TX> And<TX>(IReadOnlyCollection<FilterDefinition<TX>> lst)
        => lst.Count switch
            {
                0 => Builders<TX>.Filter.Empty,
                1 => lst.First(),
                _ => Builders<TX>.Filter.And(lst),
            };

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

    /// <summary>
    ///     获取前缀正则表达式
    /// </summary>
    /// <param name="s">前缀字符串</param>
    /// <returns>前缀正则表达式</returns>
    protected static BsonRegularExpression PrefixRegex(string s)
        => new($"^{Regex.Escape(s)}", "i");
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
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 1000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 2000));
                break;
            case TitleKind.Liability:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 2000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 3000));
                break;
            case TitleKind.Mutual:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 3000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 4000));
                break;
            case TitleKind.Equity:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 4000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 5000));
                break;
            case TitleKind.Static:
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 5000));
                break;
            case TitleKind.Dynamic:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 5000));
                break;
            case TitleKind.Cost:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 5000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 6000));
                break;
            case TitleKind.Revenue:
                lst.Add((Builders<T>.Filter.Gte($"{p}title", 6000) & Builders<T>.Filter.Lt($"{p}title", 6400))
                    | (Builders<T>.Filter.Eq($"{p}title", 6603) & Builders<T>.Filter.Eq($"{p}subtitle", 02)));
                break;
            case TitleKind.Expense:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 6400) & Builders<T>.Filter.Lt($"{p}title", 7000)
                    & !(Builders<T>.Filter.Eq($"{p}title", 6603) & Builders<T>.Filter.Eq($"{p}subtitle", 02)));
                break;
            case TitleKind.Liquidity:
                lst.Add(Builders<T>.Filter.Gte($"{p}title", 1000));
                lst.Add(Builders<T>.Filter.Lt($"{p}title", 1100));
                break;
            case TitleKind.Investment:
                lst.Add(Builders<T>.Filter.In($"{p}title", new[] { 1101, 1441 })
                    | (Builders<T>.Filter.Gte($"{p}title", 1500) & Builders<T>.Filter.Lt($"{p}title", 1600))
                    | (Builders<T>.Filter.Gte($"{p}title", 6100) & Builders<T>.Filter.Lt($"{p}title", 6200))
                    | (Builders<T>.Filter.Eq($"{p}title", 1012) & Builders<T>.Filter.Eq($"{p}subtitle", 04)));
                break;
            case TitleKind.Spending:
                lst.Add(Builders<T>.Filter.In($"{p}title", new[] { 6401, 6402, 6603, 1601, 1604, 1605, 1701 })
                    | (Builders<T>.Filter.Gte($"{p}title", 1400) & Builders<T>.Filter.Lt($"{p}title", 1420))
                    | (Builders<T>.Filter.Eq($"{p}title", 6602) &
                        Builders<T>.Filter.Nin($"{p}subtitle", new[] { 07, 11 }))
                    | (Builders<T>.Filter.Eq($"{p}title", 6711) & Builders<T>.Filter.Eq($"{p}subtitle", 09)));
                break;
            case null:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (query.IsPseudoCurrency)
        {
            case true:
                lst.Add(Builders<T>.Filter.Regex($"{p}currency", "#$"));
                break;
            case false:
                lst.Add(!Builders<T>.Filter.Regex($"{p}currency", "#$"));
                break;
        }

        if (query.Dir != 0)
            lst.Add(
                query.Dir > 0
                    ? Builders<T>.Filter.Gt($"{p}fund", -VoucherDetail.Tolerance)
                    : Builders<T>.Filter.Lt($"{p}fund", +VoucherDetail.Tolerance));
        if (query.ContentPrefix != null)
            lst.Add(Builders<T>.Filter.Regex($"{p}content", PrefixRegex(query.ContentPrefix)));
        if (query.RemarkPrefix != null)
            lst.Add(Builders<T>.Filter.Regex($"{p}remark", PrefixRegex(query.RemarkPrefix)));
        if (query.Filter?.User != null)
            lst.Add(Builders<T>.Filter.Eq($"{p}user", query.Filter?.User));
        if (query.Filter?.Currency != null)
            lst.Add(Builders<T>.Filter.Eq($"{p}currency", query.Filter?.Currency));
        if (query.Filter?.Title != null)
            lst.Add(Builders<T>.Filter.Eq($"{p}title", query.Filter.Title.Value));
        if (query.Filter?.SubTitle != null)
            lst.Add(query.Filter.SubTitle switch
                {
                    00 => Builders<T>.Filter.Exists($"{p}subtitle", false),
                    var x => Builders<T>.Filter.Eq($"{p}subtitle", x!.Value),
                });
        if (query.Filter?.Content != null)
            lst.Add(query.Filter.Content switch
                {
                    "" => Builders<T>.Filter.Exists($"{p}content", false),
                    var x => Builders<T>.Filter.Eq($"{p}content", x),
                });
        if (query.Filter?.Remark != null)
            lst.Add(query.Filter.Remark switch
                {
                    "" => Builders<T>.Filter.Exists($"{p}remark", false),
                    var x => Builders<T>.Filter.Eq($"{p}remark", x),
                });
        if (query.Filter?.Fund != null)
        {
            var f = Builders<T>.Filter.Gte($"{p}fund", query.Filter.Fund.Value - VoucherDetail.Tolerance) &
                Builders<T>.Filter.Lte($"{p}fund", query.Filter.Fund.Value + VoucherDetail.Tolerance);
            if (query.IsFundBidirectional)
                f |= Builders<T>.Filter.Gte($"{p}fund", -query.Filter.Fund.Value - VoucherDetail.Tolerance) &
                    Builders<T>.Filter.Lte($"{p}fund", -query.Filter.Fund.Value + VoucherDetail.Tolerance);
            lst.Add(f);
        }

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
            lst.Add(vfilter.Type switch
                {
                    VoucherType.Ordinary => Builders<Voucher>.Filter.Exists("special", false),
                    VoucherType.General =>
                        Builders<Voucher>.Filter.Nin("special", new[] { "acarry", "carry" }),
                    VoucherType.Amortization => Builders<Voucher>.Filter.Eq("special", "amorz"),
                    VoucherType.AnnualCarry => Builders<Voucher>.Filter.Eq("special", "acarry"),
                    VoucherType.Carry => Builders<Voucher>.Filter.Eq("special", "carry"),
                    VoucherType.Depreciation => Builders<Voucher>.Filter.Eq("special", "dep"),
                    VoucherType.Devalue => Builders<Voucher>.Filter.Eq("special", "dev"),
                    VoucherType.Uncertain => Builders<Voucher>.Filter.Eq("special", "unc"),
                    _ => throw new InvalidOperationException(),
                });

        if (vfilter.Remark != null)
            lst.Add(vfilter.Remark switch
                {
                    "" => Builders<Voucher>.Filter.Exists("remark", false),
                    var x => Builders<Voucher>.Filter.Eq("remark", x),
                });

        return And(lst);
    }

    public override FilterDefinition<Voucher> Visit(IVoucherQueryAtom query)
    {
        var lst = new List<FilterDefinition<Voucher>>
            {
                GetNativeFilter(query.VoucherFilter), GetNativeFilter(query.Range),
            };
        if (query.RemarkPrefix != null)
            lst.Add(Builders<Voucher>.Filter.Regex("remark", PrefixRegex(query.RemarkPrefix)));
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
            lst.Add(query.Filter.Name switch
                {
                    "" => Builders<T>.Filter.Exists("name", false),
                    var x => Builders<T>.Filter.Regex("name", x),
                });
        if (query.Filter.Remark != null)
            lst.Add(query.Filter.Remark switch
                {
                    "" => Builders<T>.Filter.Exists("remark", false),
                    var x => Builders<T>.Filter.Eq("remark", x),
                });
        if (query.Filter.User != null)
            lst.Add(Builders<T>.Filter.Eq("user", query.Filter.User));

        lst.Add(GetNativeFilter(query.Range));

        return And(lst);
    }
}

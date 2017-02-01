using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.THUInfo
{
    public partial class THUInfo
    {
        /// <summary>
        ///     包含记账凭证信息的细目
        /// </summary>
        private struct VDetail
        {
            /// <summary>
            ///     细目
            /// </summary>
            public VoucherDetail Detail;

            /// <summary>
            ///     细目所在的记账凭证
            /// </summary>
            public Voucher Voucher;
        }

        /// <summary>
        ///     对账问题
        /// </summary>
        private sealed class Problem
        {
            /// <summary>
            ///     消费记录组
            /// </summary>
            public List<TransactionRecord> Records { get; }

            /// <summary>
            ///     记账凭证组
            /// </summary>
            public List<VDetail> Details { get; }

            public Problem(List<TransactionRecord> records, List<VDetail> details)
            {
                Records = records;
                Details = details;
            }
        }

        /// <summary>
        ///     对账问题
        /// </summary>
        private sealed class Problems
        {
            /// <summary>
            ///     无法自动补足备注的记账凭证
            /// </summary>
            public IReadOnlyList<Problem> NoRemark { get; set; }

            /// <summary>
            ///     一组交易记录对应的记账凭证过多
            /// </summary>
            public IReadOnlyList<Problem> TooMuch { get; set; }

            /// <summary>
            ///     一组交易记录对应的记账凭证过少
            /// </summary>
            public IReadOnlyList<Problem> TooFew { get; set; }

            /// <summary>
            ///     一个记账凭证没有可能对应的交易记录
            /// </summary>
            public IReadOnlyList<VDetail> NoRecord { get; set; }

            /// <summary>
            ///     存在问题
            /// </summary>
            public bool Any => NoRemark.Any() ||
                TooMuch.Any() ||
                TooFew.Any(p => p.Details.Any()) ||
                NoRecord.Any();

            /// <summary>
            ///     待生成条目
            /// </summary>
            public IEnumerable<TransactionRecord> Records => TooFew.SelectMany(p => p.Records);
        }

        /// <summary>
        ///     对账并自动补足备注
        /// </summary>
        /// <returns>对账问题</returns>
        private Problems Compare()
        {
            var data = new List<TransactionRecord>(m_Crawler.Result);

            var voucherQuery = new VoucherQueryAtomBase { DetailFilter = DetailQuery };
            var bin = new HashSet<int>();
            var binConflict = new HashSet<int>();
            foreach (var d in Accountant.SelectVouchers(voucherQuery)
                .SelectMany(
                    v => v.Details.Where(d => d.IsMatch(DetailQuery))
                        .Select(d => new VDetail { Detail = d, Voucher = v })))
            {
                var id = Convert.ToInt32(d.Detail.Remark);
                if (!bin.Add(id))
                {
                    binConflict.Add(id);
                    continue;
                }

                var record = data.SingleOrDefault(r => r.Index == id);
                if (record == null)
                {
                    d.Detail.Remark = null;
                    Accountant.Upsert(d.Voucher);
                    continue;
                }
                // ReSharper disable once PossibleInvalidOperationException
                if (!(Math.Abs(d.Detail.Fund.Value) - record.Fund).IsZero())
                {
                    d.Detail.Remark = null;
                    Accountant.Upsert(d.Voucher);
                    continue;
                }

                data.Remove(record);
            }
            foreach (var id in binConflict)
            {
                var record = m_Crawler.Result.SingleOrDefault(r => r.Index == id);
                if (record != null)
                    data.Add(record);

                var idv = id.ToString(CultureInfo.InvariantCulture);
                foreach (var voucher in Accountant.RunVoucherQuery($"T101205 {idv.Quotation('"')}"))
                {
                    foreach (var d in
                        voucher.Details.Where(d => d.Title == 1012 && d.SubTitle == 05 && d.Remark == idv))
                        d.Remark = null;

                    Accountant.Upsert(voucher);
                }
            }

            var account = Accountant.RunVoucherQuery("T101205 \"\"")
                .SelectMany(
                    v =>
                        v.Details.Where(
                                d =>
                                    d.Title == 1012 && d.Title == 05 && d.Remark == null)
                            .Select(d => new VDetail { Detail = d, Voucher = v })).ToList();

            var noRemark = new List<Problem>();
            var tooMuch = new List<Problem>();
            var tooFew = new List<Problem>();
            var noRecord = new List<VDetail>(account);

            foreach (var dfgrp in data.GroupBy(r => new { r.Date, r.Fund }))
            {
                var grp = dfgrp;
                var acc =
                    account.Where(
                        d =>
                            d.Voucher.Date == grp.Key.Date &&
                            // ReSharper disable once PossibleInvalidOperationException
                            (Math.Abs(d.Detail.Fund.Value) - grp.Key.Fund).IsZero()).ToList();
                noRecord.RemoveAll(acc.Contains);
                switch (acc.Count.CompareTo(grp.Count()))
                {
                    case 0:
                        if (acc.Count == 1)
                        {
                            acc.Single().Detail.Remark = grp.Single().Index.ToString(CultureInfo.InvariantCulture);
                            Accountant.Upsert(acc.Single().Voucher);
                        }
                        else
                            noRemark.Add(new Problem(grp.ToList(), acc));
                        break;
                    case 1:
                        tooMuch.Add(new Problem(grp.ToList(), acc));
                        break;
                    case -1:
                        tooFew.Add(new Problem(grp.ToList(), acc));
                        break;
                }
            }

            return new Problems
                {
                    NoRecord = noRecord,
                    NoRemark = noRemark,
                    TooFew = tooFew,
                    TooMuch = tooMuch
                };
        }

        /// <summary>
        ///     显示对账结果
        /// </summary>
        /// <param name="problems">对账问题</param>
        /// <returns>执行结果</returns>
        private IQueryResult ShowComparison(Problems problems)
        {
            var sb = new StringBuilder();
            foreach (var problem in problems.NoRemark)
            {
                sb.AppendLine("---No Remark");
                foreach (var r in problem.Records)
                    sb.AppendLine(r.ToString());
                foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                    sb.AppendLine(Serializer.PresentVoucher(v));

                sb.AppendLine();
            }
            foreach (var problem in problems.TooMuch)
            {
                sb.AppendLine("---Too Much Voucher");
                foreach (var r in problem.Records)
                    sb.AppendLine(r.ToString());
                foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                    sb.AppendLine(Serializer.PresentVoucher(v));

                sb.AppendLine();
            }
            foreach (var problem in problems.TooFew)
            {
                sb.AppendLine("---Too Few Voucher");
                foreach (var r in problem.Records)
                    sb.AppendLine(r.ToString());
                foreach (var v in problem.Details.Select(d => d.Voucher).Distinct())
                    sb.AppendLine(Serializer.PresentVoucher(v));

                sb.AppendLine();
            }

            if (problems.NoRecord.Any())
            {
                sb.AppendLine("---No Record");
                foreach (var v in problems.NoRecord.Select(d => d.Voucher).Distinct())
                    sb.AppendLine(Serializer.PresentVoucher(v));
            }

            if (sb.Length == 0)
                return new Succeed();

            return new EditableText(sb.ToString());
        }
    }
}

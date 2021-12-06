﻿using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Interest
{
    /// <summary>
    ///     自动计算利息和还款
    /// </summary>
    internal abstract class InterestBase : PluginBase
    {
        protected InterestBase(Accountant accountant) : base(accountant) { }

        /// <summary>
        ///     主过滤器
        /// </summary>
        /// <returns>科目编号</returns>
        protected abstract string MajorFilter();

        /// <summary>
        ///     主科目借贷方向
        /// </summary>
        /// <returns>+1表示借方，-1表示贷方</returns>
        protected abstract int Dir();

        /// <summary>
        ///     损益子科目
        /// </summary>
        /// <returns>子科目编号</returns>
        protected abstract int MinorSubTitle();

        /// <inheritdoc />
        public override IQueryResult Execute(string expr, IEntitiesSerializer serializer)
        {
            var remark = Parsing.Token(ref expr);
            var rate = Parsing.DoubleF(ref expr) / 10000D;
            var all = Parsing.Optional(ref expr, "all");
            var endDate = !all ? Parsing.UniqueTime(ref expr) : null;
            Parsing.Eof(expr);

            var loans = Accountant.RunGroupedQuery($"({MajorFilter()})-\"\" ``rtcC").Items
                .Cast<ISubtotalRemark>()
                .ToList();
            var rmkObj =
                loans.Single(
                    b =>
                        b.Remark?.StartsWith(remark, StringComparison.InvariantCultureIgnoreCase) == true &&
                        !b.Remark.EndsWith("-利息", StringComparison.Ordinal));
            var titleObj = rmkObj.Items.Cast<ISubtotalTitle>().Single();
            var cntObj = titleObj.Items.Cast<ISubtotalContent>().Single();
            // ReSharper disable once PossibleInvalidOperationException
            var title = titleObj.Title.Value;
            var content = cntObj.Content;
            var rmk = rmkObj.Remark;
            var currency = cntObj.Items.Cast<ISubtotalCurrency>().Single().Currency;
            var info = new LoanInfo
                {
                    Currency = currency,
                    Title = title,
                    Content = content,
                    Remark = rmk,
                    Rate = rate
                };

            if (!all && !endDate.HasValue ||
                endDate.HasValue)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var lastD = Accountant.RunVoucherQuery(info.QueryInterest(this))
                        .OrderByDescending(v => v.Date, new DateComparer())
                        .FirstOrDefault()
                        ?.Date ??
                    Accountant.RunVoucherQuery(info.QueryCapital(this))
                        .OrderBy(v => v.Date, new DateComparer())
                        .First()
                        .Date.Value;
                var capQuery = $"{info.QueryCapital(this)} [~{lastD.AsDate()}]``v";
                var intQuery = $"{info.QueryInterest(this)} [~{lastD.AsDate()}]``v";
                var capitalIntegral = Accountant.RunGroupedQuery(capQuery).Fund;
                var interestIntegral = Accountant.RunGroupedQuery(intQuery).Fund;
                Regularize(
                    info,
                    ref capitalIntegral,
                    ref interestIntegral,
                    lastD,
                    endDate ?? ClientDateTime.Today);
            }
            else
            {
                var capitalIntegral = 0D;
                var interestIntegral = 0D;
                Regularize(
                    info,
                    ref capitalIntegral,
                    ref interestIntegral,
                    null,
                    ClientDateTime.Today);
            }

            return new DirtySucceed();
        }

        /// <summary>
        ///     从上次计息日后一日起计算单利利息并整理还款
        /// </summary>
        /// <param name="info">借款信息</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="interestIntegral">剩余利息</param>
        /// <param name="lastSettlement">上次计息日</param>
        /// <param name="finalDay">截止日期</param>
        private void Regularize(LoanInfo info, ref double capitalIntegral, ref double interestIntegral,
            DateTime? lastSettlement, DateTime finalDay)
        {
            var capitalPattern = info.AsCapital(this);
            var interestPattern = info.AsInterest(this);
            var rng = lastSettlement.HasValue
                ? new DateFilter(lastSettlement.Value.AddDays(1), finalDay)
                : new DateFilter(null, finalDay);
            foreach (var grp in
                Accountant
                    .RunVoucherQuery($"{info.QueryMajor(this)} {rng.AsDateRange()}")
                    .GroupBy(v => v.Date)
                    .OrderBy(grp => grp.Key, new DateComparer()))
            {
                var key = grp.Key ?? throw new ApplicationException("无法处理无穷长时间以前的利息收入");

                if (!lastSettlement.HasValue)
                    lastSettlement = key;

                // Settle Interest
                interestIntegral += SettleInterest(
                    info,
                    capitalIntegral,
                    key.Subtract(lastSettlement.Value).Days,
                    grp.SingleOrDefault(
                        v =>
                            v.Details.Any(d => d.IsMatch(interestPattern, Dir())))
                    ?? new Voucher
                        {
                            Date = key,
                            Details = new List<VoucherDetail>()
                        });
                lastSettlement = key;

                // Settle Loan
                // ReSharper disable once PossibleInvalidOperationException
                capitalIntegral +=
                    grp.SelectMany(v => v.Details.Where(d => d.IsMatch(capitalPattern, Dir())))
                        .Select(d => d.Fund.Value)
                        .Sum();

                // Settle Return
                foreach (
                    var voucher in
                    grp.Where(
                            v =>
                                v.Details.Any(
                                    d => d.IsMatch(capitalPattern, -Dir()) || d.IsMatch(interestPattern, -Dir())))
                        .OrderBy(v => v.ID))
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var value =
                        -voucher.Details.Where(
                                d => d.IsMatch(capitalPattern, -Dir()) || d.IsMatch(interestPattern, -Dir()))
                            .Select(d => d.Fund.Value)
                            .Sum();
                    if ((Dir() * (-value + interestIntegral)).IsNonNegative())
                    {
                        RegularizeVoucherDetail(info, voucher, 0, value);
                        interestIntegral -= value;
                    }
                    else
                    {
                        RegularizeVoucherDetail(info, voucher, value - interestIntegral, interestIntegral);
                        capitalIntegral -= value - interestIntegral;
                        interestIntegral = 0;
                    }
                }
            }

            if (lastSettlement == null)
                throw new ApplicationException("无法处理无穷长时间以前的利息收入");

            if (lastSettlement != finalDay)
                interestIntegral += SettleInterest(
                    info,
                    capitalIntegral,
                    finalDay.Subtract(lastSettlement.Value).Days,
                    new Voucher
                        {
                            Date = finalDay,
                            Details = new List<VoucherDetail>()
                        });
        }

        /// <summary>
        ///     计算利息
        /// </summary>
        /// <param name="info">借款信息</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="delta">间隔日数</param>
        /// <param name="voucher">记账凭证</param>
        /// <returns>利息</returns>
        private double SettleInterest(LoanInfo info, double capitalIntegral, int delta, Voucher voucher)
        {
            var interest = delta * info.Rate * capitalIntegral;
            var create = new List<VoucherDetail>
                {
                    info.AsInterest(this, interest),
                    info.AsMinor(this, -interest)
                };

            // ReSharper disable once PossibleInvalidOperationException
            var detail = voucher.Details.SingleOrDefault(d => d.IsMatch(info.AsInterest(this)));

            if (interest.IsZero())
            {
                if (detail != null)
                {
                    if (!voucher.Details.All(d => d.IsMatch(info.AsInterest(this)) || d.IsMatch(info.AsMinor(this))))
                        throw new ArgumentException("该记账凭证包含计息以外的细目", nameof(voucher));

                    Accountant.DeleteVoucher(voucher.ID);
                }

                return 0;
            }

            if (detail == null)
            {
                voucher.Details = create;
                Accountant.Upsert(voucher);
            }
            // ReSharper disable once PossibleInvalidOperationException
            else if (!(detail.Fund.Value - interest).IsZero())
            {
                if (!voucher.Details.All(d => d.IsMatch(info.AsInterest(this)) || d.IsMatch(info.AsMinor(this))))
                    throw new ArgumentException("该记账凭证包含计息以外的细目", nameof(voucher));

                voucher.Details = create;
                Accountant.Upsert(voucher);
            }

            return interest;
        }

        /// <summary>
        ///     正确登记还款
        /// </summary>
        /// <param name="info">借款信息</param>
        /// <param name="voucher">记账凭证</param>
        /// <param name="capVol">本金还款额</param>
        /// <param name="intVol">利息还款额</param>
        private void RegularizeVoucherDetail(LoanInfo info, Voucher voucher, double capVol, double intVol)
        {
            var flag = false;
            var capFlag = false;
            var intFlag = false;
            for (var i = 0; i < voucher.Details.Count; i++)
            {
                if (voucher.Details[i].IsMatch(info.AsCapital(this), -Dir()))
                {
                    if (capFlag || capVol.IsZero())
                    {
                        voucher.Details.RemoveAt(i);
                        flag = true;
                        i--;
                        continue;
                    }

                    // ReSharper disable once PossibleInvalidOperationException
                    if (!(voucher.Details[i].Fund.Value - capVol).IsZero())
                    {
                        voucher.Details[i].Fund = -capVol;
                        flag = true;
                    }

                    capFlag = true;
                }

                if (voucher.Details[i].IsMatch(info.AsInterest(this), -Dir()))
                {
                    if (intFlag || intVol.IsZero())
                    {
                        voucher.Details.RemoveAt(i);
                        flag = true;
                        i--;
                        continue;
                    }

                    // ReSharper disable once PossibleInvalidOperationException
                    if (!(voucher.Details[i].Fund.Value - intVol).IsZero())
                    {
                        voucher.Details[i].Fund = -intVol;
                        flag = true;
                    }

                    intFlag = true;
                }
            }

            if (!capFlag &&
                !capVol.IsZero())
            {
                voucher.Details.Add(info.AsCapital(this, -capVol));
                flag = true;
            }

            if (!intFlag &&
                !intVol.IsZero())
            {
                voucher.Details.Add(info.AsInterest(this, -intVol));
                flag = true;
            }

            if (flag)
                Accountant.Upsert(voucher);
        }

        private sealed class LoanInfo
        {
            /// <summary>
            ///     币种
            /// </summary>
            public string Currency { private get; set; }

            /// <summary>
            ///     科目
            /// </summary>
            public int Title { private get; set; }

            /// <summary>
            ///     借款人
            /// </summary>
            public string Content { private get; set; }

            /// <summary>
            ///     借款代码
            /// </summary>
            public string Remark { private get; set; }

            /// <summary>
            ///     日利率
            /// </summary>
            public double Rate { get; set; }

            public string QueryMajor(InterestBase my) =>
                $"(@{Currency} T{Title.AsTitle()} {Content.Quotation('\'')})*({Remark.Quotation('"')}+{(Remark + "-利息").Quotation('"')})";

            public string QueryCapital(InterestBase my) =>
                $"@{Currency} T{Title.AsTitle()} {Content.Quotation('\'')} {Remark.Quotation('"')}";

            public string QueryInterest(InterestBase my) =>
                $"@{Currency} T{Title.AsTitle()} {Content.Quotation('\'')} {(Remark + "-利息").Quotation('"')}";

            public VoucherDetail AsCapital(InterestBase my, double? fund = null)
                => new VoucherDetail
                    {
                        Currency = Currency,
                        Title = Title,
                        Content = Content,
                        Remark = Remark,
                        Fund = fund
                    };

            public VoucherDetail AsInterest(InterestBase my, double? fund = null)
                => new VoucherDetail
                    {
                        Currency = Currency,
                        Title = Title,
                        Content = Content,
                        Remark = Remark + "-利息",
                        Fund = fund
                    };

            public VoucherDetail AsMinor(InterestBase my, double? fund = null)
                => new VoucherDetail
                    {
                        Currency = Currency,
                        Title = 6603,
                        SubTitle = my.MinorSubTitle(),
                        Content = "贷款利息",
                        Fund = fund
                    };
        }
    }
}

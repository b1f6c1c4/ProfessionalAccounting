using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.BLL.Parsing;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;
using AccountingServer.Shell.Serializer;
using AccountingServer.Shell.Util;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell.Plugins.Interest
{
    /// <summary>
    ///     自动计算利息收入和还款
    /// </summary>
    internal class InterestRevenue : PluginBase
    {
        public InterestRevenue(Accountant accountant, IEntitySerializer serializer) : base(accountant, serializer) { }

        /// <inheritdoc />
        public override IQueryResult Execute(string expr)
        {
            var content = Parsing.Token(ref expr);
            var remark = Parsing.Token(ref expr);
            var rate = Parsing.DoubleF(ref expr) / 10000D;
            var all = Parsing.Optional(ref expr, "all");
            var endDate = !all ? Parsing.UniqueTime(ref expr) : null;
            Parsing.Eof(expr);

            var loans = Accountant.RunGroupedQuery($"T1221 {content.Quotation('\'')} ``r").Items.Cast<ISubtotalRemark>()
                .ToList();
            var rmk =
                loans.Single(
                        b =>
                            b.Remark?.StartsWith(remark, StringComparison.InvariantCultureIgnoreCase) == true &&
                            !b.Remark.EndsWith("-利息", StringComparison.Ordinal))
                    .Remark;

            if (!all && !endDate.HasValue ||
                endDate.HasValue)
            {
                var filter = $"T1221 {content.Quotation('\'')} {rmk.Quotation('"')}";
                var filter0 = $"T1221 {content.Quotation('\'')} {(rmk + "-利息").Quotation('"')}";
                // ReSharper disable once PossibleInvalidOperationException
                var lastD = Accountant.RunVoucherQuery(filter0)
                        .OrderByDescending(v => v.Date, new DateComparer())
                        .FirstOrDefault()
                        ?.Date ??
                    Accountant.RunVoucherQuery(filter)
                        .OrderBy(v => v.Date, new DateComparer())
                        .First()
                        .Date.Value;
                var capQuery = $"{filter} [~{lastD.AsDate()}]``v";
                var intQuery = $"{filter0} [~{lastD.AsDate()}]``v";
                var capitalIntegral = Accountant.RunGroupedQuery(capQuery).Fund;
                var interestIntegral = Accountant.RunGroupedQuery(intQuery).Fund;
                Regularize(
                    content,
                    rmk,
                    rate,
                    ref capitalIntegral,
                    ref interestIntegral,
                    lastD,
                    endDate ?? DateTime.Today.CastUtc());
            }
            else
            {
                var capitalIntegral = 0D;
                var interestIntegral = 0D;
                Regularize(
                    content,
                    rmk,
                    rate,
                    ref capitalIntegral,
                    ref interestIntegral,
                    null,
                    DateTime.Today.CastUtc());
            }
            return new Succeed();
        }

        /// <summary>
        ///     从上次计息日后一日起计算单利利息并整理还款
        /// </summary>
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="rate">利率</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="interestIntegral">剩余利息</param>
        /// <param name="lastSettlement">上次计息日</param>
        /// <param name="finalDay">截止日期</param>
        private void Regularize(string content, string rmk, double rate, ref double capitalIntegral,
            ref double interestIntegral, DateTime? lastSettlement, DateTime finalDay)
        {
            var capitalPattern = new VoucherDetail
                {
                    Title = 1221,
                    Content = content,
                    Remark = rmk
                };
            var interestPattern = new VoucherDetail
                {
                    Title = 1221,
                    Content = content,
                    Remark = rmk + "-利息"
                };
            var rng = lastSettlement.HasValue
                ? new DateFilter(lastSettlement.Value.AddDays(1), finalDay)
                : new DateFilter(null, finalDay);
            foreach (var grp in
                Accountant
                    .RunVoucherQuery(
                        $"(T1221 {content.Quotation('\'')})*({rmk.Quotation('"')}+{(rmk + "-利息").Quotation('"')}) {rng.AsDateRange()}")
                    .GroupBy(v => v.Date)
                    .OrderBy(grp => grp.Key, new DateComparer()))
            {
                var key = grp.Key ?? throw new ApplicationException("无法处理无穷长时间以前的利息收入");

                if (!lastSettlement.HasValue)
                    lastSettlement = key;

                // Settle Interest
                interestIntegral += SettleInterest(
                    content,
                    rmk,
                    rate,
                    capitalIntegral,
                    key.Subtract(lastSettlement.Value).Days,
                    grp.SingleOrDefault(
                        v =>
                            v.Details.Any(d => d.IsMatch(interestPattern, 1)))
                    ?? new Voucher
                        {
                            Date = key,
                            Details = new List<VoucherDetail>()
                        });
                lastSettlement = key;

                // Settle Loan
                // ReSharper disable once PossibleInvalidOperationException
                capitalIntegral +=
                    grp.SelectMany(v => v.Details.Where(d => d.IsMatch(capitalPattern, 1)))
                        .Select(d => d.Fund.Value)
                        .Sum();

                // Settle Return
                foreach (
                    var voucher in
                    grp.Where(
                            v =>
                                v.Details.Any(d => d.IsMatch(capitalPattern, -1) || d.IsMatch(interestPattern, -1)))
                        .OrderBy(v => v.ID))
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var value =
                        -voucher.Details.Where(d => d.IsMatch(capitalPattern, -1) || d.IsMatch(interestPattern, -1))
                            .Select(d => d.Fund.Value)
                            .Sum();
                    if ((-value + interestIntegral).IsNonNegative())
                    {
                        RegularizeVoucherDetail(content, rmk, voucher, 0, value);
                        interestIntegral -= value;
                    }
                    else
                    {
                        RegularizeVoucherDetail(content, rmk, voucher, value - interestIntegral, interestIntegral);
                        capitalIntegral -= value - interestIntegral;
                        interestIntegral = 0;
                    }
                }
            }

            if (lastSettlement == null)
                throw new ApplicationException("无法处理无穷长时间以前的利息收入");

            if (lastSettlement != finalDay)
                interestIntegral += SettleInterest(
                    content,
                    rmk,
                    rate,
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
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="rate">利率</param>
        /// <param name="capitalIntegral">剩余本金</param>
        /// <param name="delta">间隔日数</param>
        /// <param name="voucher">记账凭证</param>
        /// <returns>利息</returns>
        private double SettleInterest(string content, string rmk, double rate,
            double capitalIntegral, int delta, Voucher voucher)
        {
            var interestPattern = new VoucherDetail
                {
                    Title = 1221,
                    Content = content,
                    Remark = rmk + "-利息"
                };
            var revenuePattern = new VoucherDetail
                {
                    Title = 6603,
                    SubTitle = 02,
                    Content = "贷款利息"
                };
            var interest = delta * rate * capitalIntegral;
            var create = new List<VoucherDetail>
                {
                    new VoucherDetail
                        {
                            Title = 1221,
                            Content = content,
                            Remark = rmk + "-利息",
                            Fund = interest
                        },
                    new VoucherDetail
                        {
                            Title = 6603,
                            SubTitle = 02,
                            Content = "贷款利息",
                            Fund = -interest
                        }
                };

            // ReSharper disable once PossibleInvalidOperationException
            var detail = voucher.Details.SingleOrDefault(d => d.IsMatch(interestPattern));
            if (detail == null)
            {
                voucher.Details = create;
                Accountant.Upsert(voucher);
            }
            // ReSharper disable once PossibleInvalidOperationException
            else if (!(detail.Fund.Value - interest).IsZero())
            {
                if (!voucher.Details.All(d => d.IsMatch(interestPattern) || d.IsMatch(revenuePattern)))
                    throw new ArgumentException("该记账凭证包含计息以外的细目", nameof(voucher));

                voucher.Details = create;
                Accountant.Upsert(voucher);
            }

            return interest;
        }

        /// <summary>
        ///     正确登记还款
        /// </summary>
        /// <param name="content">借款人</param>
        /// <param name="rmk">借款代码</param>
        /// <param name="voucher">记账凭证</param>
        /// <param name="capVol">本金还款额</param>
        /// <param name="intVol">利息还款额</param>
        private void RegularizeVoucherDetail(string content, string rmk, Voucher voucher, double capVol, double intVol)
        {
            var capitalPattern = new VoucherDetail
                {
                    Title = 1221,
                    Content = content,
                    Remark = rmk
                };
            var interestPattern = new VoucherDetail
                {
                    Title = 1221,
                    Content = content,
                    Remark = rmk + "-利息"
                };
            var flag = false;
            var capFlag = false;
            var intFlag = false;
            for (var i = 0; i < voucher.Details.Count; i++)
            {
                if (voucher.Details[i].IsMatch(capitalPattern, -1))
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

                if (voucher.Details[i].IsMatch(interestPattern, -1))
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
                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Title = 1221,
                            Content = content,
                            Remark = rmk,
                            Fund = -capVol
                        });
                flag = true;
            }
            if (!intFlag &&
                !intVol.IsZero())
            {
                voucher.Details.Add(
                    new VoucherDetail
                        {
                            Title = 1221,
                            Content = content,
                            Remark = rmk + "-利息",
                            Fund = -intVol
                        });
                flag = true;
            }
            if (flag)
                Accountant.Upsert(voucher);
        }
    }
}

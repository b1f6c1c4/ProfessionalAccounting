using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AccountingServer.Entities;

namespace AccountingServer.Shell.Plugins.THUInfo
{
    // ReSharper disable once UnusedMember.Global
    internal partial class THUInfo
    {
        /// <summary>
        ///     自动生成记账凭证
        /// </summary>
        /// <param name="records">交易记录</param>
        /// <param name="failed">无法生产记账凭证的交易记录</param>
        private IEnumerable<Voucher> AutoGenerate(IEnumerable<TransactionRecord> records,
            out List<TransactionRecord> failed)
        {
            var res = Enumerable.Empty<Voucher>();
            failed = new List<TransactionRecord>();
            foreach (var grp in records.GroupBy(r => r.Date))
            {
                List<Voucher> res0;
                List<TransactionRecord> lst;
                GenerateCommon(grp, out res0, out lst);
                res = res.Concat(res0);
            }

            return res;
        }

        /// <summary>
        ///     自动生成常规记账凭证
        /// </summary>
        /// <param name="recordsGroup">待处理交易记录组</param>
        /// <param name="result">生成的记账凭证</param>
        /// <param name="specialRecords">未能处理的交易记录</param>
        private void GenerateCommon(IGrouping<DateTime, TransactionRecord> recordsGroup, out List<Voucher> result,
            out List<TransactionRecord> specialRecords)
        {
            specialRecords = new List<TransactionRecord>();
            result = new List<Voucher>();
            foreach (var rec in recordsGroup)
            {
                var ep = Convert.ToInt32(rec.Endpoint);
                var res = Templates.Where(
                        t =>
                            (t.Type == null || t.Type == rec.Type) &&
                            (t.Ranges?.Any() != true || t.Ranges.Any(iv => iv.Start <= ep && iv.End >= ep)))
                    .ToList();

                if (res.Count == 1)
                {
                    var voucher = new Voucher
                        {
                            Date = recordsGroup.Key.Date,
                            Details = new List<VoucherDetail>()
                        };

                    var coeff = 0D;
                    foreach (var s in res[0].DetailString)
                    {
                        var d = Serializer.ParseVoucherDetail(s);
                        // ReSharper disable once PossibleInvalidOperationException
                        coeff -= d.Fund.Value;
                        d.Fund *= rec.Fund;
                        voucher.Details.Add(d);
                    }

                    voucher.Details.Add(
                        new VoucherDetail
                            {
                                Title = 1012,
                                SubTitle = 05,
                                Fund = coeff * rec.Fund,
                                Remark = rec.Index.ToString(CultureInfo.InvariantCulture)
                            });

                    result.Add(voucher);
                }
                else if (res.Count > 1)
                    throw new ApplicationException(
                        $"{ep} 匹配了多个模板：" + string.Join(" ", res.Select(t => t.DetailString.FirstOrDefault())));

                specialRecords.Add(rec);
            }
        }
    }
}

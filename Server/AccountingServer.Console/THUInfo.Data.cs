using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;

namespace AccountingServer.Console
{
    public partial class THUInfo
    {
        /// <summary>
        ///     对账
        /// </summary>
        /// <returns>有差异处</returns>
        public string Compare()
        {
            var sb = new StringBuilder();

            var account =
                m_Accountant.SelectVouchers(
                                            new VoucherQueryAtomBase(
                                                filter: new VoucherDetail { Title = 1012, SubTitle = 05 })).ToList();

            var groups =
                GetData().GroupBy(t => new Tuple<DateTime, string, string, double>(t.Item2, t.Item4, t.Item3, t.Item5));
            foreach (var group in groups)
            {
                List<Voucher> vouchers = null;
                switch (group.Key.Item2)
                {
                    case "消费":
                        switch (group.Key.Item3)
                        {
                            case "饮食中心":
                                vouchers = account.Where(
                                                         v =>
                                                         v.Date == group.Key.Item1 &&
                                                         (v.Details.Any(
                                                                        d =>
                                                                        d.Title == 6602 && d.SubTitle == 03) ||
                                                          v.Details.Any(
                                                                        d =>
                                                                        d.Title == 6602 && d.SubTitle == 06) ||
                                                          v.Details.Any(
                                                                        d =>
                                                                        d.Title == 6602 && d.SubTitle == 10) ||
                                                          v.Details.Any(
                                                                        d =>
                                                                        d.Title == 1221 && d.SubTitle == null)) &&
                                                         v.Details.Any(
                                                                       d =>
                                                                       d.Title == 1012 && d.SubTitle == 05 &&
                                                                       // ReSharper disable once PossibleInvalidOperationException
                                                                       Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                                       Accountant.Tolerance))
                                                  .ToList();
                                break;
                            case "紫荆服务楼超市":
                                vouchers = account.Where(
                                                         v =>
                                                         v.Date == group.Key.Item1 &&
                                                         v.Details.Any(
                                                                       d =>
                                                                       d.Title == 6602 && d.SubTitle == 06) &&
                                                         v.Details.Any(
                                                                       d =>
                                                                       d.Title == 1012 && d.SubTitle == 05 &&
                                                                       // ReSharper disable once PossibleInvalidOperationException
                                                                       Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                                       Accountant.Tolerance))
                                                  .ToList();
                                break;
                        }
                        break;
                    case "支付宝充值":
                    case "领取旧卡余额":
                        vouchers = account.Where(
                                                 v =>
                                                 v.Date == group.Key.Item1 &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1221 && d.SubTitle == null &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance) &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1012 && d.SubTitle == 05 &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance))
                                          .ToList();
                        break;
                    case "领取圈存":
                        vouchers = account.Where(
                                                 v =>
                                                 v.Date == group.Key.Item1 &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1002 && d.SubTitle == null &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance) &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1012 && d.SubTitle == 05 &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance))
                                          .ToList();
                        break;
                    case "自助缴费(学生公寓水费)":
                        vouchers = account.Where(
                                                 v =>
                                                 v.Date == group.Key.Item1 &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1123 && d.SubTitle == null &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance) &&
                                                 v.Details.Any(
                                                               d =>
                                                               d.Title == 1012 && d.SubTitle == 05 &&
                                                               // ReSharper disable once PossibleInvalidOperationException
                                                               Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                               Accountant.Tolerance))
                                          .ToList();
                        break;
                }
                if (vouchers != null &&
                    vouchers.Count > group.Count())
                {
                    foreach (var t in group)
                    {
                        sb.AppendFormat("++ {0}", t.Item1);
                        sb.AppendLine();
                    }
                    foreach (var voucher in vouchers)
                        sb.Append(CSharpHelper.PresentVoucher(voucher));
                    sb.AppendLine("---");
                }
                else if (vouchers == null ||
                         vouchers.Count < group.Count())
                {
                    var vouchers2 = account.Where(
                                                  v =>
                                                  v.Date == group.Key.Item1 &&
                                                  v.Details.Any(
                                                                d =>
                                                                d.Title == 1012 && d.SubTitle == 05 &&
                                                                // ReSharper disable once PossibleInvalidOperationException
                                                                Math.Abs(-d.Fund.Value - group.Key.Item4) <
                                                                Accountant.Tolerance))
                                           .ToList();
                    if (vouchers2.Count == group.Count())
                    {
                        if (
                            !vouchers2.All(
                                           v =>
                                           v.Remark != null &&
                                           v.Remark.Equals(
                                                           Voucher.ReconciliationMark,
                                                           StringComparison.OrdinalIgnoreCase)))
                            foreach (var t in group)
                            {
                                sb.AppendFormat("~~ {0}", t.Item1);
                                sb.AppendLine();
                            }
                        foreach (var voucher in vouchers2)
                            account.Remove(voucher);
                        continue;
                    }

                    foreach (var t in group)
                    {
                        sb.AppendFormat("** {0}", t.Item1);
                        sb.AppendLine();
                    }
                    foreach (var voucher in vouchers2)
                        sb.Append(CSharpHelper.PresentVoucher(voucher));
                    sb.AppendLine("---");
                }
                else
                    foreach (var voucher in vouchers)
                        account.Remove(voucher);
            }

            foreach (var voucher in account)
            {
                if (voucher.Remark != null &&
                    voucher.Remark.Equals(Voucher.ReconciliationMark, StringComparison.OrdinalIgnoreCase))
                    continue;
                sb.AppendLine("-- ??:");
                sb.Append(CSharpHelper.PresentVoucher(voucher));
                sb.AppendLine();
            }

            return sb.Length > 0 ? sb.ToString() : "OK";
        }

        /// <summary>
        ///     读取xls数据
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Tuple<string, DateTime, string, string, double>> GetData()
        {
            var strCon = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + m_FileName +
                         ";Extended Properties='Excel 8.0;IMEX=1'";
            var conn = new OleDbConnection(strCon);
            conn.Open();

            var cmd = new OleDbDataAdapter("SELECT * FROM [Sheet1$]", conn);
            var ds = new DataSet();

            cmd.Fill(ds, "[Sheet1$]");

            conn.Close();

            return from DataRow row in ds.Tables[0].Rows
                   where !row.IsNull(4) && !row.IsNull(5)
                   let odt = Convert.ToDateTime(row[4])
                   let dt = odt.Date
                   let fund = Convert.ToDouble(row[5])
                   let location = row[1].ToString()
                   let type = row[2].ToString()
                   select new Tuple<string, DateTime, string, string, double>(
                       String.Format(
                                     "@ {4:s}: #{0}{1}{2}{3} {5}",
                                     row[0].ToString().CPadRight(4),
                                     location.CPadRight(17),
                                     type.CPadRight(23),
                                     row[3].ToString().CPadLeft(9),
                                     odt,
                                     fund.AsCurrency().CPadLeft(11)),
                       dt,
                       location,
                       type,
                       fund);
        }
    }
}

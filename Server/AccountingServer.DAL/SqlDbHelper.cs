using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    public class SqlDbHelper : IDbHelper
    {
        private SqlConnection m_Conn;
        private readonly string m_ConnStr;
        private readonly string m_Schema;

        public SqlDbHelper(string un, string pw)
        {
            m_Schema = un;
            m_ConnStr =
                String.Format(
                              "Server=127.0.0.1;Database=Account;User ID={0};Password={1};MultipleActiveResultSets=true",
                              un,
                              pw);
        }

        private SqlConnection GetConn()
        {
            if (m_Conn == null)
            {
                m_Conn = new SqlConnection(m_ConnStr);
                m_Conn.Open();
            }
            return m_Conn;
        }

        public void Dispose()
        {
            if (m_Conn != null &&
                m_Conn.State == ConnectionState.Open)
            {
                m_Conn.Close();
                m_Conn.Dispose();
                m_Conn = null;
            }
        }

        private string ProcessText(string text) { return text.Replace("'", "''"); }

        private SqlCommand GetCmd(string sql) { return new SqlCommand(sql.Replace("[$]", m_Schema), GetConn()); }

        private object ExecuteScalar(string sql)
        {
            using (var cmd = GetCmd(sql))
                return cmd.ExecuteScalar();
        }

        private SqlDataReader ExecuteReader(string sql)
        {
            using (var cmd = GetCmd(sql))
                return cmd.ExecuteReader();
        }

        private int ExecuteNonQuery(string sql)
        {
            using (var cmd = GetCmd(sql))
                return cmd.ExecuteNonQuery();
        }

        private void ExecuteNonQueries(string sql)
        {
            var sp = sql.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var con = GetConn();
            foreach (var s in sp)
            {
                var cmd = new SqlCommand(s, con);
                cmd.ExecuteNonQuery();
            }
        }

        public Voucher SelectVoucher(string id) { throw new NotImplementedException(); }

        public IEnumerable<Voucher> SelectVouchers(Voucher filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ID, DT, Remark FROM Items WHERE 1=1");
            if (filter.Date.HasValue)
                sb.AppendFormat(" AND DT='{0:yyyyMMdd}'", filter.Date);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new Voucher
                            {
                                Date = reader.GetDateSafe(1),
                                Remark = reader.GetInt32Safe(0).ToString() //reader.GetStringSafe(2)
                            };
        }

        public IEnumerable<Voucher> SelectVouchers(Voucher filter, DateTime? startDate, DateTime? endDate)
        {
            throw new NotImplementedException();
        }

        public long SelectVouchersCount(Voucher filter) { throw new NotImplementedException(); }
        public bool InsertVoucher(Voucher entity) { throw new NotImplementedException(); }
        public bool DeleteVoucher(string id) { throw new NotImplementedException(); }
        public int DeleteVouchers(Voucher filter) { throw new NotImplementedException(); }
        public bool UpdateVoucher(Voucher entity) { throw new NotImplementedException(); }

        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Voucher> SelectVouchersWithDetail(VoucherDetail filter, DateTime? startDate,
                                                             DateTime? endDate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> dFilters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Voucher> SelectVouchersWithDetail(IEnumerable<VoucherDetail> filters, DateTime? startDate,
                                                             DateTime? endDate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT Item, Title, Fund, Remark FROM Details WHERE 1=1");
            if (filter.Title.HasValue)
                sb.AppendFormat(" AND Title={0:0000.00}", filter.Title);
            if (filter.Remark != null) // IMPORTANT
                sb.AppendFormat(" AND Item={0}", filter.Remark);
            if (filter.Fund.HasValue)
                sb.AppendFormat(" AND Fund={0:0.0000}", filter.Fund);

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                {
                    var title = reader.GetDecimalSafe(1).Value;
                    var subtitle = (int?)(100 * (title - (int)title));
                    if (subtitle == 0)
                        subtitle = null;
                    yield return
                        new VoucherDetail
                            {
                                Title = (int)title,
                                SubTitle = subtitle,
                                Fund = (double?)reader.GetDecimalSafe(2),
                                Content = reader.GetStringSafe(3)
                            };
                }
        }

        public IEnumerable<VoucherDetail> SelectDetails(VoucherDetail filter, DateTime? startDate, DateTime? endDate)
        {
            throw new NotImplementedException();
        }

        public long SelectDetailsCount(VoucherDetail filter) { throw new NotImplementedException(); }
        public bool InsertDetail(VoucherDetail entity) { throw new NotImplementedException(); }
        public int DeleteDetails(VoucherDetail filter) { throw new NotImplementedException(); }
        public void Shutdown() { throw new NotImplementedException(); }

        /*public IEnumerable<DbFixedAsset> SelectFixedAssets(DbFixedAsset filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM FixedAssets WHERE 1=1");
            if (filter.ID != null)
                sb.AppendFormat(" AND ID='{0}'", filter.ID);
            if (filter.Name != null)
                sb.AppendFormat(" AND Item={0}", filter.Name);
            if (filter.DT.HasValue)
                sb.AppendFormat(" AND DT='{0:yyyyMMdd}'", filter.DT);
            if (filter.Value.HasValue)
                sb.AppendFormat(" AND Value={0:0.0000}", filter.Value);
            if (filter.DepreciableLife.HasValue)
                sb.AppendFormat(" AND DepreciableLife={0:0}", filter.DepreciableLife);
            if (filter.Salvge.HasValue)
                sb.AppendFormat(" AND Salvge={0:0.0000}", filter.Salvge);
            if (filter.Title.HasValue)
                sb.AppendFormat(" AND Title={0:0000.00}", filter.Title);

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new DbFixedAsset
                        {
                            ID = reader.GetStringSafe(0),
                            Name = reader.GetStringSafe(1),
                            DT = reader.GetDateSafe(2),
                            Value = reader.GetDecimalSafe(4),
                            DepreciableLife = reader.GetInt32Safe(5),
                            Salvge = reader.GetDecimalSafe(6),
                            Title = reader.GetDecimalSafe(7)
                        };
        }*/
    }

    public static class SqlDbHelpExtension
    {
        public static Int16? GetInt16Safe(this SqlDataReader reader, int id)
        {
            if (reader.IsDBNull(id))
                return null;
            return reader.GetInt16(id);
        }

        public static int? GetInt32Safe(this SqlDataReader reader, int id)
        {
            if (reader.IsDBNull(id))
                return null;
            return reader.GetInt32(id);
        }

        public static decimal? GetDecimalSafe(this SqlDataReader reader, int id)
        {
            if (reader.IsDBNull(id))
                return null;
            return reader.GetDecimal(id);
        }

        public static string GetStringSafe(this SqlDataReader reader, int id)
        {
            if (reader.IsDBNull(id))
                return "";
            return reader.GetString(id);
        }

        public static DateTime? GetDateSafe(this SqlDataReader reader, int id)
        {
            if (reader.IsDBNull(id))
                return null;
            return reader.GetDateTime(id);
        }
    }
}

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
        private string m_Schema;

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
            var sp = sql.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            var con = GetConn();
            foreach (var s in sp)
            {
                var cmd = new SqlCommand(s, con);
                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<DbTitle> SelectTitles(DbTitle filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ID, Name, H_ID, TLevel FROM Titles WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0:0000.00}", filter.ID);
            if (filter.Name != null)
                sb.AppendFormat(" AND Name={0}", filter.Name);
            if (filter.H_ID.HasValue)
                sb.AppendFormat(" AND H_ID={0:0000.00}", filter.H_ID);
            if (filter.TLevel.HasValue)
                sb.AppendFormat(" AND TLevel={0:0}", filter.TLevel);

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new DbTitle
                            {
                                ID = reader.GetDecimalSafe(0),
                                Name = reader.GetStringSafe(1),
                                H_ID = reader.GetDecimalSafe(2),
                                TLevel = reader.GetInt16Safe(3)
                            };
        }

        public bool InsertTitle(DbTitle entity)
        {
            return ExecuteNonQuery(
                                   entity.H_ID.HasValue
                                       ? String.Format(
                                                       "INSERT INTO Titles VALUES ({0:0000.00},'{1}',{2:0000.00},{3:0})",
                                                       entity.ID,
                                                       ProcessText(entity.Name),
                                                       entity.H_ID,
                                                       entity.TLevel)
                                       : String.Format(
                                                       "INSERT INTO Titles VALUES ({0:0000.00},'{1}',NULL,{2:0})",
                                                       entity.ID,
                                                       ProcessText(entity.Name),
                                                       entity.TLevel)) == 1;
        }

        public int DeleteTitles(DbTitle filter)
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM Titles WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0:0000.00}", filter.ID);
            if (filter.Name != null)
                sb.AppendFormat(" AND Name={0}", filter.Name);
            if (filter.H_ID.HasValue)
                sb.AppendFormat(" AND H_ID={0:0000.00}", filter.H_ID);
            if (filter.TLevel.HasValue)
                sb.AppendFormat(" AND TLevel={0:0}", filter.TLevel);

            return ExecuteNonQuery(sb.ToString());
        }

        public IEnumerable<DbItem> SelectItems(DbItem filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ID, DT, Remark FROM Items WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0:0}", filter.ID);
            if (filter.DT.HasValue)
                sb.AppendFormat(" AND DT='{0:yyyyMMdd}'", filter.DT);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new DbItem
                            {
                                ID = reader.GetInt32Safe(0),
                                DT = reader.GetDateSafe(1),
                                Remark = reader.GetStringSafe(2)
                            };
        }

        public int SelectItemsCount(DbItem filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT COUNT(*) FROM Items WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0:0}", filter.ID);
            if (filter.DT.HasValue)
                sb.AppendFormat(" AND DT='{0:yyyyMMdd}'", filter.DT);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            return Convert.ToInt32(ExecuteScalar(sb.ToString()));
        }

        public bool InsertItem(DbItem entity)
        {
            var con = GetConn();
            var cmd = new SqlCommand(
                entity.ID.HasValue
                    ? (entity.DT.HasValue
                           ? String.Format("INSERT INTO Items VALUES ({0:0},'{1:yyyyMMdd}',", entity.ID, entity.DT)
                           : String.Format("INSERT INTO Items VALUES ({0:0},NULL,", entity.ID))
                      + (String.IsNullOrEmpty(entity.Remark) ? "NULL);" : String.Format("'{0}');", entity.Remark))
                    : (entity.DT.HasValue
                           ? String.Format("INSERT INTO Items(DT,Remark) VALUES ('{0:yyyyMMdd}',", entity.DT)
                           : String.Format("INSERT INTO Items(DT,Remark) VALUES (NULL,"))
                      +
                      (String.IsNullOrEmpty(entity.Remark)
                           ? "NULL);"
                           : String.Format("'{0}');", ProcessText(entity.Remark))),
                con);
            if (cmd.ExecuteNonQuery() != 1)
                return false;
            cmd.CommandText = "SELECT SCOPE_IDENTITY()";
            if (!entity.ID.HasValue)
                entity.ID = Convert.ToInt32(cmd.ExecuteScalar());
            return true;
        }

        public int DeleteItems(DbItem filter)
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM Items WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0:0}", filter.ID);
            if (filter.DT.HasValue)
                sb.AppendFormat(" AND DT='{0:yyyyMMdd}'", filter.DT);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            return ExecuteNonQuery(sb.ToString());
        }

        public IEnumerable<DbDetail> SelectDetails(DbDetail filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT Item, Title, Fund, Remark FROM Details WHERE 1=1");
            if (filter.Title.HasValue)
                sb.AppendFormat(" AND Title={0:0000.00}", filter.Title);
            if (filter.Item.HasValue)
                sb.AppendFormat(" AND Item={0:0}", filter.Item);
            if (filter.Fund.HasValue)
                sb.AppendFormat(" AND Fund={0:0.0000}", filter.Fund);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new DbDetail
                            {
                                Item = reader.GetInt32Safe(0),
                                Title = reader.GetDecimalSafe(1),
                                Fund = reader.GetDecimalSafe(2),
                                Remark = reader.GetStringSafe(3)
                            };
        }

        public int SelectDetailsCount(DbDetail filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT COUNT(*) FROM Details WHERE 1=1");
            if (filter.Title.HasValue)
                sb.AppendFormat(" AND Title={0:0000.00}", filter.Title);
            if (filter.Item.HasValue)
                sb.AppendFormat(" AND Item={0:0}", filter.Item);
            if (filter.Fund.HasValue)
                sb.AppendFormat(" AND Fund={0:0.0000}", filter.Fund);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            return Convert.ToInt32(ExecuteScalar(sb.ToString()));
        }

        public bool InsertDetail(DbDetail entity)
        {
            return ExecuteNonQuery(
                                   String.IsNullOrEmpty(entity.Remark)
                                       ? String.Format(
                                                       "INSERT INTO Details VALUES ({0:0},{1:0000.00},{2:0.0000},NULL)",
                                                       entity.Item,
                                                       entity.Title,
                                                       entity.Fund)
                                       : String.Format(
                                                       "INSERT INTO Details VALUES ({0:0},{1:0000.00},{2:0.0000},'{3}')",
                                                       entity.Item,
                                                       entity.Title,
                                                       entity.Fund,
                                                       ProcessText(entity.Remark))) == 1;
        }

        public int DeleteDetails(DbDetail filter)
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM Details WHERE 1=1");
            if (filter.Title.HasValue)
                sb.AppendFormat(" AND Title={0:0000.00}", filter.Title);
            if (filter.Item.HasValue)
                sb.AppendFormat(" AND Item={0:0}", filter.Item);
            if (filter.Fund.HasValue)
                sb.AppendFormat(" AND Fund={0:0.0000}", filter.Fund);
            if (filter.Remark != null)
                if (filter.Remark == "")
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", ProcessText(filter.Remark));

            return ExecuteNonQuery(sb.ToString());
        }

        public IEnumerable<DbFixedAsset> SelectFixedAssets(DbFixedAsset filter)
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
        }

        public bool InsertFixedAsset(DbFixedAsset entity)
        {
            return
                ExecuteNonQuery(
                                String.Format(
                                              "INSERT INTO FixedAssets VALUES ({0},{1},{2:yyyyMMdd},{3:0.0000},{4:0},{5:0.0000},{6:0000.00})",
                                              entity.ID,
                                              entity.Name,
                                              entity.DT,
                                              entity.Value,
                                              entity.DepreciableLife,
                                              entity.Salvge,
                                              entity.Title)) == 1;
        }


        public int DeleteFixedAssets(DbFixedAsset filter)
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM FixedAssets WHERE 1=1");
            if (filter.ID != null)
                sb.AppendFormat(" AND ID='{0}'", filter.ID);
            if (filter.Name != null)
                sb.AppendFormat(" AND Item='{0}'", ProcessText(filter.Name));
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

            return ExecuteNonQuery(sb.ToString());
        }

        public IEnumerable<DbShortcut> SelectShortcuts(DbShortcut filter)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM Shortcuts WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0}", filter.ID);
            if (filter.Name != null)
                if (filter.Name == "")
                    sb.Append(" AND Name IS NULL");
                else
                    sb.AppendFormat(" AND Name='{0}'", ProcessText(filter.Name));
            if (filter.Path != null)
                if (filter.Path == "")
                    sb.Append(" AND Path IS NULL");
                else
                    sb.AppendFormat(" AND Path='{0}'", ProcessText(filter.Path));

            using (var reader = ExecuteReader(sb.ToString()))
                while (reader.Read())
                    yield return
                        new DbShortcut
                            {
                                ID = reader.GetInt32Safe(0),
                                Name = reader.GetStringSafe(1),
                                Path = reader.GetStringSafe(2)
                            };
        }

        public bool InsertShortcuts(DbShortcut entity)
        {
            return ExecuteNonQuery(
                                   entity.ID.HasValue
                                       ? String.Format(
                                                       "INSERT INTO Shortcuts VALUES ({0},'{1}','{2}')",
                                                       entity.ID,
                                                       ProcessText(entity.Name),
                                                       ProcessText(entity.Path))
                                       : String.Format(
                                                       "INSERT INTO Shortcuts(Name,Path) VALUES ('{0}','{1}')",
                                                       ProcessText(entity.Name),
                                                       ProcessText(entity.Path))) == 1;
        }

        public int DeleteShortcuts(DbShortcut filter)
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM Shortcuts WHERE 1=1");
            if (filter.ID.HasValue)
                sb.AppendFormat(" AND ID={0}", filter.ID);
            if (filter.Name != null)
                if (filter.Name == "")
                    sb.Append(" AND Name IS NULL");
                else
                    sb.AppendFormat(" AND Name='{0}'", ProcessText(filter.Name));
            if (filter.Path != null)
                if (filter.Path == "")
                    sb.Append(" AND Path IS NULL");
                else
                    sb.AppendFormat(" AND Path='{0}'", ProcessText(filter.Path));

            return ExecuteNonQuery(sb.ToString());
        }

        public void DeltaItemsFrom(int id)
        {
            ExecuteNonQuery(
                            String.Format(
                                          "UPDATE Details SET Item=Item+1 WHERE Item>={0};",
                                          id));
            var maxid = Convert.ToInt32(ExecuteScalar("SELECT MAX(ID) FROM Items"));
            for (var i = maxid; i >= id; i--)
                ExecuteNonQuery(String.Format("UPDATE Items SET ID=ID+1 WHERE ID={0}", i));

            /*ExecuteNonQuery(
                            String.Format(
                                          "UPDATE t2 SET t2.DT=t1.DT, t2.Remark=t1.Remark FROM Items AS t1 INNER JOIN Items AS t2 ON t1.ID+1=t2.ID WHERE t1.ID>={0}",
                                          id));*/
            /*ExecuteNonQuery(
                            String.Format(
                                          "",
                                          id));*/
        }

        public IEnumerable<DbDetail> GetXBalances(DbDetail filter, bool noCarry = false, int? sID = null, int? eID = null, int dir = 0)
        {
            if (filter.Remark != null)
            {
                var sql = String.Format("SELECT SUM(Fund) FROM Details WHERE Remark='{0}'", ProcessText(filter.Remark));
                if (filter.Title.HasValue)
                    sql += String.Format(" AND Title={0:0000.00}", filter.Title);
                if (noCarry)
                    sql += String.Format(" AND [$].IsCarry(Item)=0");
                if (sID.HasValue)
                    sql += String.Format(" AND Item>={0:0}", sID);
                if (eID.HasValue)
                    sql += String.Format(" AND Item<={0:0}", eID);
                if (dir > 0)
                    sql += " AND Fund > 0";
                else if (dir > 0)
                    sql += " AND Fund < 0";
                var res = ExecuteScalar(sql);
                var xb = res == DBNull.Value ? 0 : Convert.ToDecimal(res);
                yield return new DbDetail {Title = filter.Title, Remark = filter.Remark, Fund = xb};
                yield break;
            }
            if (filter.Title.HasValue)
            {
                var sql = String.Format(
                                        "SELECT Remark, SUM(Fund) FROM Details WHERE Title={0:0000.00}",
                                        filter.Title);
                if (noCarry)
                    sql += String.Format(" AND [$].IsCarry(Item)=0");
                if (sID.HasValue)
                    sql += String.Format(" AND Item>={0:0}", sID);
                if (eID.HasValue)
                    sql += String.Format(" AND Item<={0:0}", eID);
                if (dir > 0)
                    sql += " AND Fund > 0";
                else if (dir > 0)
                    sql += " AND Fund < 0";
                sql += " GROUP BY Title,Remark";
                using (var reader = ExecuteReader(sql))
                {
                    while (reader.Read())
                        yield return
                            new DbDetail
                                {
                                    Title = filter.Title,
                                    Remark = reader.GetStringSafe(0),
                                    Fund = reader.GetDecimalSafe(1)
                                };
                    yield break;
                }
            }
            {
                var sql = "SELECT Title,Remark, SUM(Fund) FROM Details WHERE 1=1";
                if (noCarry)
                    sql += String.Format(" AND [$].IsCarry(Item)=0");
                if (sID.HasValue)
                    sql += String.Format(" AND Item>={0:0}", sID);
                if (eID.HasValue)
                    sql += String.Format(" AND Item<={0:0}", eID);
                if (dir > 0)
                    sql += " AND Fund > 0";
                else if (dir > 0)
                    sql += " AND Fund < 0";
                using (
                    var reader = ExecuteReader(sql))
                    while (reader.Read())
                        yield return
                            new DbDetail
                            {
                                Title = reader.GetDecimalSafe(0),
                                Remark = reader.GetStringSafe(1),
                                Fund = reader.GetDecimalSafe(2)
                            };
            }
        }

        public void Depreciate() { ExecuteNonQuery("EXEC Depreciate_All"); }
        public void Carry() { ExecuteNonQuery("EXEC Carry_All"); }

        public IEnumerable<DailyBalance> GetDailyBalance(decimal title, string remark, int dir = 0)
        {
            if (remark != null)
            {
                var sql =
                    String.Format(
                                  "SELECT DT, SUM(Fund) FROM Details INNER JOIN Items ON Details.Item=Items.ID WHERE Title={0:0000.00}",
                                  title);
                if (remark == String.Empty)
                    sql += " AND Details.Remark IS NULL";
                else
                    sql += String.Format(" AND Details.Remark='{0}'", ProcessText(remark));
                if (dir != 0)
                    sql += dir > 0 ? " AND Fund>0" : " AND Fund<0";
                sql += " GROUP BY DT ORDER BY DT";
                using (var reader = ExecuteReader(sql))
                    while (reader.Read())
                        yield return new DailyBalance {DT = reader.GetDateSafe(0), Balance = reader.GetDecimal(1)};
            }
            else
            {
                var sql =
                    String.Format(
                                  "SELECT DT, SUM(Fund) FROM Details INNER JOIN Items ON Details.Item=Items.ID WHERE Title={0:0000.00}",
                                  title);
                if (dir != 0)
                    sql += dir > 0 ? " AND Fund>0" : " AND Fund<0";
                sql += " GROUP BY DT ORDER BY DT";
                using (var reader = ExecuteReader(sql))
                    while (reader.Read())
                        yield return new DailyBalance {DT = reader.GetDateSafe(0), Balance = reader.GetDecimal(1)};
            }
        }

        public IEnumerable<DailyBalance> GetDailyXBalance(decimal title, int dir = 0)
        {
            var sql =
                String.Format(
                              "SELECT DT, Remark, SUM(Fund) FROM Details INNER JOIN Items ON Details.Item=Items.ID WHERE Title={0:0000.00}",
                              title);
            if (dir != 0)
                sql += dir > 0 ? " AND Fund>0" : " AND Fund<0";
            sql += " GROUP BY DT, Details.Remark ORDER BY DT, Details.Remark";
            using (var reader = ExecuteReader(sql))
                while (reader.Read())
                    yield return
                        new DailyBalance
                            {
                                DT = reader.GetDateSafe(0),
                                Remark = reader.GetStringSafe(1),
                                Balance = reader.GetDecimal(2)
                            };
        }

        public int GetIDFromDate(DateTime date)
        {
            var sql =
                String.Format(
                              "SELECT t2.ID FROM Items AS t1 INNER JOIN Items AS t2 ON t1.ID+1=t2.ID WHERE t2.DT>='{0:yyyyMMdd}' AND t1.DT<'{0:yyyyMMdd}'",
                              date);
            var res=ExecuteScalar(sql);
            if (res != null)
                return (int)res;
            if (
                (int)
                ExecuteScalar(String.Format("SELECT COUNT(*) FROM Items AS t WHERE t.DT>='{0:yyyyMMdd}'", date)) == 0)
                return 1+
                    (int)
                    ExecuteScalar(
                                  "SELECT t1.ID FROM Items AS t1 LEFT OUTER JOIN Items AS t2 ON t1.ID+1=t2.ID WHERE t2.ID IS NULL");
            return 1+(int)
                    ExecuteScalar(
                                  "SELECT t2.ID FROM Items AS t1 INNER JOIN Items AS t2 ON t1.ID+1=t2.ID WHERE t1.DT IS NULL");
        }

        public string GetFixedAssetName(Guid id)
        {
            return (string)ExecuteScalar(String.Format("SELECT Name FROM FixedAssets WHERE ID='{0}'", id));
        }

        /*public decimal GetBalance(decimal title)
        {
            return (decimal)ExecuteScalar("SELECT SUM(FUND) FROM Details WHERE Title=" + title);
        }
        public decimal GetBalance(decimal title, string remark)
        {
            return
                (decimal)
                ExecuteScalar(
                              "SELECT SUM(FUND) FROM Details WHERE Title=" + title +
                              (remark == null ? " AND Remark IS NULL" : " AND Remark=" + remark.Replace("'", "''")));
        }
        public decimal GetBalance(decimal[] titles, string[] rmks, int[] ids)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT SUM(FUND) FROM Details WHERE 1=1");
            if (titles!=null && titles.Any())
            {
                sb.Append(" AND Title IN (");
                foreach (var title in titles)
                    sb.AppendFormat("{0:0000.00},", title);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            if (rmks != null && rmks.Any())
            {
                sb.Append(rmks.Contains(null) ? " AND Title Is NULL OR Title IN (" : " AND Title IN (");
                foreach (var rmk in rmks.Where(rmk => rmk!=null))
                    sb.AppendFormat("'{0}',", rmk.Replace("'","''"));
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            if (ids != null && ids.Any())
            {
                sb.Append(" AND Item IN (");
                foreach (var id in ids)
                    sb.AppendFormat("{0:0},", id);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            return (decimal)ExecuteScalar(sb.ToString());
        }

        public List<DbTitle> GetTitles()
        {
            var lst = new List<DbTitle>();
            using (var reader = ExecuteReader("SELECT * FROM Titles"))
            {
                while (reader.Read())
                {
                    lst.Add(
                            new DbTitle
                                {
                                    ID = reader.GetDecimal(0),
                                    Name = reader.GetString(1),
                                    H_ID = reader.IsDBNull(2) ? null : (decimal?)reader.GetDecimal(2),
                                    TLevel = reader.GetInt32(3)
                                });
                }
            }
            lst.ForEach(t => t.H_Title = lst.SingleOrDefault(tx => tx.ID == t.H_ID));
            lst.ForEach(t=>t.UpdateBalance());
            return lst;
        }

        public List<DbItem> GetItems(int? lIDBound,int? uIDBound,DateTime? lDTBound,DateTime? uDTBound,bool isDTNull,bool isRmkEnabled,string remark)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM Items WHERE 1=1");
            if (lIDBound.HasValue)
                sb.AppendFormat(" AND ID>={0:0}", lIDBound);
            if (uIDBound.HasValue)
                sb.AppendFormat(" AND ID<={0:0}", uIDBound);
            if (isDTNull)
            {
                if (lDTBound.HasValue ||
                    uDTBound.HasValue)
                {
                    sb.Append(" AND (DT IS NULL OR 1=1");

                    if (lDTBound.HasValue)
                        sb.AppendFormat(" AND DT>={0:0}", lIDBound);
                    if (uDTBound.HasValue)
                        sb.AppendFormat(" AND DT<={0:0}", uIDBound);

                    sb.Append(")");
                }
                else
                {
                    sb.Append(" AND DT IS NULL");
                }
            }
            else
            {
                if (lDTBound.HasValue)
                    sb.AppendFormat(" AND DT>={0:0}", lIDBound);
                if (uDTBound.HasValue)
                    sb.AppendFormat(" AND DT<={0:0}", uIDBound);
            }

            if (isRmkEnabled)
            {
                if (String.IsNullOrEmpty(remark))
                    sb.Append(" AND Remark IS NULL");
                else
                    sb.AppendFormat(" AND Remark='{0}'", remark);
            }

            var lst = new List<DbItem>();
            using (var reader=ExecuteReader(sb.ToString()))
            {
                while (reader.Read())
                {
                    lst.Add(
                            new DbItem
                                {
                                    ID = reader.GetInt32(0),
                                    DT =
                                        reader.IsDBNull(1)
                                            ? null
                                            : (DateTime?)DateTime.ParseExact(reader.GetString(1), "yyyyMMdd", null),
                                    Remark = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    m_Details = GetDetails(reader.GetInt32(0))
                                });
                }
            }
            return lst;
        }

        private List<DbDetail> GetDetails(string sql)
        {
            var lst = new List<DbDetail>();
            using (var reader = ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    lst.Add(
                            new DbDetail
                                {
                                    Item_ID = reader.GetInt32(0),
                                    Title_ID = reader.GetDecimal(1),
                                    Fund = reader.GetDecimal(2),
                                    Remark = reader.IsDBNull(3) ? null : reader.GetString(3)
                                });
                }
            }
            return lst;
        }
        public List<DbDetail> GetDetails(int id)
        {
            return GetDetails("SELECT * FROM Details WHERE Item=" + id);
        }
        public List<DbDetail> GetDetails(decimal[] titles, string[] rmks, int[] ids)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM Details WHERE 1=1");
            if (titles!=null && titles.Any())
            {
                sb.Append(" AND Title IN (");
                foreach (var title in titles)
                    sb.AppendFormat("{0:0000.00},", title);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            if (rmks != null && rmks.Any())
            {
                sb.Append(rmks.Contains(null) ? " AND Title Is NULL OR Title IN (" : " AND Title IN (");
                foreach (var rmk in rmks.Where(rmk => rmk!=null))
                    sb.AppendFormat("'{0}',", rmk.Replace("'","''"));
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            if (ids != null && ids.Any())
            {
                sb.Append(" AND Item IN (");
                foreach (var id in ids)
                    sb.AppendFormat("{0:0},", id);
                sb.Remove(sb.Length - 1, 1);
                sb.Append(")");
            }
            return GetDetails(sb.ToString());
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

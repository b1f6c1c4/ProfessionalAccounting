using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using AccountingServer.Entities;

namespace AccountingServer
{
    public partial class THUInfo
    {
        public string Compare()
        {
            var sb = new StringBuilder();

            //var account = m_Accountant.SelectVouchersWithDetail(new VoucherDetail { Title = 1012, SubTitle = 05 });
            //var accountAux = m_Accountant.SelectVouchersWithDetail(new VoucherDetail { Title = 1221, Content = @"学生卡待领" });
            
            var fetch = GetData();
            foreach (DataRow row in fetch.Rows)
            {
                sb.AppendFormat(
                                "{0}{1}{2}{3}{4}{5:yyyyMMdd}",
                                row[0],
                                row[1],
                                row[2],
                                row[3],
                                row[4],
                                row[5]);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private DataTable GetData()
        {
            var strCon = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + m_FileName + ";Extended Properties='Excel 8.0;IMEX=1'";
            var conn = new OleDbConnection(strCon);
            conn.Open();
            
            var cmd = new OleDbDataAdapter("SELECT * FROM [Sheet1$]", conn);
            var ds = new DataSet();

            cmd.Fill(ds, "[Sheet1$]");

            conn.Close();

            return ds.Tables[0];
        }
    }
}

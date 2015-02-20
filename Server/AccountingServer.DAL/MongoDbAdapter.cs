using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    public class MongoDbAdapter : IDbAdapter, IDbServer
    {
        #region member

        /// <summary>
        ///     MongoDb客户端
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoClient m_Client;

        /// <summary>
        ///     MongoDb服务器
        /// </summary>
        private MongoServer m_Server;

        /// <summary>
        ///     MongoDb数据库
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoDatabase m_Db;

        /// <summary>
        ///     记账凭证集合
        /// </summary>
        private MongoCollection<Voucher> m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private MongoCollection<Asset> m_Assets;

        /// <summary>
        ///     摊销集合
        /// </summary>
        private MongoCollection<Amortization> m_Amortizations;

        #endregion

        public MongoDbAdapter() { Connected = false; }

        static MongoDbAdapter()
        {
            BsonSerializer.RegisterSerializer(typeof(Voucher), new VoucherSerializer());
            BsonSerializer.RegisterSerializer(typeof(VoucherDetail), new VoucherDetailSerializer());
            BsonSerializer.RegisterSerializer(typeof(Asset), new AssetSerializer());
            BsonSerializer.RegisterSerializer(typeof(AssetItem), new AssetItemSerializer());
            BsonSerializer.RegisterSerializer(typeof(Amortization), new AmortizationSerializer());
            BsonSerializer.RegisterSerializer(typeof(AmortItem), new AmortItemSerializer());
            BsonSerializer.RegisterSerializer(typeof(Balance), new BalanceSerializer());
        }

        #region server

        /// <summary>
        ///     是否已经连接到数据库
        /// </summary>
        public bool Connected { get; private set; }

        public void Launch()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongod --config \"C:\\Users\\b1f6c1c4\\Documents\\tjzh\\Account\\mongod.conf\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        public void Connect()
        {
            m_Client = new MongoClient("mongodb://localhost");
            m_Server = m_Client.GetServer();

            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");

            Connected = true;
        }

        public void Disconnect()
        {
            if (!Connected)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;
            m_Amortizations = null;

            m_Server.Disconnect();
            m_Server = null;
            m_Client = null;

            Connected = false;
        }

        public void Backup()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongodump -h 127.0.0.1 -d accounting -o \"C:\\Users\\b1f6c1c4\\OneDrive\\Backup\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        #endregion

        #region voucher

        public Voucher SelectVoucher(string id) { return m_Vouchers.FindOneById(ObjectId.Parse(id)); }

        public IEnumerable<Voucher> FilteredSelect(IVoucherQuery query)
        {
            return m_Vouchers.Find(MongoDbQueryHelper.GetQuery(query));
        }

        public IEnumerable<VoucherDetail> FilteredSelectDetails(IVoucherQuery query)
        {
            return m_Vouchers.Find(MongoDbQueryHelper.GetQuery(query))
                             .SelectMany(v => v.Details)
                             .Where(d => d.IsMatch(query.DetailFilter));
        }

        public IEnumerable<Balance> FilteredGroup(IGroupingQuery query)
        {
            var level = query.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);
            if (query.AggrType != AggregationType.None)
                level |= SubtotalLevel.Day;

            var emit = GetEmitJavascript(level);

            var sbMap = new StringBuilder();
            sbMap.AppendLine("function() {");

            // vfilter
            sbMap.Append("    var vfilter = ");
            sbMap.Append(MongoDbQueryHelper.GetJavascriptFilter(query.VoucherFilter));
            sbMap.AppendLine(";");
            sbMap.AppendLine("    if (!vfilter(this)) return;");

            // rng
            sbMap.Append("    var rng = ");
            sbMap.Append(MongoDbQueryHelper.GetJavascriptFilter(query.Range));
            sbMap.AppendLine(";");
            sbMap.AppendLine("    if (!rng(this)) return;");

            // filters
            sbMap.AppendLine("    var chk = ");
            sbMap.Append(MongoDbQueryHelper.GetJavascriptFilter(query.DetailFilter));
            sbMap.AppendLine(";");

            if (query.All)
            {
                sbMap.AppendLine("    var i = 0;");
                sbMap.AppendLine("    for (i = 0;i < this.detail.length;i++)");
                sbMap.AppendLine("        if (chk(this.detail[i]))");
                sbMap.AppendLine("            break;");
                sbMap.AppendLine("    if (i >= this.detail.length");
                sbMap.AppendLine("        return;");
            }

            sbMap.Append(GetTheDateJavascript(level));
            sbMap.AppendLine("    this.detail.forEach(function(d) {");

            if (!query.All)
                sbMap.AppendLine("    if (chk(d))");
            sbMap.AppendLine(emit);
            sbMap.AppendLine("    });");

            sbMap.AppendLine("}");
            const string reduce =
                "function(key, values) { var total = 0; for (var i = 0; i < values.length; i++) total += values[i].balance; return { balance: total };}";
            var args = new MapReduceArgs
                           {
                               MapFunction = new BsonJavaScript(sbMap.ToString()),
                               ReduceFunction = new BsonJavaScript(reduce)
                           };
            var res = m_Vouchers.MapReduce(args);
            return res.GetResultsAs<Balance>();
        }


        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.Remove(MongoDbQueryHelper.GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(IVoucherQuery query) { throw new NotImplementedException(); }

        public bool Upsert(Voucher entity)
        {
            var res = m_Vouchers.Save(entity);
            return res.DocumentsAffected <= 1;
        }

        #endregion

        #region asset

        public Asset SelectAsset(Guid id) { return m_Assets.FindOne(MongoDbQueryHelper.GetUniqueQuery(id)); }

        public IEnumerable<Asset> FilteredSelect(Asset filter)
        {
            return m_Assets.Find(MongoDbQueryHelper.GetQuery(filter));
        }

        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.Remove(MongoDbQueryHelper.GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public bool Upsert(Asset entity)
        {
            var res = m_Assets.Save(entity);
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(Asset filter)
        {
            var res = m_Assets.Remove(MongoDbQueryHelper.GetQuery(filter));
            return res.DocumentsAffected;
        }

        #endregion

        #region amortization

        public Amortization SelectAmortization(Guid id)
        {
            return m_Amortizations.FindOne(MongoDbQueryHelper.GetUniqueQuery(id));
        }

        public IEnumerable<Amortization> FilteredSelect(Amortization filter)
        {
            return m_Amortizations.Find(MongoDbQueryHelper.GetQuery(filter));
        }

        public bool DeleteAmortization(Guid id)
        {
            var res = m_Amortizations.Remove(MongoDbQueryHelper.GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public bool Upsert(Amortization entity)
        {
            var res = m_Amortizations.Save(entity);
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(Amortization filter)
        {
            var res = m_Amortizations.Remove(MongoDbQueryHelper.GetQuery(filter));
            return res.DocumentsAffected;
        }

        #endregion

        #region javascript

        private static string GetTheDateJavascript(SubtotalLevel subtotalLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    var theDate = this.date;");
            if (!subtotalLevel.HasFlag(SubtotalLevel.Week))
                return sb.ToString();
            sb.AppendLine("    theDate.setHours(0);");
            sb.AppendLine("    theDate.setMinutes(0);");
            sb.AppendLine("    theDate.setSeconds(0);");
            sb.AppendLine();
            if (subtotalLevel.HasFlag(SubtotalLevel.Year))
                sb.AppendLine("    theDate.setMonth(0, 1);");
            else if (subtotalLevel.HasFlag(SubtotalLevel.Month))
                sb.AppendLine("    theDate.setDate(1);");
            else if (subtotalLevel.HasFlag(SubtotalLevel.BillingMonth))
            {
                sb.AppendLine("    if (theDate.getDate() >= 9) {");
                sb.AppendLine("        theDate.setDate(9);");
                sb.AppendLine("        if (theDate.getMonth() == 11) {");
                sb.AppendLine("            theDate.setMonth(0);");
                sb.AppendLine("            theDate.setFullYear(theDate.getFullYear() + 1);");
                sb.AppendLine("        } else {");
                sb.AppendLine("            theDate.setMonth(theDate.getMonth() + 1);");
                sb.AppendLine("        }");
                sb.AppendLine("    } else {");
                sb.AppendLine("        theDate.setDate(9);");
                sb.AppendLine("    }");
            }
            else if (subtotalLevel.HasFlag(SubtotalLevel.FinancialMonth))
            {
                sb.AppendLine("    if (theDate.getDate() >= 20) {");
                sb.AppendLine("        theDate.setDate(19);");
                sb.AppendLine("        if (theDate.getMonth() == 11) {");
                sb.AppendLine("            theDate.setMonth(0);");
                sb.AppendLine("            theDate.setFullYear(theDate.getFullYear() + 1);");
                sb.AppendLine("        } else {");
                sb.AppendLine("            theDate.setMonth(theDate.getMonth() + 1);");
                sb.AppendLine("        }");
                sb.AppendLine("    } else {");
                sb.AppendLine("        theDate.setDate(19);");
                sb.AppendLine("    }");
            }
            else // if (subtotalLevel.HasFlag(SubtotalLevel.Week))
            {
                sb.AppendLine("    if (theDate.getDay() == 0)");
                sb.AppendLine("        theDate.setDate(theDate.getDate() - 6);");
                sb.AppendLine("    else");
                sb.AppendLine("        theDate.setDate(theDate.getDate() + 1 - theDate.getDay());");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private static string GetEmitJavascript(SubtotalLevel subtotalLevel)
        {
            var sb = new StringBuilder();
            sb.Append("emit({");
            if (subtotalLevel.HasFlag(SubtotalLevel.Title))
                sb.Append("title: d.title,");
            if (subtotalLevel.HasFlag(SubtotalLevel.SubTitle))
                sb.Append("subtitle: d.subtitle,");
            if (subtotalLevel.HasFlag(SubtotalLevel.Content))
                sb.Append("content: d.content,");
            if (subtotalLevel.HasFlag(SubtotalLevel.Remark))
                sb.Append("remark: d.remark,");
            if (subtotalLevel.HasFlag(SubtotalLevel.Day))
                sb.Append("date: theDate,");
            sb.Append("}, { balance: d.fund });");
            var emit = sb.ToString();
            return emit;
        }

        #endregion
    }
}

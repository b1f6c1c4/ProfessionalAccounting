using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                                   VoucherDetail filter = null,
                                                   DateFilter? rng = null,
                                                   int dir = 0)
        {
            return m_Vouchers.Find(MongoDbQueryHelper.GetQuery(vfilter, filter, rng, dir));
        }


        public IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                                   IEnumerable<VoucherDetail> filters = null,
                                                   DateFilter? rng = null,
                                                   int dir = 0,
                                                   bool useAnd = false)
        {
            return m_Vouchers.Find(MongoDbQueryHelper.GetQuery(vfilter, filters, rng, dir, useAnd));
        }

        public IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                                VoucherDetail filter = null,
                                                                DateFilter? rng = null,
                                                                int dir = 0)
        {
            var res = m_Vouchers.Find(MongoDbQueryHelper.GetQuery(vfilter, filter, rng, dir));

            return from voucher in res
                   from detail in voucher.Details
                   where detail.IsMatch(filter)
                   where dir == 0 || (dir > 0 ? detail.Fund > 0 : detail.Fund < 0)
                   select detail;
        }

        public IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                                IEnumerable<VoucherDetail> filters = null,
                                                                DateFilter? rng = null,
                                                                int dir = 0,
                                                                bool useAnd = false)
        {
            var res = m_Vouchers.Find(MongoDbQueryHelper.GetQuery(vfilter, filters, rng, dir, useAnd));

            return from voucher in res
                   from detail in voucher.Details
                   where detail.IsMatch(filters, useAnd)
                   where dir == 0 || (dir > 0 ? detail.Fund > 0 : detail.Fund < 0)
                   select detail;
        }

        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.Remove(MongoDbQueryHelper.GetUniqueQuery(id));
            return res.DocumentsAffected == 1;
        }

        public long FilteredDelete(Voucher vfilter, VoucherDetail filter, DateFilter? rng)
        {
            var res = m_Vouchers.Remove(MongoDbQueryHelper.GetQuery(vfilter, filter, rng));
            return res.DocumentsAffected;
        }

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
    }
}

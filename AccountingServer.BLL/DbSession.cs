using AccountingServer.DAL;

namespace AccountingServer.BLL
{
    internal class DbSession
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        public IDbAdapter Db { get; private set; }

        /// <summary>
        ///     获取是否已经连接到数据库
        /// </summary>
        public bool Connected => Db != null;

        public void Connect(string uri) => Db = Facade.Create(uri);
    }
}

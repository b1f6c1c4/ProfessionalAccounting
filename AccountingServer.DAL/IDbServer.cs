namespace AccountingServer.DAL
{
    /// <summary>
    ///     数据库服务器接口
    /// </summary>
    public interface IDbServer
    {
        /// <summary>
        ///     启动服务器
        /// </summary>
        void Launch();

        /// <summary>
        ///     备份数据库
        /// </summary>
        void Backup();
    }
}

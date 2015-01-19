namespace AccountingServer.Console
{
    public partial class AccountingConsole
    {
        /// <summary>
        ///     检查是否已连接；若未连接，尝试连接
        /// </summary>
        private void AutoConnect()
        {
            if (!m_Accountant.Connected)
            {
                m_Accountant.Launch();
                m_Accountant.Connect();
            }
        }

        /// <summary>
        ///     连接数据库服务器
        /// </summary>
        /// <returns>连接情况</returns>
        private IQueryResult ConnectServer()
        {
            m_Accountant.Connect();
            return new Suceed();
        }

        /// <summary>
        ///     启动数据库服务器
        /// </summary>
        /// <returns>启动情况</returns>
        private IQueryResult LaunchServer()
        {
            m_Accountant.Launch();
            return new Suceed();
        }

        /// <summary>
        ///     备份数据库
        /// </summary>
        /// <returns>备份情况</returns>
        private IQueryResult Backup()
        {
            m_Accountant.Backup();
            return new Suceed();
        }
    }
}

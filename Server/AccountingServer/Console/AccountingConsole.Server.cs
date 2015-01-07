using System;
using System.Diagnostics;

namespace AccountingServer.Console
{
    internal partial class AccountingConsole
    {
        /// <summary>
        ///     关闭数据库服务器
        /// </summary>
        /// <returns>关闭情况</returns>
        private string ShutdownServer()
        {
            try
            {
                m_Accountant.Shutdown();
                m_Accountant.Disconnect();
                return "OK";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        /// <summary>
        ///     连接数据库服务器
        /// </summary>
        /// <returns>连接情况</returns>
        private string ConnectServer()
        {
            try
            {
                m_Accountant.Connect();
                return "OK";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        /// <summary>
        ///     启动数据库服务器
        /// </summary>
        /// <returns>启动情况</returns>
        private string LaunchServer()
        {
            try
            {
                m_Accountant.Launch();
                return "OK";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}

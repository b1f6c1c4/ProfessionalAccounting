using System;
using System.Diagnostics;

namespace AccountingServer
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
        private static string LaunchServer()
        {
            try
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

                return String.Format("OK {0}", process.Id);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}

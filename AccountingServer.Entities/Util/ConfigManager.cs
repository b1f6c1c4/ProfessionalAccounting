using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AccountingServer.Entities.Util
{
    /// <summary>
    ///     可重载的配置文件管理
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        ///     读取配置文件
        /// </summary>
        /// <param name="throws">允许抛出异常</param>
        void Reload(bool throws);
    }

    /// <summary>
    ///     元配置文件管理
    /// </summary>
    public static class MetaConfigManager
    {
        /// <summary>
        ///     所有配置文件
        /// </summary>
        private static readonly List<IConfigManager> ConfigManagers = new List<IConfigManager>();

        /// <summary>
        ///     添加配置文件
        /// </summary>
        /// <param name="manager">配置文件管理</param>
        public static void Register(IConfigManager manager) => ConfigManagers.Add(manager);

        /// <summary>
        ///     重新读取配置文件
        /// </summary>
        public static long ReloadAll()
        {
            foreach (var manager in ConfigManagers)
                manager.Reload(true);

            return ConfigManagers.Count;
        }
    }

    /// <summary>
    ///     配置文件管理
    /// </summary>
    /// <typeparam name="T">配置文件类型</typeparam>
    public interface IConfigManager<out T> : IConfigManager
    {
        /// <summary>
        ///     配置文件
        /// </summary>
        T Config { get; }
    }

    /// <summary>
    ///     配置文件管理器
    /// </summary>
    /// <typeparam name="T">配置文件类型</typeparam>
    public class ConfigManager<T> : IConfigManager<T>
    {
        /// <summary>
        ///     文件名
        /// </summary>
        private readonly string m_FileName;

        /// <summary>
        ///     配置文件
        /// </summary>
        private T m_Config;

        /// <summary>
        ///     读取配置文件时发生的错误
        /// </summary>
        private Exception m_Exception;

        /// <summary>
        ///     设置配置文件
        /// </summary>
        /// <param name="filename">文件名</param>
        public ConfigManager(string filename)
        {
            m_FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.d", filename);
            Reload(false);
            MetaConfigManager.Register(this);
        }

        /// <inheritdoc />
        public T Config
        {
            get
            {
                if (m_Exception != null)
                    throw new MemberAccessException($"加载配置文件{m_FileName}时发生错误", m_Exception);

                return m_Config;
            }
        }

        /// <inheritdoc />
        public void Reload(bool throws)
        {
            try
            {
                var ser = new XmlSerializer(typeof(T));
                using (var stream = new StreamReader(m_FileName))
                    m_Config = (T)ser.Deserialize(stream);
            }
            catch (Exception e)
            {
                if (throws)
                    throw;

                m_Exception = e;
            }
        }
    }
}

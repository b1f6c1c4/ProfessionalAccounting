﻿using System;
using System.IO;
using System.Xml.Serialization;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     配置文件管理器
    /// </summary>
    /// <typeparam name="T">配置文件类型</typeparam>
    public class CustomManager<T>
    {
        /// <summary>
        ///     文件名
        /// </summary>
        private readonly string m_FileName;

        /// <summary>
        ///     配置文件
        /// </summary>
        private readonly T m_Config;

        /// <summary>
        ///     读取配置文件时发生的错误
        /// </summary>
        private readonly Exception m_Exception;

        /// <summary>
        ///     读取配置文件；若发生错误，保存在<c>m_Exception</c>中
        /// </summary>
        /// <param name="filename">文件名</param>
        public CustomManager(string filename)
        {
            m_FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);

            try
            {
                var ser = new XmlSerializer(typeof(T));
                using (var stream = new StreamReader(m_FileName))
                    m_Config = (T)ser.Deserialize(stream);
            }
            catch (Exception e)
            {
                m_Exception = e;
            }
        }

        /// <summary>
        ///     配置文件
        /// </summary>
        public T Config
        {
            get
            {
                if (m_Exception != null)
                    throw new MemberAccessException($"加载配置文件{m_FileName}时发生错误", m_Exception);

                return m_Config;
            }
        }
    }
}
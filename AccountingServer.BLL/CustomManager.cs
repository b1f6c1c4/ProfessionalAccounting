using System;
using System.IO;
using System.Xml.Serialization;

namespace AccountingServer.BLL
{
    public class CustomManager<T>
    {
        private readonly string m_FileName;

        private readonly T m_Config;

        private readonly Exception m_Exception;

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

        public T Config
        {
            get
            {
                if (m_Exception != null)
                    throw new MethodAccessException($"加载配置文件{m_FileName}时发生错误", m_Exception);

                return m_Config;
            }
        }
    }
}

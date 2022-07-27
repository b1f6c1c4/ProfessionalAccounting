/* Copyright (C) 2020-2022 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AccountingServer.Entities.Util;

/// <summary>
///     配置文件管理
/// </summary>
public interface IConfigManager
{
    /// <summary>
    ///     读取配置文件
    /// </summary>
    /// <param name="throws">允许抛出异常</param>
    Task Reload(bool throws);
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
///     配置文件管理器工厂
/// </summary>
/// <typeparam name="T">配置文件类型</typeparam>
public interface IConfigManagerFactory
{
    IConfigManager<T> Generate<T>(string filename);
}

/// <summary>
///     元配置文件管理
/// </summary>
public static class MetaConfigManager
{
    /// <summary>
    ///     所有配置文件
    /// </summary>
    private static readonly List<IConfigManager> ConfigManagers = new();

    /// <summary>
    ///     默认配置文件管理器工厂
    /// </summary>
    public static IConfigManagerFactory DefaultFactory { private get; set; } = new FSConfigManagerFactory();

    public static IConfigManager<T> Generate<T>(string filename)
    {
        var icm = DefaultFactory.Generate<T>(filename);
        Register(icm);
        return icm;
    }

    /// <summary>
    ///     添加配置文件
    /// </summary>
    /// <param name="manager">配置文件管理</param>
    private static void Register(IConfigManager manager) => ConfigManagers.Add(manager);

    /// <summary>
    ///     重新读取配置文件
    /// </summary>
    public static async IAsyncEnumerable<string> ReloadAll()
    {
        var tasks = ConfigManagers.Select((m) => (m, m.Reload(true))).ToList();
        foreach (var (m, t) in tasks)
        {
            await t;
            yield return m.GetType() + "\n";
        }
    }
}

/// <summary>
///     配置文件管理器
/// </summary>
/// <typeparam name="T">配置文件类型</typeparam>
public abstract class XmlConfigManager<T> : IConfigManager<T>
{
    /// <summary>
    ///     资源标识
    /// </summary>
    protected string Identifier { private get; set; }

    /// <summary>
    ///     配置文件
    /// </summary>
    private T m_Config;

    /// <summary>
    ///     读取配置文件时发生的错误
    /// </summary>
    private Exception m_Exception;

    /// <inheritdoc />
    public T Config
    {
        get
        {
            if (m_Exception != null)
                throw new MemberAccessException($"加载配置文件{Identifier}时发生错误", m_Exception);

            return m_Config;
        }
    }

    /// <summary>
    ///     读取配置文件
    /// </summary>
    protected abstract Task<StreamReader> Retrieve();

    /// <inheritdoc />
    public async Task Reload(bool throws)
    {
        try
        {
            var ser = new XmlSerializer(typeof(T));
            using var stream = await Retrieve();
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

/// <summary>
///     基于文件系统的配置文件管理器工厂
/// </summary>
public class FSConfigManagerFactory : IConfigManagerFactory
{
    public IConfigManager<T> Generate<T>(string filename)
        => new FSConfigManager<T>(filename);
}

/// <summary>
///     基于文件系统的配置文件管理器
/// </summary>
internal class FSConfigManager<T> : XmlConfigManager<T>
{
    /// <summary>
    ///     文件名
    /// </summary>
    private readonly string m_FileName;

    /// <summary>
    ///     设置配置文件
    /// </summary>
    /// <param name="filename">文件名</param>
    internal FSConfigManager(string filename)
    {
        m_FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.d", filename);
        Identifier = m_FileName;
        Reload(false);
    }

    /// <inheritdoc />
    protected override async Task<StreamReader> Retrieve()
        => new(m_FileName);
}

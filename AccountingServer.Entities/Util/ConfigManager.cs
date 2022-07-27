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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AccountingServer.Entities.Util;

/// <summary>
///     配置文件管理
/// </summary>
public static class Cfg
{
    private static readonly ReaderWriterLockSlim m_Lock = new();

    private static readonly Dictionary<string, StreamReader> m_ConfigStreamsMap = new();

    private static readonly Dictionary<Type, string> m_ConfigTypesMap = new();

    private static readonly Dictionary<Type, dynamic> m_ConfigsMap = new();

    private static T ReadLocked<T>(Func<T> func)
    {
        m_Lock.EnterReadLock();
        try
        {
            return func();
        }
        finally
        {
            m_Lock.ExitReadLock();
        }
    }

    private static T WriteLocked<T>(Func<T> func)
    {
        m_Lock.EnterWriteLock();
        try
        {
            return func();
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }

    public static Func<string, Task<StreamReader>> DefaultLoader;

    public static void RegisterType<T>(string filename)
        => WriteLocked(() => {
                m_ConfigTypesMap[typeof(T)] = filename;
                return Nothing.AtAll;
            });

    public static async Task RegisterName(string filename)
    {
        var stream = await DefaultLoader(filename);
        m_Lock.EnterWriteLock();
        try
        {
            m_ConfigStreamsMap[filename] = stream;
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }

    public static T Get<T>()
    {
        var (stream, obj) = ReadLocked(() => {
            StreamReader stream = null;
            if (m_ConfigsMap.ContainsKey(typeof(T)))
                return (stream, m_ConfigsMap[typeof(T)]);
            var fn = m_ConfigTypesMap[typeof(T)];
            stream = m_ConfigStreamsMap[fn];
            return (stream, null);
        });

        if (stream == null)
            return obj;

        obj = new XmlSerializer(typeof(T)).Deserialize(stream);

        return WriteLocked(() => {
                m_ConfigsMap[typeof(T)] = obj;
                return (T)obj;
            });
    }

    public static void Assign<T>(T obj)
        => WriteLocked(() => {
                m_ConfigsMap[typeof(T)] = obj;
                return Nothing.AtAll;
            });

    /// <summary>
    ///     重新读取配置文件
    /// </summary>
    public static async IAsyncEnumerable<string> ReloadAll()
    {
        m_Lock.EnterWriteLock();
        try {
            m_ConfigsMap.Clear();
            var fns = new List<string>();
            foreach (var (k, v) in m_ConfigStreamsMap)
                fns.Add(k);
            m_ConfigStreamsMap.Clear();
            foreach (var (m, t) in fns.Select((fn) => (fn, DefaultLoader(fn))).ToList())
            {
                await t;
                yield return $"Loaded config file {m}\n";
            }
        }
        finally
        {
            m_Lock.ExitWriteLock();
        }
    }
}

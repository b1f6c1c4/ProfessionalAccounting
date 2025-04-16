/* Copyright (C) 2020-2025 b1f6c1c4
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AccountingServer.Entities.Util;

/// <summary>
///     配置文件管理
/// </summary>
public static class Cfg
{
    private static readonly ReaderWriterLockSlim Lock = new();

    private static readonly Dictionary<string, StreamReader> ConfigStreamsMap = new();

    private static readonly Dictionary<Type, string> ConfigTypesMap = new();

    private static readonly Dictionary<Type, dynamic> ConfigsMap = new();

    public static Func<string, Task<StreamReader>> DefaultLoader;

    private static T ReadLocked<T>(Func<T> func)
    {
        Lock.EnterReadLock();
        try
        {
            return func();
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    private static T WriteLocked<T>(Func<T> func)
    {
        Lock.EnterWriteLock();
        try
        {
            return func();
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public static void RegisterType<T>(string filename)
        => WriteLocked(() =>
            {
                ConfigTypesMap[typeof(T)] = filename;
                return Nothing.AtAll;
            });

    public static async Task RegisterName(string filename)
    {
        var stream = await DefaultLoader(filename);
        Lock.EnterWriteLock();
        try
        {
            ConfigStreamsMap[filename] = stream;
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    public static T Get<T>()
    {
        var (stream, obj) = ReadLocked(static () =>
            {
                if (ConfigsMap.ContainsKey(typeof(T)))
                    return ((StreamReader)null, ConfigsMap[typeof(T)]);

                var fn = ConfigTypesMap[typeof(T)];
                var stream = ConfigStreamsMap[fn];
                return (stream, null);
            });

        if (stream == null)
            return obj;

        obj = new XmlSerializer(typeof(T)).Deserialize(stream);

        return WriteLocked(() =>
            {
                ConfigsMap[typeof(T)] = obj;
                return (T)obj;
            });
    }

    public static void Assign<T>(T obj)
        => WriteLocked(() =>
            {
                ConfigsMap[typeof(T)] = obj;
                return Nothing.AtAll;
            });

    /// <summary>
    ///     重新读取配置文件
    /// </summary>
    public static async IAsyncEnumerable<string> ReloadAll()
    {
        Lock.EnterWriteLock();
        try
        {
            ConfigsMap.Clear();
            var fns = new List<string>();
            foreach (var (k, _) in ConfigStreamsMap)
                fns.Add(k);
            ConfigStreamsMap.Clear();
            foreach (var (m, t) in fns.Select(static fn => (fn, DefaultLoader(fn))).ToList())
            {
                ConfigStreamsMap[m] = await t;
                yield return $"Loaded config file {m}\n";
            }

            foreach (var (t, m) in ConfigsMap)
                yield return $"{m} is bound to {t.FullName}";
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }
}

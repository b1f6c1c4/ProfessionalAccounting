/* Copyright (C) 2020-2021 b1f6c1c4
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

using System.IO;
using System.Resources;

namespace AccountingServer.Shell.Plugins;

/// <summary>
///     插件基类
/// </summary>
internal abstract class PluginBase
{
    /// <summary>
    ///     执行插件表达式
    /// </summary>
    /// <param name="expr">表达式</param>
    /// <param name="session">客户端会话</param>
    /// <returns>执行结果</returns>
    public abstract IQueryResult Execute(string expr, Session session);

    /// <summary>
    ///     显示插件帮助
    /// </summary>
    /// <returns>帮助内容</returns>
    public virtual string ListHelp()
    {
        var type = GetType();
        var resName = $"{type.Namespace}.Resources.Document.txt";
        using var stream = type.Assembly.GetManifestResourceStream(resName);
        if (stream == null)
            throw new MissingManifestResourceException();

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

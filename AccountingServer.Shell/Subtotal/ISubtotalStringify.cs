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

using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal;

/// <summary>
///     分类汇总结果序列化
/// </summary>
internal interface ISubtotalStringify
{
    /// <summary>
    ///     执行分类汇总
    /// </summary>
    /// <param name="raw">分类汇总结果</param>
    /// <param name="par">参数</param>
    /// <param name="serializer">表示器</param>
    /// <param name="client">客户端</param>
    /// <returns>分类汇总结果</returns>
    string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer, Client client);
}

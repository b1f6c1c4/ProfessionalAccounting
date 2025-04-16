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
using System.Linq;
using AccountingServer.Entities.Util;

namespace AccountingServer.Shell.Plugins.YieldRate;

/// <summary>
///     收益率求解器
/// </summary>
internal class YieldRateSolver
{
    /// <summary>
    ///     日期
    /// </summary>
    private readonly double[] m_Delta;

    /// <summary>
    ///     现金流
    /// </summary>
    private readonly double[] m_Fund;

    /// <summary>
    ///     期数
    /// </summary>
    private readonly int m_N;

    public YieldRateSolver(IEnumerable<double> delta, IEnumerable<double> fund)
    {
        m_Delta = delta.ToArray();
        m_Fund = fund.ToArray();
        m_N = m_Delta.Length;
        if (m_Fund.Length != m_N)
            throw new ArgumentException("数组大小不匹配");
    }

    /// <summary>
    ///     试算净值
    /// </summary>
    /// <param name="b">收益率+1</param>
    /// <param name="val">净值</param>
    /// <param name="der">净值对收益率的偏导数</param>
    private void Value(double b, out double val, out double der)
    {
        val = 0D;
        der = 0D;
        for (var i = 0; i < m_N; i++)
        {
            var v = Math.Pow(b, m_Delta[i] - 1);
            val += v * b * m_Fund[i];
            der += v * m_Delta[i] * m_Fund[i];
        }
    }

    /// <summary>
    ///     采用牛顿下山迭代法求解收益率
    /// </summary>
    /// <returns>收益率</returns>
    public double Solve()
    {
        var lambda = 1D; // learning rate

        var credit = 1000000; // avoid dead loop
        while (credit-- > 0)
        {
            var b = 1D; // the searching variable

            var dir = 0; // search direction
            while (credit-- > 0)
            {
                Value(b, out var v, out var d);
                if (v.IsZero())
                    return b - 1;

                var del = v / d; // Newton
                if (dir > 0)
                {
                    if (del < 0)
                        break;
                }
                else if (dir < 0)
                {
                    if (del > 0)
                        break;
                }
                else
                    dir = del > 0 ? 1 : -1;

                b -= lambda * del;
            }

            lambda /= 2;
        }

        return double.NaN;
    }
}

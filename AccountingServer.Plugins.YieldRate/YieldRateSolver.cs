using System;
using AccountingServer.BLL;

namespace AccountingServer.Plugins.YieldRate
{
    /// <summary>
    ///     收益率求解器
    /// </summary>
    internal class YieldRateSolver
    {
        /// <summary>
        ///     期数
        /// </summary>
        private readonly int m_N;

        /// <summary>
        ///     日期
        /// </summary>
        private readonly double[] m_Delta;

        /// <summary>
        ///     现金流
        /// </summary>
        private readonly double[] m_Fund;

        public YieldRateSolver(double[] delta, double[] fund)
        {
            m_Delta = delta;
            m_Fund = fund;
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
            var lambda = 1D;

            while (true)
            {
                var b = 1D;

                var dir = 0;
                var flag = false;
                while (true)
                {
                    double v, d;
                    Value(b, out v, out d);
                    if (v.IsZero())
                    {
                        flag = true;
                        break;
                    }

                    var del = v / d;
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
                if (flag)
                    return b - 1;
                lambda /= 2;
            }
        }
    }
}

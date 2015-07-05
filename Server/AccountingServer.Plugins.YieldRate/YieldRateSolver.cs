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
        /// <returns>净值</returns>
        private double Value(double b)
        {
            var aggr = 0D;
            for (var i = 0; i < m_N; i++)
                aggr += Math.Pow(b, m_Delta[i]) * m_Fund[i];
            return aggr;
        }

        /// <summary>
        ///     试算净值对收益率的偏导数
        /// </summary>
        /// <param name="b">收益率+1</param>
        /// <returns>净值对收益率的偏导数</returns>
        private double Derivative(double b)
        {
            var aggr = 0D;
            for (var i = 0; i < m_N; i++)
                aggr += Math.Pow(b, m_Delta[i] - 1) * m_Delta[i] * m_Fund[i];
            return aggr;
        }

        /// <summary>
        ///     采用牛顿迭代法求解收益率
        /// </summary>
        /// <returns>收益率</returns>
        public double Solve()
        {
            var b = 1D;

            while (true)
            {
                var v = Value(b);
                if (v.IsZero())
                    break;

                var d = Derivative(b);
                b -= v / d;
            }
            return b - 1;
        }
    }
}

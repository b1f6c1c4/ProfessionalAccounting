using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using AccountingServer.Console.Chart;

namespace AccountingServer.Console
{
    /// <summary>
    ///     表达式执行结果
    /// </summary>
    public interface IQueryResult
    {
        /// <summary>
        ///     是否要连续输入
        /// </summary>
        bool AutoReturn { get; }
    }

    /// <summary>
    ///     更新或插入执行结果
    /// </summary>
    public interface IUpsertResult { }

    /// <summary>
    ///     成功
    /// </summary>
    public class Suceed : IQueryResult
    {
        public override string ToString() { return "OK"; }

        /// <inheritdoc />
        public bool AutoReturn { get { return true; } }
    }

    /// <summary>
    ///     失败
    /// </summary>
    public class Failed : IQueryResult, IUpsertResult
    {
        /// <summary>
        ///     异常
        /// </summary>
        private readonly Exception m_Exception;

        public Failed(Exception exception) { m_Exception = exception; }

        public override string ToString() { return m_Exception.ToString(); }

        /// <inheritdoc />
        public bool AutoReturn { get { return true; } }
    }

    public class NumberAffected : IQueryResult, IUpsertResult
    {
        /// <summary>
        ///     文档数
        /// </summary>
        private readonly long m_N;

        /// <summary>
        ///     文档数
        /// </summary>
        /// <param name="n"></param>
        public NumberAffected(long n) { m_N = n; }

        public override string ToString() { return m_N.ToString(CultureInfo.InvariantCulture); }

        /// <inheritdoc />
        public bool AutoReturn { get { return true; } }
    }

    /// <summary>
    ///     文本
    /// </summary>
    public abstract class Text : IQueryResult
    {
        /// <summary>
        ///     文字内容
        /// </summary>
        private readonly string m_Text;

        protected Text(string text) { m_Text = text; }

        public override string ToString() { return m_Text; }

        /// <inheritdoc />
        public abstract bool AutoReturn { get; }
    }

    /// <summary>
    ///     可编辑文本
    /// </summary>
    public class EditableText : Text, IUpsertResult
    {
        public EditableText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool AutoReturn { get { return false; } }
    }

    /// <summary>
    ///     不可编辑文本
    /// </summary>
    public class UnEditableText : Text
    {
        public UnEditableText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool AutoReturn { get { return true; } }
    }

    /// <summary>
    ///     图标数据
    /// </summary>
    public class ChartData : IQueryResult
    {
        public ChartData(AccountingChart chart)
        {
            ChartAreas = new List<ChartArea>();
            Series = new List<Series>();
            ChartAreas.Add(chart.Setup());
            foreach (var series in chart.GatherAsset())
                Series.Add(series);
        }

        public ChartData(IEnumerable<AccountingChart> accountingCharts)
        {
            ChartAreas = new List<ChartArea>();
            Series = new List<Series>();
            foreach (var chart in accountingCharts)
            {
                ChartAreas.Add(chart.Setup());
                foreach (var series in chart.GatherAsset())
                    Series.Add(series);
            }
        }

        /// <summary>
        ///     系列
        /// </summary>
        public IList<Series> Series { get; private set; }

        /// <summary>
        ///     图表区域
        /// </summary>
        public IList<ChartArea> ChartAreas { get; private set; }

        /// <inheritdoc />
        public bool AutoReturn { get { return true; } }
    }
}

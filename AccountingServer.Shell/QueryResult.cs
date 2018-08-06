using System.Globalization;

namespace AccountingServer.Shell
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

        /// <summary>
        ///     是否需要刷新
        /// </summary>
        bool Dirty { get; }
    }

    /// <summary>
    ///     成功
    /// </summary>
    internal class PlainSucceed : IQueryResult
    {
        /// <inheritdoc />
        public bool AutoReturn => true;

        /// <inheritdoc />
        public bool Dirty => false;

        public override string ToString() => "OK";
    }

    /// <summary>
    ///     脏成功
    /// </summary>
    internal class DirtySucceed : IQueryResult
    {
        /// <inheritdoc />
        public bool AutoReturn => true;

        /// <inheritdoc />
        public bool Dirty => true;

        public override string ToString() => "Done";
    }

    internal class NumberAffected : IQueryResult
    {
        /// <summary>
        ///     文档数
        /// </summary>
        private readonly long m_N;

        /// <summary>
        ///     文档数
        /// </summary>
        /// <param name="n"></param>
        public NumberAffected(long n) => m_N = n;

        /// <inheritdoc />
        public bool AutoReturn => true;

        /// <inheritdoc />
        public bool Dirty => m_N != 0;

        public override string ToString() => m_N.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     文本
    /// </summary>
    internal abstract class Text : IQueryResult
    {
        /// <summary>
        ///     文字内容
        /// </summary>
        private readonly string m_Text;

        /// <summary>
        ///     文字内容
        /// </summary>
        protected Text(string text) => m_Text = text;

        /// <inheritdoc />
        public abstract bool Dirty { get; }

        /// <inheritdoc />
        public abstract bool AutoReturn { get; }

        public override string ToString() => m_Text;
    }

    /// <summary>
    ///     普通文本
    /// </summary>
    internal class PlainText : Text
    {
        public PlainText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool Dirty => false;

        /// <inheritdoc />
        public override bool AutoReturn => false;
    }

    /// <summary>
    ///     脏文本
    /// </summary>
    internal class DirtyText : Text
    {
        public DirtyText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool Dirty => true;

        /// <inheritdoc />
        public override bool AutoReturn => true;
    }
}

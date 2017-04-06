using System.Globalization;

namespace AccountingServer.Shell.Util
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
    ///     成功
    /// </summary>
    internal class Succeed : IQueryResult
    {
        public override string ToString() => "OK";

        /// <inheritdoc />
        public bool AutoReturn => true;
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

        public override string ToString() => m_N.ToString(CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public bool AutoReturn => true;
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

        public override string ToString() => m_Text;

        /// <inheritdoc />
        public abstract bool AutoReturn { get; }
    }

    /// <summary>
    ///     可编辑文本
    /// </summary>
    internal class EditableText : Text
    {
        public EditableText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool AutoReturn => false;
    }

    /// <summary>
    ///     不可编辑文本
    /// </summary>
    internal class UnEditableText : Text
    {
        public UnEditableText(string text) : base(text) { }

        /// <inheritdoc />
        public override bool AutoReturn => true;
    }
}

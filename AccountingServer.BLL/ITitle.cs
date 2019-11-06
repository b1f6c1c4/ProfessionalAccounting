namespace AccountingServer.BLL
{
    /// <summary>
    ///     会计科目
    /// </summary>
    public interface ITitle
    {
        /// <summary>
        ///     会计科目一级科目代码
        /// </summary>
        int Title { get; }

        /// <summary>
        ///     会计科目二级科目代码，若为<c>null</c>表示无二级科目
        /// </summary>
        int? SubTitle { get; }
    }
}

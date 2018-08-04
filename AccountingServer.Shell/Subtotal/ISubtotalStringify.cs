using AccountingServer.Entities;

namespace AccountingServer.Shell.Subtotal
{
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
        /// <returns>分类汇总结果</returns>
        string PresentSubtotal(ISubtotalResult raw, ISubtotal par);
    }
}

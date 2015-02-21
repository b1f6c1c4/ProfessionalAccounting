using System;

namespace AccountingServer.Entities
{
    public interface IDistributed
    {
        /// <summary>
        ///     编号
        /// </summary>
        Guid? ID { get; set; }

        /// <summary>
        ///     名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        string Remark { get; set; }
    }
}

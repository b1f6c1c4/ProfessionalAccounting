namespace AccountingServer.DAL
{
    /// <summary>
    ///     ���ݿ�������ӿ�
    /// </summary>
    public interface IDbServer
    {
        /// <summary>
        ///     ����������
        /// </summary>
        void Launch();

        /// <summary>
        ///     �������ݿ�
        /// </summary>
        void Backup();
    }
}

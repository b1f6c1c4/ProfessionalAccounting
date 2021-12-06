using System;
using System.Collections.Generic;
using AccountingServer.Entities;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     ���ݿ���ʽӿ�
    /// </summary>
    public interface IDbAdapter
    {
        #region server

        /// <summary>
        ///     �Ƿ��Ѿ����ӵ����ݿ�
        /// </summary>
        bool Connected { get; }

        /// <summary>
        ///     ���ӷ�����
        /// </summary>
        void Connect();

        /// <summary>
        ///     �Ͽ�������
        /// </summary>
        void Disconnect();

        #endregion

        #region voucher

        /// <summary>
        ///     ����Ų��Ҽ���ƾ֤
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>����ƾ֤�����û����Ϊ<c>null</c></returns>
        Voucher SelectVoucher(string id);

        /// <summary>
        ///     ������ʽ���Ҽ���ƾ֤
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <returns>��һϸĿƥ�����ʽ�ļ���ƾ֤</returns>
        IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query);

        /// <summary>
        ///     ������ʽ����ϸĿ
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <returns>ƥ�����ʽ��ϸĿ</returns>
        IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query);

        /// <summary>
        ///     ������ʽִ�з������
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <returns>������ܽ��</returns>
        IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query);

        /// <summary>
        ///     �����ɾ������ƾ֤
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool DeleteVoucher(string id);

        /// <summary>
        ///     ������ʽɾ������ƾ֤
        /// </summary>
        /// <param name="query">����ʽ</param>
        /// <returns>��ɾ����ϸĿ����</returns>
        long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query);

        /// <summary>
        ///     ��ӻ��滻����ƾ֤
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">�¼���ƾ֤</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Voucher entity);

        #endregion

        #region asset

        /// <summary>
        ///     ����Ų����ʲ�
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�ʲ������û����Ϊ<c>null</c></returns>
        Asset SelectAsset(Guid id);

        /// <summary>
        ///     �������������ʲ�
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>ƥ����������ʲ�</returns>
        IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     �����ɾ���ʲ�
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool DeleteAsset(Guid id);

        /// <summary>
        ///     ��������ɾ���ʲ�
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>��ɾ�����ʲ�����</returns>
        long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     ��ӻ��滻�ʲ�
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">���ʲ�</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Asset entity);

        #endregion

        #region amortization

        /// <summary>
        ///     ����Ų���̯��
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>̯�������û����Ϊ<c>null</c></returns>
        Amortization SelectAmortization(Guid id);

        /// <summary>
        ///     ������������̯��
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>ƥ���������̯��</returns>
        IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     �����ɾ��̯��
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool DeleteAmortization(Guid id);

        /// <summary>
        ///     ��������ɾ��̯��
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>��ɾ����̯������</returns>
        long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter);

        /// <summary>
        ///     ��ӻ��滻̯��
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">��̯��</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Amortization entity);

        #endregion
    }
}

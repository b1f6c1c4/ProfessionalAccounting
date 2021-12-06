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

        /// <summary>
        ///     ����Ų��Ҽ���ƾ֤
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>����ƾ֤�����û����Ϊ<c>null</c></returns>
        Voucher SelectVoucher(string id);

        /// <summary>
        ///     �����������Ҽ���ƾ֤
        /// </summary>
        /// <param name="filter">������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>ƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher filter, DateFilter rng);

        /// <summary>
        ///     �����ɾ������ƾ֤
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool DeleteVoucher(string id);

        /// <summary>
        ///     ��������ɾ������ƾ֤
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>��ɾ���ļ���ƾ֤����</returns>
        long FilteredDelete(Voucher filter);

        /// <summary>
        ///     ��ӻ��滻����ƾ֤
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">�¼���ƾ֤</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Voucher entity);

        /// <summary>
        ///     ��ϸĿ���������Ҽ���ƾ֤
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>��һϸĿƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     ����������ϸĿ���������Ҽ���ƾ֤
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>��һϸĿƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher vfilter, VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     ��ϸĿ���������Ҽ���ƾ֤
        /// </summary>
        /// <param name="filters">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>��һϸĿƥ����һ�������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters, DateFilter rng);

        /// <summary>
        ///     ��ϸĿ����������ϸĿ
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>ƥ���������ϸĿ</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter, DateFilter rng);

        /// <summary>
        ///     ��������ɾ������ƾ֤
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>��ɾ����ϸĿ����</returns>
        long FilteredDelete(Voucher vfilter, VoucherDetail filter);

        /// <summary>
        ///     ��������ɾ��ϸĿ
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>��ɾ����ϸĿ����</returns>
        long FilteredDelete(VoucherDetail filter);


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
        IEnumerable<Asset> FilteredSelect(Asset filter);

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
        long FilteredDelete(Asset filter);

        /// <summary>
        ///     ��ӻ��滻�ʲ�
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">���ʲ�</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Asset entity);
    }
}

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
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>��һϸĿƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                            VoucherDetail filter = null,
                                            DateFilter? rng = null);

        /// <summary>
        ///     �����������Ҽ���ƾ֤
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filters">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <param name="useAnd">��ϸĿ������֮��Ĺ�ϵΪ��ȡ</param>
        /// <returns>��һϸĿƥ����һ�������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher vfilter = null,
                                            IEnumerable<VoucherDetail> filters = null,
                                            DateFilter? rng = null,
                                            bool useAnd = false);

        /// <summary>
        ///     ������������ϸĿ
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>ƥ���������ϸĿ</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                         VoucherDetail filter = null,
                                                         DateFilter? rng = null);

        /// <summary>
        ///     ������������ϸĿ
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filters">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <param name="useAnd">��ϸĿ������֮��Ĺ�ϵΪ��ȡ</param>
        /// <returns>ƥ���������ϸĿ</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(Voucher vfilter = null,
                                                         IEnumerable<VoucherDetail> filters = null,
                                                         DateFilter? rng = null,
                                                         bool useAnd = false);

        /// <summary>
        ///     �����ɾ������ƾ֤
        /// </summary>
        /// <param name="id">���</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool DeleteVoucher(string id);

        /// <summary>
        ///     ��������ɾ������ƾ֤
        /// </summary>
        /// <param name="vfilter">������</param>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="rng">���ڹ�����</param>
        /// <returns>��ɾ����ϸĿ����</returns>
        long FilteredDelete(Voucher vfilter = null,
                            VoucherDetail filter = null,
                            DateFilter? rng = null);

        /// <summary>
        ///     ��ӻ��滻����ƾ֤
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">�¼���ƾ֤</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Voucher entity);


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
        IEnumerable<Amortization> FilteredSelect(Amortization filter);

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
        long FilteredDelete(Amortization filter);

        /// <summary>
        ///     ��ӻ��滻̯��
        ///     <para>���ޱ�ţ�������±��</para>
        /// </summary>
        /// <param name="entity">��̯��</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Upsert(Amortization entity);
    }
}

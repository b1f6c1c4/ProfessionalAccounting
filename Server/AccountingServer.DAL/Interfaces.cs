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
        /// <returns>ƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher filter);

        /// <summary>
        ///     �������������ڲ��Ҽ���ƾ֤
        ///     <para>��<paramref name="startDate" />��<paramref name="endDate" />��Ϊ<c>null</c>���򷵻����������ڵļ���ƾ֤</para>
        /// </summary>
        /// <param name="filter">������</param>
        /// <param name="startDate">��ʼ���ڣ���Ϊ<c>null</c>��ʾ�������С���ڣ����������</param>
        /// <param name="endDate">��ֹ���ڣ���Ϊ<c>null</c>��ʾ������������</param>
        /// <returns>ָ������ƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(Voucher filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     �����������Ҽ���ƾ֤������
        /// </summary>
        /// <param name="filter">������</param>
        /// <returns>ƥ��������ļ���ƾ֤����</returns>
        long FilteredCount(Voucher filter);

        /// <summary>
        ///     ��Ӽ���ƾ֤
        ///     <para>��<paramref name="entity" />û��ָ����ţ�����ӳɹ�����Զ���<paramref name="entity" />��ӱ��</para>
        /// </summary>
        /// <param name="entity">����ƾ֤</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Insert(Voucher entity);

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
        int FilteredDelete(Voucher filter);

        /// <summary>
        ///     ��ӻ��滻����ƾ֤
        ///     <para>���ܸı����ƾ֤�ı��</para>
        /// </summary>
        /// <param name="entity">�¼���ƾ֤</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Update(Voucher entity);

        /// <summary>
        ///     ��ϸĿ���������Ҽ���ƾ֤
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>��һϸĿƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(VoucherDetail filter);

        /// <summary>
        ///     ����������ϸĿ���������Ҽ���ƾ֤
        ///     <para>��<paramref name="startDate" />��<paramref name="endDate" />��Ϊ<c>null</c>���򷵻����������ڵļ���ƾ֤</para>
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="startDate">��ʼ���ڣ���Ϊ<c>null</c>��ʾ�������С���ڣ����������</param>
        /// <param name="endDate">��ֹ���ڣ���Ϊ<c>null</c>��ʾ������������</param>
        /// <returns>ָ��������һϸĿƥ��������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(VoucherDetail filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     ��ϸĿ���������Ҽ���ƾ֤
        /// </summary>
        /// <param name="filters">ϸĿ������</param>
        /// <returns>��һϸĿƥ����һ�������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters);

        /// <summary>
        ///     ����������ϸĿ���������Ҽ���ƾ֤
        ///     <para>��<paramref name="startDate" />��<paramref name="endDate" />��Ϊ<c>null</c>���򷵻����������ڵļ���ƾ֤</para>
        /// </summary>
        /// <param name="filters">ϸĿ������</param>
        /// <param name="startDate">��ʼ���ڣ���Ϊ<c>null</c>��ʾ�������С���ڣ����������</param>
        /// <param name="endDate">��ֹ���ڣ���Ϊ<c>null</c>��ʾ������������</param>
        /// <returns>ָ��������һϸĿƥ����һ�������ļ���ƾ֤</returns>
        IEnumerable<Voucher> FilteredSelect(IEnumerable<VoucherDetail> filters,
                                            DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     ��ϸĿ����������ϸĿ
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>ƥ���������ϸĿ</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter);

        /// <summary>
        ///     ����������ϸĿ����������ϸĿ
        ///     <para>��<paramref name="startDate" />��<paramref name="endDate" />��Ϊ<c>null</c>���򷵻����������ڵļ���ƾ֤</para>
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <param name="startDate">��ʼ���ڣ���Ϊ<c>null</c>��ʾ�������С���ڣ����������</param>
        /// <param name="endDate">��ֹ���ڣ���Ϊ<c>null</c>��ʾ������������</param>
        /// <returns>ָ������ƥ���������ϸĿ</returns>
        IEnumerable<VoucherDetail> FilteredSelectDetails(VoucherDetail filter, DateTime? startDate, DateTime? endDate);

        /// <summary>
        ///     ��ϸĿ����������ϸĿ������
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>ƥ���������ϸĿ����</returns>
        long FilteredCount(VoucherDetail filter);

        /// <summary>
        ///     ���ϸĿ
        /// </summary>
        /// <param name="entity">ϸĿ</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Insert(VoucherDetail entity);

        /// <summary>
        ///     ��������ɾ��ϸĿ
        /// </summary>
        /// <param name="filter">ϸĿ������</param>
        /// <returns>��ɾ����ϸĿ����</returns>
        int FilteredDelete(VoucherDetail filter);


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
        ///     ����ʲ�
        ///     <para>��<paramref name="entity" />û��ָ����ţ�����ӳɹ�����Զ���<paramref name="entity" />��ӱ��</para>
        /// </summary>
        /// <param name="entity">�ʲ�</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Insert(Asset entity);

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
        int FilteredDelete(Asset filter);

        /// <summary>
        ///     ��ӻ��滻�ʲ�
        ///     <para>���ܸı��ʲ��ı��</para>
        /// </summary>
        /// <param name="entity">���ʲ�</param>
        /// <returns>�Ƿ�ɹ�</returns>
        bool Update(Asset entity);

        //void Depreciate();
        //void Carry();
    }

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
        ///     �رշ�����
        /// </summary>
        void Shutdown();
    }
}

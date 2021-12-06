using System;
using System.Net;

namespace AccountingServer.TCP
{
    /// <summary>
    ///     �пͻ������ӵ�������
    /// </summary>
    /// <param name="ipEndPoint">�ͻ��˵�ַ</param>
    public delegate void ClientConnectedEventHandler(IPEndPoint ipEndPoint);

    /// <summary>
    ///     �пͻ��˴ӷ������Ͽ�����
    /// </summary>
    /// <param name="ipEndPoint">�ͻ��˵�ַ</param>
    public delegate void ClientDisconnectedEventHandler(IPEndPoint ipEndPoint);

    /// <summary>
    ///     ��һ����UTF8���롢<c>\n</c>��β���ַ������������
    /// </summary>
    /// <param name="str">�������ַ���</param>
    public delegate void DataArrivalEventHandler(string str);

    /// <summary>
    ///     TCP���ӹ������ӿ�
    /// </summary>
    public interface ITcpHelper : IDisposable
    {
        event ClientConnectedEventHandler ClientConnected;
        event ClientDisconnectedEventHandler ClientDisconnected;
        event DataArrivalEventHandler DataArrival;

        /// <summary>
        ///     ��ȡ������IP��ַ�Ͷ˿ں�
        /// </summary>
        string IPEndPointString { get; }

        /// <summary>
        ///     ��UTF8������ͻ��˷����ַ�����������<c>\n</c>��Ϊ��β
        /// </summary>
        /// <param name="s">�ַ���</param>
        void Write(string s);

        /// <summary>
        ///     �Ͽ���ǰ�ͻ��˵�����
        /// </summary>
        void Disconnect();
    }
}

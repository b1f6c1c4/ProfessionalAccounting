using System;
using System.Net;

namespace AccountingServer.TCP
{
    /// <summary>
    ///     有客户端连接到服务器
    /// </summary>
    /// <param name="ipEndPoint">客户端地址</param>
    public delegate void ClientConnectedEventHandler(IPEndPoint ipEndPoint);

    /// <summary>
    ///     有客户端从服务器断开连接
    /// </summary>
    /// <param name="ipEndPoint">客户端地址</param>
    public delegate void ClientDisconnectedEventHandler(IPEndPoint ipEndPoint);

    /// <summary>
    ///     有一组以UTF8编码、<c>\n</c>结尾的字符串到达服务器
    /// </summary>
    /// <param name="str">解码后的字符串</param>
    public delegate void DataArrivalEventHandler(string str);

    /// <summary>
    ///     TCP连接管理器接口
    /// </summary>
    public interface ITcpHelper : IDisposable
    {
        event ClientConnectedEventHandler ClientConnected;
        event ClientDisconnectedEventHandler ClientDisconnected;
        event DataArrivalEventHandler DataArrival;

        /// <summary>
        ///     获取服务器IP地址和端口号
        /// </summary>
        string IPEndPointString { get; }

        /// <summary>
        ///     以UTF8编码向客户端发送字符串，并加上<c>\n</c>作为结尾
        /// </summary>
        /// <param name="s">字符串</param>
        void Write(string s);

        /// <summary>
        ///     断开当前客户端的连接
        /// </summary>
        void Disconnect();
    }
}

namespace AccountingServer.TCP
{
    public interface ITcpHelper
    {
        event ClientConnectedEventHandler ClientConnected;
        event ClientDisconnectedEventHandler ClientDisconnected;
        event DataArrivalEventHandler DataArrival;
        string IPEndPointString { get; }
        void Write(string s);
        void Disconnect();
        void Stop();
    }
}

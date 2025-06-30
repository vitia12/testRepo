using System.Net.Sockets;

namespace NetSDR.Library.Services;

public interface IUdpClientWrapper : IDisposable
{
    Task<UdpReceiveResult> ReceiveAsync();
    void Connect(string hostName, int port);
    void Disconnect();
}
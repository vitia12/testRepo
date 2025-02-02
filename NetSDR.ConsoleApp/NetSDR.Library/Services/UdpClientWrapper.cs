using System.Net;
using System.Net.Sockets;

namespace NetSDR.Library.Services;

public class UdpClientWrapper : IUdpClientWrapper
{
    private UdpClient _udpClient = new();

    public async Task<UdpReceiveResult> ReceiveAsync() => await _udpClient.ReceiveAsync();

    public void Connect(string hostName, int port)
    {
        _udpClient = new(port);

        // Known issue: For some reason _udpClient.Connect doesn't work locally for 127.0.0.1
        // To test locally just comment lines 18,19
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostName), port);
        _udpClient.Connect(remoteEndPoint);
    }

    public void Disconnect() => _udpClient.Close();

    public void Dispose() => _udpClient.Dispose();
}
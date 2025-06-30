using System.Net.Sockets;

namespace NetSDR.Library.Services;

public class TcpClientWrapper : ITcpClientWrapper
{
    private TcpClient _tcpClient = new();

    public void Connect(string hostname, int port)
    {
        _tcpClient = new();
        _tcpClient.Connect(hostname, port);
    }

    public Stream GetStream() => _tcpClient.GetStream();

    public void Disconnect() => _tcpClient.Close();

    public void Dispose() => _tcpClient.Dispose();
}
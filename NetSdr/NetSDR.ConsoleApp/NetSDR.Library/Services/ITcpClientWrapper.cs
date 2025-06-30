namespace NetSDR.Library.Services;

public interface ITcpClientWrapper : IDisposable
{
    Stream GetStream();
    void Connect(string hostname, int port);
    void Disconnect();
}
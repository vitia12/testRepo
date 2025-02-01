using NetSDR.Library.Models;

namespace NetSDR.Library.Services;

public interface INetSdrClient
{
    void Connect(string host = "127.0.0.1", int tcpPort = 50000, int udpPort = 60000);
    void Disconnect();
    void StartStopReceiver(bool start, StartStopReceiverConfig startStopReceiverConfig);
    void SetFrequency(long frequencyHz, FrequencyConfig frequencyConfig);
}
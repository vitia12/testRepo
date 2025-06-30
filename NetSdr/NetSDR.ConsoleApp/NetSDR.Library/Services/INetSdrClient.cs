using NetSDR.Library.Models;

namespace NetSDR.Library.Services;

public interface INetSdrClient
{
    void Connect(string host, int tcpPort, int udpPort);
    void Disconnect();
    void ToggleReceiverState(bool start, ReceiverConfig receiverConfig);
    void SetFrequency(long frequencyHz, FrequencyConfig frequencyConfig);
}
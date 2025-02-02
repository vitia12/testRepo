namespace NetSDR.Library.Consts;

public static class CommandConstants
{
    public const byte StartReceiver = 0x02;
    public const byte StopReceiver = 0x01;
    public const byte RealAdSamples = 0x00;
    public const byte ComplexIqData = 0x80;
    public const int ResponseBufferSize = 1024;
    public const int MinimumUdpPacketLength = 4;
    public const int FileBufferSize = 4096;
    public const string NackHeader = "0200";
    public const string AckHeader = "6003";
}
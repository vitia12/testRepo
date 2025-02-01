using NetSDR.Library.Converters;
using System.Text.Json.Serialization;

namespace NetSDR.Library.Models
{
    public class EnvironmentSettings
    {
        [JsonPropertyName("Host")]
        public string Host { get; set; } = "127.0.0.1";

        [JsonPropertyName("TcpPort")]
        public int TcpPort { get; set; } = 50000;

        [JsonPropertyName("UdpPort")]
        public int UdpPort { get; set; } = 60000;

        [JsonPropertyName("StartStopReceiver")]
        public required StartStopReceiverConfig StartStopReceiver { get; set; }

        [JsonPropertyName("Frequency")]
        public required FrequencyConfig FrequencyConfig { get; set; }
    }

    public class StartStopReceiverConfig
    {
        [JsonPropertyName("IsComplex")]
        public bool IsComplex { get; set; } = true;

        [JsonConverter(typeof(HexStringToByteConverter))]
        public byte CaptureMode { get; set; } = 0x00;

        [JsonPropertyName("FifoSamples")]
        public byte? FifoSamples { get; set; }
    }

    public class FrequencyConfig
    {
        [JsonConverter(typeof(HexStringToByteConverter))]
        public byte ChannelId { get; set; } = 0xFF;
    }
}
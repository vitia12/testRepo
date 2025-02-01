using System.Text.Json.Serialization;
using System.Text.Json;

namespace NetSDR.Library.Converters;

public class HexStringToByteConverter : JsonConverter<byte>
{
    public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hexString = reader.GetString() ?? throw new JsonException("Invalid hex string.");
        return Convert.ToByte(hexString, 16);
    }

    public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{value:X2}"); // Keeps the JSON format as hex
    }
}

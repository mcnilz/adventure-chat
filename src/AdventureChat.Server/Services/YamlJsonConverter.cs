using YamlDotNet.Serialization;

namespace AdventureChat.Server.Services;

internal static class YamlJsonConverter
{
    public static string Convert(string yaml)
    {
        var raw = new DeserializerBuilder().WithDuplicateKeyChecking().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<object>(yaml);
        return new SerializerBuilder().JsonCompatible().Build().Serialize(raw);
    }
}

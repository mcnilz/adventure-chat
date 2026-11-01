using AdventureChat.Server.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdventureChat.Server.Services;

public interface IRoomYamlSerializer
{
    string Serialize(RoomYamlDto dto);
    RoomYamlDto Deserialize(string yaml);
}

public sealed class RoomYamlSerializer : IRoomYamlSerializer
{
    private const string SchemaHeader = "# yaml-language-server: $schema=../../schemas/room.schema.json\n";

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public RoomYamlSerializer()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking()
            .Build();
    }

    public string Serialize(RoomYamlDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var yaml = _serializer.Serialize(dto);
        return SchemaHeader + yaml;
    }

    public RoomYamlDto Deserialize(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        return _deserializer.Deserialize<RoomYamlDto>(yaml);
    }
}

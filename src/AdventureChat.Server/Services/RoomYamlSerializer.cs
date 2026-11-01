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
    private readonly string _schemaHeader;

    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public RoomYamlSerializer(string? schemaPath = null, string? roomsDirectory = null)
    {
        var association = "../../schemas/room.schema.json";
        if (schemaPath is not null && roomsDirectory is not null)
        {
            var relative = Path.GetRelativePath(roomsDirectory, schemaPath);
            association = Path.IsPathFullyQualified(relative) ? new Uri(schemaPath).AbsoluteUri : relative.Replace('\\', '/');
        }
        _schemaHeader = $"# yaml-language-server: $schema={association}\n";
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
        return _schemaHeader + yaml;
    }

    public RoomYamlDto Deserialize(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);
        return _deserializer.Deserialize<RoomYamlDto>(yaml);
    }
}

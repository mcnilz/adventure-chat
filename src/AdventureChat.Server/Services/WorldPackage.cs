using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace AdventureChat.Server.Services;

public sealed record WorldPackage(string Root, string Id, string Name, string FormatVersion, string ContentVersion, string StartRoom, string StartSpawn)
{
    public static WorldPackage Load(string root)
    {
        root = Path.GetFullPath(root);
        var file = SafePath(root, "world.yaml", true);
        try
        {
            var yaml = ReadYaml(file);
            var deserializer = new DeserializerBuilder().WithDuplicateKeyChecking().WithAttemptingUnquotedStringTypeDeserialization().Build();
            var raw = deserializer.Deserialize<Dictionary<object, object>>(yaml);
            var version = raw.TryGetValue("formatVersion", out var value) ? value?.ToString() : null;
            if (version is not ("1.0.0" or "1.1.0" or "1.2.0")) throw new InvalidOperationException($"World manifest '{file}', field formatVersion: unsupported version '{version}'.");
            var json = new SerializerBuilder().JsonCompatible().Build().Serialize(raw);
            var schemaFile = FindSchema("world.schema.json", version);
            var schema = JsonSchema.FromText(File.ReadAllText(schemaFile));
            var result = schema.Evaluate(JsonNode.Parse(json), new EvaluationOptions { OutputFormat = OutputFormat.List });
            if (!result.IsValid) throw new InvalidOperationException($"Invalid world manifest '{file}': {string.Join("; ", result.Details.Where(d => d.HasErrors).SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}")))}");
            string Field(string name) => raw[name].ToString()!;
            var contentVersion = Field("contentVersion");
            if (!Regex.IsMatch(contentVersion, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"))
                throw new InvalidOperationException($"Invalid contentVersion in world manifest '{file}'.");
            return new WorldPackage(root, Field("id"), Field("name"), version, contentVersion, Field("startRoom"), Field("startSpawn"));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Invalid world manifest '{file}': {ex.Message}", ex);
        }
    }

    public void EnsureManifestUnchanged()
    {
        if (Load(Root) != this) throw new InvalidOperationException($"World '{Id}' manifest changed; restart before editing.");
    }

    public IReadOnlyList<AdventureChat.Server.Models.RoomYamlDto> LoadRooms()
    {
        EnsureManifestUnchanged();
        var directory = SafePath(Root, "rooms");
        if (!Directory.Exists(directory)) throw new InvalidOperationException($"World '{Id}': rooms directory is missing.");
        var serializer = new RoomYamlSerializer();
        var validator = new RoomValidator(FindSchema("room.schema.json", FormatVersion));
        var candidates = new List<(string File, AdventureChat.Server.Models.RoomYamlDto Room)>();
        foreach (var file in Directory.GetFiles(directory, "*.yaml").OrderBy(path => path, StringComparer.Ordinal))
        {
            var relative = "rooms/" + Path.GetFileName(file);
            try
            {
                SafePath(Root, relative, true);
                var yaml = ReadYaml(file);
                var json = YamlJsonConverter.Convert(yaml);
                var schema = validator.ValidateJson(json);
                if (!schema.IsValid) throw new InvalidOperationException(string.Join("; ", schema.Errors));
                candidates.Add((relative, serializer.Deserialize(yaml)));
            }
            catch (Exception ex) { throw new InvalidOperationException($"World '{Id}', file '{relative}': {ex.Message}", ex); }
        }
        foreach (var (file, room) in candidates)
        {
            try
            {
                var validation = validator.Validate(room);
                if (!validation.IsValid) throw new InvalidOperationException(string.Join("; ", validation.Errors));
                if (!string.Equals(Path.GetFileNameWithoutExtension(file), room.Id, StringComparison.Ordinal))
                    throw new InvalidOperationException("Room ID must match its file name.");
                ValidateBackground(room);
            }
            catch (Exception ex) { throw new InvalidOperationException($"World '{Id}', file '{file}': {ex.Message}", ex); }
        }
        var rooms = candidates.Select(candidate => candidate.Room).ToList();
        var result = validator.ValidateWorld(rooms);
        if (!result.IsValid) throw new InvalidOperationException($"Invalid world '{Id}', room files {string.Join(", ", rooms.Select(r => $"rooms/{r.Id}.yaml"))}: {string.Join("; ", result.Errors)}");
        var start = rooms.SingleOrDefault(r => string.Equals(r.Id, StartRoom, StringComparison.Ordinal));
        if (start is null || !start.Spawns.Any(s => string.Equals(s.Id, StartSpawn, StringComparison.Ordinal)))
            throw new InvalidOperationException($"World '{Id}', world.yaml: startRoom '{StartRoom}' or startSpawn '{StartSpawn}' does not exist.");
        return rooms;
    }

    public void ValidateBackground(AdventureChat.Server.Models.RoomYamlDto room)
    {
        var background = SafePath(Root, room.Background, true);
        if (!room.Background.StartsWith("assets/images/", StringComparison.Ordinal) || !string.Equals(Path.GetExtension(background), ".png", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Background must be a PNG in assets/images.");
        try { PngValidator.Validate(background, room.Size.Width, room.Size.Height); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { throw new InvalidOperationException($"Invalid background PNG: {ex.Message}", ex); }
    }

    public static string FindSchema(string name, string version = "1.0.0")
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var path = Path.Combine(current, "schemas", "world", version, name);
            if (File.Exists(path)) return path;
            current = Directory.GetParent(current)?.FullName;
        }
        throw new InvalidOperationException($"Required offline world schema '{name}' was not found.");
    }

    public static string SafePath(string root, string relative, bool mustExist = false)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Contains(':') || relative.Split('/').Any(p => p is "" or "." or ".."))
            throw new InvalidOperationException($"Invalid world-relative path '{relative}'.");
        var basePath = Path.GetFullPath(root);
        var path = basePath;
        for (var ancestor = new DirectoryInfo(basePath); ancestor is not null; ancestor = ancestor.Parent)
            if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("World path cannot contain symbolic links.");
        foreach (var part in relative.Split('/'))
        {
            path = Path.Combine(path, part);
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException($"Symbolic links are not permitted in world paths: '{relative}'.");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        if (mustExist && !File.Exists(path)) throw new InvalidOperationException($"World file '{relative}' does not exist.");
        return path;
    }

    public static string ReadYaml(string file)
    {
        using var stream = File.OpenRead(file);
        if (stream.Length > 1024 * 1024) throw new InvalidOperationException("YAML document exceeds 1 MB.");
        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();
        EnsureSafeYaml(yaml);
        return yaml;
    }

    public static void EnsureSafeYaml(string yaml)
    {
        if (yaml.Length > 1024 * 1024) throw new InvalidOperationException("YAML document exceeds 1 MB.");
        var parser = new Parser(new StringReader(yaml));
        while (parser.MoveNext())
        {
            if (parser.Current is AnchorAlias) throw new InvalidOperationException("YAML aliases are not permitted.");
            if (parser.Current is NodeEvent node && (!node.Anchor.IsEmpty || !node.Tag.IsEmpty))
                throw new InvalidOperationException("YAML anchors and explicit tags are not permitted.");
        }
    }
}

using AdventureChat.Server.Models;
using YamlDotNet.RepresentationModel;

namespace AdventureChat.Server.Services;

internal static class EditorFormatUpgrade
{
    public const string Target = "1.8.0";
    public const string TransitionTarget = "1.9.0";
    public const string ActorAnimationTarget = "1.10.0";

    public static bool CanUpgrade(string source, string target) =>
        target is Target or TransitionTarget or ActorAnimationTarget && Version.Parse(source) < Version.Parse(target);

    public static void Save(WorldPackage world, string sourceVersion, string targetVersion, string roomFile, RoomYamlDto room,
        IReadOnlyList<RoomYamlDto> existing, IReadOnlyList<RoomYamlDto> candidates)
    {
        if (world.FormatVersion != sourceVersion || !CanUpgrade(sourceVersion, targetVersion))
            throw new InvalidOperationException("Das Weltformat hat sich geändert. Bitte den Editor neu laden.");

        var manifest = WorldPackage.SafePath(world.Root, "world.yaml", true);
        var original = WorldPackage.ReadYaml(manifest);
        var targetManifest = ChangeVersion(original, targetVersion);
        _ = WorldPackage.ReadManifest(world.Root, manifest, targetManifest);
        var schema = WorldPackage.FindSchema("room.schema.json", targetVersion);
        var serializer = new RoomYamlSerializer(schema, Path.GetDirectoryName(roomFile));
        var validator = new RoomValidator(schema);
        var yaml = serializer.Serialize(room);
        foreach (var saved in existing)
        {
            var content = WorldPackage.ReadYaml(WorldPackage.SafePath(world.Root, $"rooms/{saved.Id}.yaml", true));
            RequireValid(validator.ValidateJson(YamlJsonConverter.Convert(content)));
        }
        RequireValid(validator.ValidateWorld(existing));
        RequireValid(validator.ValidateWorld(candidates));
        RequireValid(validator.ValidateJson(YamlJsonConverter.Convert(yaml)));
        world.ValidateBackground(room);

        // The existing rooms were validated against the target schema. Replacing
        // the manifest first therefore keeps the world readable between writes.
        var manifestTemp = $"{manifest}.{Guid.NewGuid():N}.tmp";
        var roomTemp = $"{roomFile}.{Guid.NewGuid():N}.tmp";
        var backup = $"{manifestTemp}.backup";
        try
        {
            File.WriteAllText(manifestTemp, targetManifest);
            File.WriteAllText(roomTemp, yaml);
            File.Replace(manifestTemp, manifest, backup);
            try { File.Move(roomTemp, roomFile, overwrite: true); }
            catch
            {
                File.Move(backup, manifest, overwrite: true);
                throw;
            }
            world.AdoptFormat(targetVersion);
            DeleteQuietly(backup);
        }
        finally
        {
            DeleteQuietly(manifestTemp);
            DeleteQuietly(roomTemp);
        }
    }

    private static string ChangeVersion(string original, string targetVersion)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(original));
        var mapping = (YamlMappingNode)yaml.Documents.Single().RootNode;
        var node = mapping.Children[new YamlScalarNode("formatVersion")];
        var before = checked((int)node.Start.Index);
        var after = checked((int)node.End.Index);
        return original[..before] + targetVersion + original[after..];
    }

    private static void RequireValid(ValidationResult result)
    {
        if (!result.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
    }

    private static void DeleteQuietly(string path)
    {
        try { File.Delete(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
}

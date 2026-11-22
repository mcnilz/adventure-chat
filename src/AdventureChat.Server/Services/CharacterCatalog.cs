using System.Text.Json.Nodes;

namespace AdventureChat.Server.Services;

public sealed record CharacterDefinition(string Id, string Name, string Image, int FrameWidth, int FrameHeight,
    string? TopDownImage = null, bool NpcOnly = false, string? SpeechImage = null,
    string? TopDownSpeechImage = null, string? WaveImage = null)
{
    public static string? AssetUrl(string worldId, string? image) => image is null ? null :
        $"/world-assets/{Uri.EscapeDataString(worldId)}/images/{string.Join('/', image[14..].Split('/').Select(Uri.EscapeDataString))}";

    public string? TopDownImageUrl(string worldId) => AssetUrl(worldId, TopDownImage);
    public string ImageUrl(string worldId) => AssetUrl(worldId, Image)!;
}

internal static class CharacterCatalog
{
    // Schema validation precedes this semantic and resource validation.
    public static IReadOnlyList<CharacterDefinition> Load(string root, JsonNode manifest)
    {
        if (manifest["characters"] is not JsonArray entries) return Array.Empty<CharacterDefinition>();
        var result = new List<CharacterDefinition>();
        foreach (var entry in entries)
        {
            var character = new CharacterDefinition(entry!["id"]!.GetValue<string>(), entry["name"]!.GetValue<string>(),
                entry["image"]!.GetValue<string>(), entry["frameWidth"]!.GetValue<int>(), entry["frameHeight"]!.GetValue<int>(),
                entry["topDownImage"]?.GetValue<string>(), entry["npcOnly"]?.GetValue<bool>() ?? false,
                entry["speechImage"]?.GetValue<string>(), entry["topDownSpeechImage"]?.GetValue<string>(), entry["waveImage"]?.GetValue<string>());
            if (result.Any(c => c.Id == character.Id)) throw new InvalidOperationException($"Duplicate character ID '{character.Id}'.");
            foreach (var image in new[] { character.Image, character.TopDownImage, character.SpeechImage, character.TopDownSpeechImage, character.WaveImage })
                if (image is not null) ValidateSheet(root, character, image);
            result.Add(character);
        }
        if (!result.Any(c => !c.NpcOnly && c.Id == manifest["defaultCharacterId"]!.GetValue<string>()))
            throw new InvalidOperationException("defaultCharacterId must reference a playable character in this world.");
        return result.AsReadOnly();
    }

    private static void ValidateSheet(string root, CharacterDefinition character, string image)
    {
        try { PngValidator.Validate(WorldPackage.SafePath(root, image, true), character.FrameWidth * 4, character.FrameHeight * 3); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        { throw new InvalidOperationException($"Character '{character.Id}', image '{image}': {ex.Message}", ex); }
    }
}

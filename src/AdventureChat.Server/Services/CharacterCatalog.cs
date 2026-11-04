using System.Text.Json.Nodes;

namespace AdventureChat.Server.Services;

public sealed record CharacterDefinition(string Id, string Name, string Image, int FrameWidth, int FrameHeight)
{
    public string ImageUrl(string worldId) => $"/world-assets/{Uri.EscapeDataString(worldId)}/images/{string.Join('/', Image[14..].Split('/').Select(Uri.EscapeDataString))}";
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
                entry["image"]!.GetValue<string>(), entry["frameWidth"]!.GetValue<int>(), entry["frameHeight"]!.GetValue<int>());
            if (result.Any(c => c.Id == character.Id)) throw new InvalidOperationException($"Duplicate character ID '{character.Id}'.");
            try
            {
                var file = WorldPackage.SafePath(root, character.Image, true);
                PngValidator.Validate(file, character.FrameWidth * 4, character.FrameHeight * 3);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            { throw new InvalidOperationException($"Character '{character.Id}', image '{character.Image}': {ex.Message}", ex); }
            result.Add(character);
        }
        if (!result.Any(c => c.Id == manifest["defaultCharacterId"]!.GetValue<string>()))
            throw new InvalidOperationException("defaultCharacterId must reference a character in this world.");
        return result.AsReadOnly();
    }
}

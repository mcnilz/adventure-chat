using System.Net.Http.Json;
using System.Text.Json;
using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using NUnit.Framework;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcAuthoringTests
{
    [Test]
    public async Task EditorValidatesAndSavesNpcsAcrossAnExplicitFormatUpgrade()
    {
        await using var server = CreateReferenceCopy();
        var world = Path.Combine(server.TempRoot, "world");
        var lounge = Path.Combine(world, "rooms/lounge.yaml");
        var serializer = new RoomYamlSerializer();
        var original = File.ReadAllText(lounge);
        var draft = serializer.Deserialize(original);
        var olderRoom = serializer.Deserialize(original);
        olderRoom.Npcs = null;
        File.WriteAllText(lounge, serializer.Serialize(olderRoom));
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.11.0", "1.10.0", StringComparison.Ordinal));
        await server.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        const string roomUrl = "/api/editor/rooms/lounge?worldId=reference";
        var status = await client.GetFromJsonAsync<JsonElement>("/api/editor/status?worldId=reference");
        Assert.That(status.GetProperty("formatVersion").GetString(), Is.EqualTo("1.10.0"));
        client.DefaultRequestHeaders.Add("X-Author-Token", status.GetProperty("token").GetString());

        using var validationResponse = await client.PostAsJsonAsync("/api/editor/validate?worldId=reference&targetFormat=1.11.0&includeWorld=true", draft);
        var validation = await validationResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(validation.GetProperty("valid").GetBoolean(), Is.True, validation.GetRawText());

        using var upgrade = await client.PostAsJsonAsync(roomUrl + "&upgradeFrom=1.10.0&targetFormat=1.11.0", draft);
        Assert.That(upgrade.IsSuccessStatusCode, Is.True, await upgrade.Content.ReadAsStringAsync());
        var saved = (await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!;
        var options = saved.Npcs!.Single().Options;
        options[0] = options[0] with { Reply = "Willkommen in der Lounge." };
        using var save = await client.PostAsJsonAsync(roomUrl, saved);
        Assert.That(save.IsSuccessStatusCode, Is.True, await save.Content.ReadAsStringAsync());

        var reloaded = await client.GetFromJsonAsync<RoomYamlDto>(roomUrl);
        Assert.That(reloaded!.Npcs!.Single().Options[0].Reply, Is.EqualTo("Willkommen in der Lounge."));
    }
}

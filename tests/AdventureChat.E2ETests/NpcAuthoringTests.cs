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
    public async Task EditorRoundTripsTourAndRejectsAnUnknownExitOrUnreachableStation()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        const string roomUrl = "/api/editor/rooms/lounge?worldId=reference";
        var status = await client.GetFromJsonAsync<JsonElement>("/api/editor/status?worldId=reference");
        client.DefaultRequestHeaders.Add("X-Author-Token", status.GetProperty("token").GetString());
        var lounge = (await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!;
        var route = lounge.Npcs!.Single().Tour!;
        route[2] = route[2] with { Text = "Willkommen im Garten." };
        using var saved = await client.PostAsJsonAsync(roomUrl, lounge);
        Assert.That(saved.IsSuccessStatusCode, Is.True, await saved.Content.ReadAsStringAsync());

        var door = route[1];
        route[1] = door with { Exit = "missing-exit" };
        using var missing = await client.PostAsJsonAsync(roomUrl, lounge);
        Assert.That(missing.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(await missing.Content.ReadAsStringAsync(), Does.Contain("tour exit 'missing-exit'"));
        route[1] = door;

        route[2] = route[2] with { Position = new(350, 150) };
        using var unreachable = await client.PostAsJsonAsync(roomUrl, lounge);
        Assert.That(unreachable.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(await unreachable.Content.ReadAsStringAsync(), Does.Contain("unreachable in room 'garden'"));
        var reloaded = (await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!.Npcs!.Single().Tour!;
        Assert.That(reloaded[1].Exit, Is.EqualTo("to-garden"));
        Assert.That(reloaded[2], Is.EqualTo(new NpcTourStep(new(224, 294), "Willkommen im Garten.")));

        const string gardenUrl = "/api/editor/rooms/garden?worldId=reference";
        var garden = (await client.GetFromJsonAsync<RoomYamlDto>(gardenUrl))!;
        garden.Exits!.RemoveAll(exit => exit.Id == "to-lounge");
        using var oneWay = await client.PostAsJsonAsync(gardenUrl, garden);
        Assert.That(oneWay.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(await oneWay.Content.ReadAsStringAsync(), Does.Contain("tour has no walkable return route"));
        Assert.That((await client.GetFromJsonAsync<RoomYamlDto>(gardenUrl))!.Exits!.Any(exit => exit.Id == "to-lounge"), Is.True);
    }

    [Test]
    public async Task EditorValidatesAndSavesNpcsAcrossAnExplicitFormatUpgrade()
    {
        await using var server = CreateReferenceCopy();
        var world = Path.Combine(server.TempRoot, "world");
        var lounge = Path.Combine(world, "rooms/lounge.yaml");
        var serializer = new RoomYamlSerializer();
        var original = File.ReadAllText(lounge);
        var draft = serializer.Deserialize(original);
        draft.Npcs![0] = draft.Npcs[0] with { Tour = null, FollowDistance = null };
        var npc = draft.Npcs!.Single();
        npc.Options.RemoveAll(option => option.When is not null);
        npc.Options[0] = npc.Options[0] with { Replies = null };
        var olderRoom = serializer.Deserialize(original);
        olderRoom.Npcs = null;
        File.WriteAllText(lounge, serializer.Serialize(olderRoom));
        var manifest = Path.Combine(world, "world.yaml");
        DowngradeReference(manifest, "1.10.0", serializer);
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

    [Test]
    public async Task EditorUpgradesAndRoundTripsConditionsButRejectsBrokenReferences()
    {
        await using var server = CreateReferenceCopy();
        var serializer = new RoomYamlSerializer();
        var loungePath = Path.Combine(server.TempRoot, "world/rooms/lounge.yaml");
        var draft = serializer.Deserialize(File.ReadAllText(loungePath));
        draft.Npcs![0] = draft.Npcs[0] with { Tour = null, FollowDistance = null };
        var olderRoom = serializer.Deserialize(File.ReadAllText(loungePath));
        var olderOptions = olderRoom.Npcs!.Single().Options;
        olderOptions.RemoveAll(option => option.When is not null);
        olderOptions[0] = olderOptions[0] with { Replies = null };
        File.WriteAllText(loungePath, serializer.Serialize(olderRoom));
        var manifest = Path.Combine(server.TempRoot, "world/world.yaml");
        DowngradeReference(manifest, "1.11.0", serializer);
        await server.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        const string roomUrl = "/api/editor/rooms/lounge?worldId=reference";
        var status = await client.GetFromJsonAsync<JsonElement>("/api/editor/status?worldId=reference");
        client.DefaultRequestHeaders.Add("X-Author-Token", status.GetProperty("token").GetString());
        using var upgrade = await client.PostAsJsonAsync(roomUrl + "&upgradeFrom=1.11.0&targetFormat=1.12.0", draft);
        Assert.That(upgrade.IsSuccessStatusCode, Is.True, await upgrade.Content.ReadAsStringAsync());

        var saved = (await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!;
        var replies = saved.Npcs!.Single().Options[0].Replies!;
        Assert.That(replies.Single().When, Is.EqualTo(new DialogueCondition("bell", "rung")));
        replies[0] = replies[0] with { When = new("bell", "missing") };
        using var invalid = await client.PostAsJsonAsync(roomUrl, saved);
        Assert.That(invalid.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(await invalid.Content.ReadAsStringAsync(), Does.Contain("unknown state 'missing'"));
        Assert.That(serializer.Deserialize(File.ReadAllText(loungePath)).Npcs!.Single().Options[0].Replies![0].When.State,
            Is.EqualTo("rung"));
    }

    [Test]
    public async Task EditorPreservesOrderedWaypointsAcrossUpgradeAndRejectsUnwalkableEdits()
    {
        await using var server = CreateReferenceCopy();
        var serializer = new RoomYamlSerializer();
        var path = Path.Combine(server.TempRoot, "world/rooms/garden.yaml");
        var draft = serializer.Deserialize(File.ReadAllText(path));
        DowngradeReference(Path.Combine(server.TempRoot, "world/world.yaml"), "1.12.0", serializer);
        await server.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        const string roomUrl = "/api/editor/rooms/garden?worldId=reference";
        var status = await client.GetFromJsonAsync<JsonElement>("/api/editor/status?worldId=reference");
        client.DefaultRequestHeaders.Add("X-Author-Token", status.GetProperty("token").GetString());
        using var invalidVersion = await client.PostAsJsonAsync(roomUrl, draft);
        Assert.That(invalidVersion.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        using var upgrade = await client.PostAsJsonAsync(roomUrl + "&upgradeFrom=1.12.0&targetFormat=1.13.0", draft);
        Assert.That(upgrade.IsSuccessStatusCode, Is.True, await upgrade.Content.ReadAsStringAsync());

        var saved = (await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!;
        var waypoints = saved.Npcs!.Single().Waypoints!;
        Assert.That(waypoints, Is.EqualTo(new Point2D[] { new(210, 310), new(184, 328), new(126, 310) }));
        waypoints[1] = new(800, 350);
        using var invalidEdit = await client.PostAsJsonAsync(roomUrl, saved);
        Assert.That(invalidEdit.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That((await client.GetFromJsonAsync<RoomYamlDto>(roomUrl))!.Npcs!.Single().Waypoints![1], Is.EqualTo(new Point2D(184, 328)));
    }

    private static void DowngradeReference(string manifest, string version, RoomYamlSerializer serializer)
    {
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.14.0", version, StringComparison.Ordinal));
        var loungePath = Path.Combine(Path.GetDirectoryName(manifest)!, "rooms/lounge.yaml");
        var lounge = serializer.Deserialize(File.ReadAllText(loungePath));
        if (lounge.Npcs is { Count: > 0 }) lounge.Npcs[0] = lounge.Npcs[0] with { Tour = null, FollowDistance = null };
        File.WriteAllText(loungePath, serializer.Serialize(lounge));
        var gardenPath = Path.Combine(Path.GetDirectoryName(manifest)!, "rooms/garden.yaml");
        var garden = serializer.Deserialize(File.ReadAllText(gardenPath));
        garden.Npcs = null;
        File.WriteAllText(gardenPath, serializer.Serialize(garden));
    }
}

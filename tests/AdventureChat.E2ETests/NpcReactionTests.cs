using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.NpcDialogueTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcReactionTests
{
    private const string Nela = "window.__ADVENTURE_GAME__.room.npcs.find(npc => npc.id === 'nela')";
    private const string Guard = "window.__ADVENTURE_GAME__.room.npcs.find(npc => npc.id === 'guard')";

    [Test]
    public async Task TwoVisitorsSeeTheSameArrivalAfterRingingTogether()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await JoinWorld(browser, server.BaseUrl, "mara");
        var second = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Task.WhenAll(Ring(first), Ring(second));
        foreach (var page in new[] { first, second })
        {
            try
            {
                await page.WaitForFunctionAsync($"{Nela}.position.x === 160 && {Nela}.position.y === 330 && !{Nela}.motion?.route.length",
                    options: new() { Timeout = 5000 });
            }
            catch (TimeoutException)
            {
                Assert.Fail($"Nela: {await page.EvaluateAsync<string>($"JSON.stringify({Nela})")}. " +
                    $"Aktion: {await page.Locator("#action-feedback").InnerTextAsync()}.");
            }
        }
    }

    [Test]
    public async Task DoorStopsAnInterruptedCallAndTheGuardCanBeCalledAgain()
    {
        await using var server = CreateDoorCall();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await JoinWorld(browser, server.BaseUrl, "bruno");
        var second = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Act(first, "Ziehen", 160, 200);
        await second.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.objects.find(obj => obj.id === 'chair').position.x === 200");
        Assert.That(await second.EvaluateAsync<double>($"{Guard}.position.x"), Is.EqualTo(50));

        await Act(first, "Benutzen", 290, 250);
        await second.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.exits.length === 1");
        Assert.That(await second.EvaluateAsync<double>($"{Guard}.position.x"), Is.EqualTo(50));
        await Act(second, "Drücken", 210, 200);
        await second.WaitForFunctionAsync($"{Guard}.motion?.route.at(-1)?.x === 500");
        await Act(first, "Benutzen", 340, 250);
        await first.WaitForFunctionAsync($"window.__ADVENTURE_GAME__.room.exits.length === 0 && !{Guard}.motion?.route.length");
        var stopped = await first.EvaluateAsync<double>($"{Guard}.position.x");
        Assert.That(stopped, Is.GreaterThanOrEqualTo(50).And.LessThan(280));

        await Act(first, "Benutzen", 290, 250);
        await second.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.exits.length === 1");
        await Act(second, "Ziehen", 160, 200);
        await second.WaitForFunctionAsync($"{Guard}.position.x === 500 && !{Guard}.motion?.route.length");
    }

    private static TestServerFixture CreateDoorCall()
    {
        var server = CreateServer();
        var world = Path.Combine(server.TempRoot, "world");
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        File.Copy(Path.Combine(reference, "assets/images/bruno.png"), Path.Combine(world, "assets/images/bruno.png"));
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.8.0", "1.15.0", StringComparison.Ordinal) + """

            defaultCharacterId: bruno
            characters:
              - { id: bruno, name: Bruno, image: assets/images/bruno.png, frameWidth: 128, frameHeight: 128 }
            """);
        var path = Path.Combine(world, "rooms/hall.yaml");
        var serializer = new RoomYamlSerializer();
        var room = serializer.Deserialize(File.ReadAllText(path));
        room.Npcs = [new("guard", "Wache", "bruno", new(50, 300), new(70, 300),
            [new("hello", "Hallo", "Guten Tag"), new("bye", "Tschüss", "Auf Wiedersehen")])];
        foreach (var action in room.Objects!.Single(obj => obj.Id == "chair").Interaction!.Actions)
            action.NpcReaction = new ObjectNpcReaction("guard", new(500, 300));
        File.WriteAllText(path, serializer.Serialize(room));
        return server;
    }

    private static async Task Act(IPage page, string verb, int x, int y)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = verb, Exact = true }).ClickAsync();
        await ClickCanvas(page, x, y);
    }

    private static async Task Ring(IPage page)
    {
        await Act(page, "Drücken", 203, 240);
    }
}

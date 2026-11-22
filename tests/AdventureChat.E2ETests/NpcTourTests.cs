using Microsoft.Playwright;
using NUnit.Framework;
using AdventureChat.Server.Services;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcTourTests
{
    private const string Game = "window.__ADVENTURE_GAME__";
    private const string StartLabel = "Führung starten";
    private const string StopLabel = "Führung beenden";

    [Test]
    public async Task ConsentingGuestFollowsNelaAndCanStopAfterTheRoomChange()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var guest = await Join(browser, server.BaseUrl, "Gast");
        var observer = await Join(browser, server.BaseUrl, "Zuschauer");

        await TalkToGuide(guest, 112, 234);
        await Assertions.Expect(Button(guest, StopLabel)).ToBeHiddenAsync();
        await guest.WaitForFunctionAsync($"!{Game}.room.npcs.find(npc => npc.id === 'nela').motion?.route.length");
        await Button(guest, StartLabel).ClickAsync(new() { Timeout = 5000 });
        await Assertions.Expect(Button(guest, StopLabel)).ToBeVisibleAsync();
        await Assertions.Expect(Button(observer, StopLabel)).ToBeHiddenAsync();
        await observer.WaitForFunctionAsync($"{Game}.room.npcs.find(npc => npc.id === 'nela')?.motion?.route.length > 1");
        await ClickCanvas(observer, 160, 338);
        await observer.WaitForFunctionAsync($"{Game}.character.position.y > 335");
        await Assertions.Expect(guest.Locator("#npc-tour-message")).ToContainTextAsync("An der Klingel");
        await guest.WaitForFunctionAsync($"{Game}.room.id === 'garden'");
        await Assertions.Expect(Button(guest, StopLabel)).ToBeVisibleAsync();
        await guest.EvaluateAsync($$"""
            () => {
              const game = {{Game}};
              const send = game.sendChatMessage.bind(game);
              game.sendChatMessage = text => {
                game.sendChatMessage = send;
                return game.networkClient.sendChatMessage(text, 'lounge');
              };
            }
            """);
        await guest.FillAsync("#chat-input", "Nachricht aus der vorigen Lounge");
        await guest.ClickAsync("#chat-send");
        await Assertions.Expect(guest.Locator("#chat-messages")).ToContainTextAsync("Sende die Nachricht erneut.");
        await Assertions.Expect(guest.Locator("#chat-input")).ToHaveValueAsync("Nachricht aus der vorigen Lounge");
        await Assertions.Expect(guest.Locator("#npc-tour-message")).ToContainTextAsync("Hier ist der Tavernen-Garten.");
        await guest.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.name === 'Nela')");
        await Button(observer, "Drücken").ClickAsync();
        await ClickCanvas(observer, 203, 240);
        await Assertions.Expect(observer.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.");
        if (Environment.GetEnvironmentVariable("NPC_TOUR_EVIDENCE") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            await guest.ScreenshotAsync(new() { Path = Path.Combine(directory, "garden-station.png") });
        }
        await Button(guest, StopLabel).ClickAsync();
        await Assertions.Expect(guest.Locator("#action-feedback")).ToHaveTextAsync("Führung beendet.");
        await ClickCanvas(guest, 211, 338);
        await guest.WaitForFunctionAsync($"{Game}.character.position.y > 335");
        await guest.FillAsync("#chat-input", "Zurück im Raumchat");
        await guest.ClickAsync("#chat-send");
        await Assertions.Expect(guest.Locator("#chat-messages")).ToContainTextAsync("Zurück im Raumchat");
        await Assertions.Expect(observer.Locator("#chat-messages")).Not.ToContainTextAsync("Zurück im Raumchat");
        Assert.That(await observer.EvaluateAsync<string>($"{Game}.room.id"), Is.EqualTo("lounge"));
        await observer.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 112 && !npc.motion?.route.length)");
    }

    [Test]
    public async Task AnotherGuestCanCloseTheExitDuringTheTour()
    {
        await using var server = CreateDoorTour();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var guest = await Join(browser, server.BaseUrl, "Gast");
        var observer = await Join(browser, server.BaseUrl, "Türdienst");
        await TalkToGuide(guest, 100, 266);
        await Button(guest, StartLabel).ClickAsync();
        await guest.WaitForFunctionAsync($"{Game}.room.npcs.find(npc => npc.id === 'guide')?.position.x > 145");
        Assert.That(await guest.EvaluateAsync<double>($"{Game}.character.position.x"), Is.LessThan(160),
            "The guest must wait for the guide to pass their starting point.");
        await guest.WaitForFunctionAsync($"{Game}.character.state === 'walk' && {Game}.character.position.x > 205");
        await Button(observer, "Schließen").ClickAsync();
        await ClickCanvas(observer, 340, 250);
        await Assertions.Expect(observer.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.");
        await Assertions.Expect(guest.Locator("#action-feedback")).ToContainTextAsync("Durchgang ist versperrt.");
        Assert.That(await guest.EvaluateAsync<string>($"{Game}.room.id"), Is.EqualTo("hall"));
        Assert.That(await guest.EvaluateAsync<double>($"{Game}.character.position.x"), Is.LessThan(280));
        await Assertions.Expect(Button(guest, StopLabel)).ToBeHiddenAsync();
        await ClickCanvas(guest, 120, 325);
        await guest.WaitForFunctionAsync($"{Game}.character.position.y > 323");
    }

    [Test]
    public async Task StopWhileWalkingAndDisconnectBothReleaseTheGuideForANewTour()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var guest = await Join(browser, proxy.BaseUrl, "Gast");
        var observer = await Join(browser, server.BaseUrl, "Zuschauer");
        await TalkToGuide(guest, 112, 234);
        await Button(guest, StartLabel).ClickAsync();
        await guest.WaitForFunctionAsync($"{Game}.character.state === 'walk'");
        await Button(guest, StopLabel).ClickAsync();
        await Assertions.Expect(guest.Locator("#action-feedback")).ToHaveTextAsync("Führung beendet.");
        await guest.WaitForFunctionAsync($"{Game}.character.state === 'idle' && {Game}.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 112 && !npc.motion?.route.length)");
        await TalkToGuide(guest, 112, 234);
        await Button(guest, StartLabel).ClickAsync();
        await Assertions.Expect(Button(guest, StopLabel)).ToBeVisibleAsync();
        var oldSession = await guest.EvaluateAsync<string>($"{Game}.sessionId");
        proxy.Interrupt();
        await guest.WaitForFunctionAsync($"id => {Game}.connectionStatus === 'connected' && {Game}.sessionId !== id", oldSession);
        await Assertions.Expect(Button(guest, StopLabel)).ToBeHiddenAsync();
        await Assertions.Expect(guest.Locator("#action-feedback")).ToContainTextAsync("Verbindung unterbrochen");
        Assert.That(await guest.EvaluateAsync<string>($"{Game}.room.id"), Is.EqualTo("lounge"));
        await observer.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 112 && !npc.motion?.route.length)");
        await TalkToGuide(observer, 112, 234);
        await Button(observer, StartLabel).ClickAsync();
        await Assertions.Expect(Button(observer, StopLabel)).ToBeVisibleAsync();
        await observer.WaitForFunctionAsync($"{Game}.room.id === 'garden'");
        await Assertions.Expect(observer.Locator("#npc-tour-message")).ToContainTextAsync("Hier ist der Tavernen-Garten.");
        await observer.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.name === 'Nela' && npc.position.x === 224)");
        var spacing = await observer.EvaluateAsync<double>($$"""
            () => {
              const game = {{Game}};
              const guide = game.room.npcs.find(npc => npc.name === 'Nela').position;
              return Math.hypot(guide.x - game.character.position.x, guide.y - game.character.position.y);
            }
            """);
        Assert.That(spacing, Is.EqualTo(48).Within(2));
        await Button(observer, StopLabel).ClickAsync();
        await Assertions.Expect(observer.Locator("#action-feedback")).ToHaveTextAsync("Führung beendet.");
        await observer.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.name === 'Nela' && npc.motion?.route.at(-1)?.x === 66)");
        await observer.WaitForFunctionAsync($"!{Game}.room.npcs.some(npc => npc.name === 'Nela')");
        Assert.That(await observer.EvaluateAsync<string>($"{Game}.room.id"), Is.EqualTo("garden"));
        await guest.WaitForFunctionAsync($"{Game}.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 112 && !npc.motion?.route.length)");
        await TalkToGuide(guest, 112, 234);
        await Assertions.Expect(Button(guest, StartLabel)).ToBeVisibleAsync();
    }

    private static TestServerFixture CreateDoorTour()
    {
        var server = CreateServer();
        var world = Path.Combine(server.TempRoot, "world");
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        File.Copy(Path.Combine(reference, "assets/images/bruno.png"), Path.Combine(world, "assets/images/bruno.png"));
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.8.0", "1.14.0", StringComparison.Ordinal) + """

            defaultCharacterId: bruno
            characters:
              - { id: bruno, name: Bruno, image: assets/images/bruno.png, frameWidth: 128, frameHeight: 128 }
            """);
        var hall = Path.Combine(world, "rooms/hall.yaml");
        var serializer = new RoomYamlSerializer();
        var room = serializer.Deserialize(File.ReadAllText(hall) + """

            walkSpeed: 65
            npcs:
              - id: guide
                name: Bruno
                characterId: bruno
                position: { x: 100, y: 300 }
                talkPoint: { x: 145, y: 300 }
                tour:
                  - { position: { x: 220, y: 300 }, text: Hier geht es zur Tür. }
                  - { exit: next }
                  - { position: { x: 140, y: 300 } }
                options:
                  - { id: hello, text: Hallo, reply: Willkommen }
                  - { id: bye, text: Auf Wiedersehen, reply: Bis bald }
            """);
        var door = room.Objects!.Single(obj => obj.Id == "door").Interaction!;
        door.InitialState = "open";
        door.States.Single(state => state.Id == "closed").Obstacles = null;
        File.WriteAllText(hall, serializer.Serialize(room));
        return server;
    }

    private static ILocator Button(IPage page, string label) => page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });

    private static async Task<IPage> Join(IBrowser browser, string url, string name)
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(url);
        await page.FillAsync("#player-name", name);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync($"{Game}?.connectionStatus === 'connected'");
        return page;
    }

    private static async Task TalkToGuide(IPage page, double x, double y)
    {
        await Button(page, "Spreche mit").ClickAsync();
        await ClickCanvas(page, x, y);
        await Assertions.Expect(page.Locator("#npc-dialogue")).ToBeVisibleAsync();
    }
}

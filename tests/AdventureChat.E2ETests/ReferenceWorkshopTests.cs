using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;

namespace AdventureChat.E2ETests;

public class ReferenceWorkshopTests
{
    internal static readonly (int X, int Y) ChairAtTable = (169, 194);
    internal static readonly (int X, int Y) ReturnDoor = (49, 68);
    internal static readonly (int X, int Y) SwitchPanel = (286, 74);
    internal static readonly (int X, int Y) GateOpening = (370, 78);
    internal static readonly (int X, int Y) GateBeam = (370, 28);
    internal static readonly (int X, int Y) WorkshopEntrance = (472, 242);

    [Test]
    public async Task TwoPlayers_ReverseWorkshopActionsAndCrossTheChangedPassages()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var mara = await JoinWorkshop(browser, server.BaseUrl, "mara");
        var bruno = await JoinWorkshop(browser, server.BaseUrl, "bruno");
        await mara.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await Verb(mara, "Schließen", ReturnDoor);
        await ExpectObjectImage(bruno, "door", "closed");
        Assert.That(await TryWalk(bruno, 49, 118), Is.False, "Closed return door blocks its sill.");
        Assert.That(await TryWalk(bruno, 168, 205), Is.False, "Chair at the table occupies its footprint.");
        await Walk(bruno, 106, 236);
        await Capture(mara, "initial");

        await ClickCanvas(bruno, 222, 230);
        await WaitAt(bruno, 222, 230);
        await Verb(mara, "Ziehen", ChairAtTable);
        await mara.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text.includes('überdecken')");
        await ExpectObjectX(mara, "chair", 148);
        Assert.That(await bruno.EvaluateAsync<string?>("window.__ADVENTURE_GAME__.character.speechBubble?.text ?? null"), Is.Null);
        await Walk(bruno, 120, 261);
        await Verb(mara, "Ziehen", ChairAtTable);
        await ExpectObjectX(bruno, "chair", 204);
        await Walk(bruno, 168, 205);
        Assert.That(await TryWalk(bruno, 222, 230), Is.False, "Pulled chair occupies its other footprint.");
        await Walk(bruno, 120, 261);

        await Verb(mara, "Öffnen", ReturnDoor);
        await ExpectObjectImage(bruno, "door", "open");
        await Capture(mara, "open-and-pulled");
        await Verb(bruno, "Drücken", (224, 215));
        await ExpectObjectX(mara, "chair", 148);

        await Walk(bruno, 49, 118);
        await Capture(bruno, "door-threshold");
        await FollowExit(bruno, ReturnDoor, "garden");
        await FollowExit(bruno, WorkshopEntrance, "workshop");
        await Walk(bruno, 545, 320);
        Assert.That(await TryWalk(bruno, 49, 118), Is.True);
        await bruno.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'walk'");
        await Verb(mara, "Schließen", ReturnDoor);
        await ExpectObjectImage(bruno, "door", "closed");
        Assert.That(await bruno.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.room.exits.some(exit => exit.id === 'to-garden')"), Is.False);
        await bruno.WaitForFunctionAsync("""
            () => {
              const actor = window.__ADVENTURE_GAME__.character;
              return actor.state === 'idle' && actor.position.x > 69 && actor.speechBubble?.text.includes('versperrt');
            }
            """);
        Assert.That(await TryWalk(bruno, 389, 115), Is.False, "Closed gate blocks the sill.");

        await Verb(bruno, "Öffnen", ReturnDoor);
        await ExpectObjectImage(mara, "door", "open");
        await Verb(mara, "Benutzen", SwitchPanel);
        await ExpectGate(bruno, true);
        await ClickCanvas(bruno, 389, 115);
        await WaitAt(bruno, 389, 115);
        Assert.That(await bruno.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.id"), Is.EqualTo("workshop"));
        await Verb(mara, "Benutzen", SwitchPanel);
        await mara.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text.includes('überdecken')");
        await ExpectGate(mara, true);
        await ExpectGate(bruno, true);
        await Walk(bruno, 389, 159);
        await Capture(mara, "gate-open");
        await Verb(mara, "Benutzen", SwitchPanel);
        await ExpectGate(bruno, false);
        await Verb(mara, "Benutzen", SwitchPanel);
        await ExpectGate(bruno, true);
        await Verb(bruno, "Schließen", GateBeam);
        await ExpectGate(mara, false);
        await Verb(mara, "Benutzen", SwitchPanel);
        await ExpectGate(bruno, true);
        await FollowExit(bruno, GateOpening, "garden");
        Assert.That(await TryWalk(bruno, 420, 284), Is.False, "The garden door meets the wall on its left side.");
        Assert.That(await TryWalk(bruno, 472, 284), Is.False, "The garden threshold is in front of the closed door.");
        await Capture(bruno, "garden-entrance");
        await FollowExit(bruno, WorkshopEntrance, "workshop");
        await ExpectGate(bruno, true);
        await FollowExit(bruno, ReturnDoor, "garden");
        await FollowExit(bruno, WorkshopEntrance, "workshop");

        await mara.FillAsync("#chat-input", "Das Gartentor ist offen");
        await mara.ClickAsync("#chat-send");
        await Assertions.Expect(bruno.Locator("#chat-messages")).ToContainTextAsync("Das Gartentor ist offen");
    }

    [Test]
    public async Task WorkshopState_SurvivesLaterEntryAndReconnectAndResetsWhenTheWorldRestarts()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var mara = await JoinWorkshop(browser, server.BaseUrl, "mara");
        await Verb(mara, "Ziehen", ChairAtTable);
        await ExpectObjectX(mara, "chair", 204);
        await Verb(mara, "Schließen", ReturnDoor);
        await ExpectObjectImage(mara, "door", "closed");
        await Verb(mara, "Benutzen", SwitchPanel);
        await ExpectGate(mara, true);
        await FollowExit(mara, GateOpening, "garden");
        await mara.CloseAsync();

        var bruno = await JoinWorkshop(browser, proxy.BaseUrl, "bruno");
        await ExpectChangedState(bruno);
        var session = await bruno.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        proxy.Interrupt();
        await bruno.WaitForFunctionAsync("""
            session => {
              const game = window.__ADVENTURE_GAME__;
              return game?.connectionStatus === 'connected' && game.sessionId !== session && game.room.id === 'lounge';
            }
            """, session);
        await FollowGardenRoute(bruno);
        await ExpectChangedState(bruno);
        await bruno.CloseAsync();

        await server.StopAsync();
        await server.StartAsync();
        var fresh = await JoinWorkshop(browser, server.BaseUrl, "mara");
        await ExpectObjectImage(fresh, "door", "open");
        await ExpectObjectX(fresh, "chair", 148);
        await ExpectGate(fresh, false);
    }

    private static async Task ExpectChangedState(IPage page)
    {
        await ExpectObjectImage(page, "door", "closed");
        await ExpectObjectX(page, "chair", 204);
        await ExpectGate(page, true);
    }

    internal static TestServerFixture CreateReferenceCopy()
    {
        var server = new TestServerFixture(authoring: true, externalWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "../../../../../worlds/reference"));
        var files = Directory.GetFiles(Path.Combine(source, "rooms"), "*.yaml")
            .Concat(Directory.GetFiles(Path.Combine(source, "assets/images"), "*.png"))
            .Append(Path.Combine(source, "world.yaml"));
        foreach (var file in files)
        {
            var destination = Path.Combine(server.TempRoot, "world", Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        return server;
    }

    internal static async Task<IPage> JoinWorkshop(IBrowser browser, string url, string character)
    {
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await page.GotoAsync(url);
        await page.FillAsync("#player-name", character);
        await page.WaitForFunctionAsync("!document.querySelector('#world-enter').disabled");
        await page.SelectOptionAsync("#character-select", character);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.room.id === 'lounge'");
        await FollowGardenRoute(page);
        return page;
    }

    private static async Task FollowGardenRoute(IPage page)
    {
        await FollowExit(page, (572, 215), "garden");
        await FollowExit(page, WorkshopEntrance, "workshop");
    }

    private static async Task FollowExit(IPage page, (int X, int Y) target, string destination)
    {
        await ClickCanvas(page, target.X, target.Y);
        await page.WaitForFunctionAsync("room => window.__ADVENTURE_GAME__.room.id === room", destination);
    }

    private static Task<bool> TryWalk(IPage page, int x, int y) => page.EvaluateAsync<bool>(
        "async point => (await window.__ADVENTURE_GAME__.setTarget(point)).accepted", new { x, y });

    private static async Task Walk(IPage page, int x, int y)
    {
        Assert.That(await TryWalk(page, x, y), Is.True, $"Walk to ({x},{y}).");
        await WaitAt(page, x, y);
    }

    private static Task WaitAt(IPage page, int x, int y) => page.WaitForFunctionAsync("""
            point => {
              const actor = window.__ADVENTURE_GAME__.character;
              return actor.state === 'idle' && Math.hypot(actor.position.x - point.x, actor.position.y - point.y) < 1.5;
            }
            """, new { x, y });

    internal static async Task Verb(IPage page, string name, (int X, int Y) target)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true }).ClickAsync();
        await ClickCanvas(page, target.X, target.Y);
    }

    internal static Task ExpectObjectX(IPage page, string id, int x) => page.WaitForFunctionAsync(
        "({id, x}) => window.__ADVENTURE_GAME__.room.objects.find(object => object.id === id)?.position.x === x", new { id, x });

    private static Task ExpectObjectImage(IPage page, string id, string state) => page.WaitForFunctionAsync(
        "({id, state}) => window.__ADVENTURE_GAME__.room.objects.find(object => object.id === id)?.image.endsWith(`-${state}.png`)",
        new { id, state });

    internal static Task ExpectGate(IPage page, bool open) => page.WaitForFunctionAsync("""
        open => {
          const room = window.__ADVENTURE_GAME__.room;
          const image = id => room.objects.find(object => object.id === id)?.image;
          return room.exits.some(exit => exit.id === 'through-gate') === open &&
            image('gate')?.endsWith(open ? '-open.png' : '-closed.png') &&
            image('switch')?.endsWith(open ? '-on.png' : '-off.png');
        }
        """, open);

    internal static Task Capture(IPage page, string state) => page.ScreenshotAsync(new()
    {
        Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-workshop-{state}.png")
    });
}

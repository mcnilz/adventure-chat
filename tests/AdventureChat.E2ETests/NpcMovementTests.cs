using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcMovementTests
{
    private const string Npc = "window.__ADVENTURE_GAME__.room.npcs?.find(n => n.id === 'ida')";

    [Test]
    public async Task VisitorsSeeTheSamePatrolAndItPausesUntilBothConversationsEnd()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await EnterGarden(browser, server.BaseUrl);
        await WaitForStop(first, 184);
        await WaitForStop(first, 126);
        var arrivedAt = await WaitForStop(first, 210);
        var startedAt = await first.WaitForFunctionAsync($"({Npc})?.motion?.route.length > 1 && performance.now()");
        Assert.That(await startedAt.JsonValueAsync<double>() - arrivedAt, Is.InRange(800, 1300));
        await Talk(first);
        var paused = await first.EvaluateAsync<double>($"({Npc}).position.x");
        Assert.That(paused, Is.GreaterThan(126));

        var second = await Join(browser, server.BaseUrl);
        await second.EvaluateAsync("""
            () => {
              const game = window.__ADVENTURE_GAME__;
              const previous = game.onRoomChanged;
              game.onRoomChanged = room => {
                if (room.id === 'garden') window.idaAtEntry = room.npcs.find(npc => npc.id === 'ida').position.x;
                previous?.(room);
              };
            }
            """);
        await GoToGarden(second);
        Assert.That(await second.EvaluateAsync<double>("window.idaAtEntry"), Is.EqualTo(paused).Within(1));
        await Talk(second);
        await Capture(first, "garden-conversation");

        await first.Keyboard.PressAsync("Escape");
        await Assertions.Expect(first.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await Assertions.Expect(second.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await Task.Delay(1200);
        Assert.That(await first.EvaluateAsync<double>($"({Npc}).position.x"), Is.EqualTo(paused).Within(1));
        Assert.That(await second.EvaluateAsync<double>($"({Npc}).position.x"), Is.EqualTo(paused).Within(1));

        await second.Keyboard.PressAsync("Escape");
        await first.WaitForFunctionAsync($"Math.abs(({Npc}).position.x - {paused.ToString(System.Globalization.CultureInfo.InvariantCulture)}) > 5");
    }

    [Test]
    public async Task UnreachableWaypointKeepsTheNpcOnSafeGroundUntilThePassageOpens()
    {
        await using var server = CreateServer();
        var world = Path.Combine(server.TempRoot, "world");
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        File.Copy(Path.Combine(reference, "assets/images/bruno.png"), Path.Combine(world, "assets/images/bruno.png"));
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.8.0", "1.13.0", StringComparison.Ordinal) + """

            defaultCharacterId: bruno
            characters:
              - { id: bruno, name: Bruno, image: assets/images/bruno.png, frameWidth: 128, frameHeight: 128 }
            """);
        var hall = Path.Combine(world, "rooms/hall.yaml");
        File.AppendAllText(hall, """

            walkSpeed: 65
            npcs:
              - id: ida
                name: Ida
                characterId: bruno
                position: { x: 100, y: 300 }
                talkPoint: { x: 120, y: 300 }
                waypoints: [{ x: 500, y: 300 }, { x: 100, y: 300 }]
                options:
                  - { id: hello, text: Hallo, reply: Willkommen }
                  - { id: bye, text: Auf Wiedersehen, reply: Bis bald }
            """);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await Join(browser, server.BaseUrl);
        var second = await Join(browser, server.BaseUrl);
        Assert.That(await first.EvaluateAsync<double>($"({Npc}).position.x"), Is.EqualTo(100));

        await UseDoor(first, 290);
        await second.WaitForFunctionAsync($"({Npc}).position.x > 130");
        await UseDoor(first, 340);
        await second.WaitForFunctionAsync($"({Npc}).position.x > 130 && !({Npc}).motion.route.length");
        var stopped = await second.EvaluateAsync<double>($"({Npc}).position.x");
        Assert.That(stopped, Is.LessThan(280));
        await Task.Delay(300);
        Assert.That(await first.EvaluateAsync<double>($"({Npc}).position.x"), Is.EqualTo(stopped));
        Assert.That(await second.EvaluateAsync<double>($"({Npc}).position.x"), Is.EqualTo(stopped));

        await UseDoor(first, 290);
        await second.WaitForFunctionAsync($"({Npc}).position.x === 500 && !({Npc}).motion.route.length");
    }

    [Test]
    public async Task PreviewPatrolMovesAndItsConversationDoesNotPauseTheLiveNpc()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var live = await EnterGarden(browser, server.BaseUrl);
        var editor = await browser.NewPageAsync();
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await editor.SelectOptionAsync("#room-select", "garden");
        await Assertions.Expect(editor.Locator("#input-id")).ToHaveValueAsync("garden");
        await Assertions.Expect(editor.Locator("#status-banner")).ToHaveTextAsync("Raum 'Tavernen-Garten' geladen.");
        var firstSnapshot = editor.WaitForResponseAsync(IsPreviewSnapshot);
        await editor.ClickAsync("#btn-toggle-mode");
        var initial = await (await firstSnapshot).JsonAsync();
        Assert.That(initial!.Value.GetProperty("snapshot").GetProperty("roomId").GetString(), Is.EqualTo("garden"));
        Assert.That(initial.Value.GetProperty("patrols").GetProperty("checkpoints").GetArrayLength(), Is.EqualTo(1));
        IResponse nextSnapshot;
        try { nextSnapshot = await editor.WaitForResponseAsync(IsPreviewSnapshot, new() { Timeout = 5000 }); }
        catch (TimeoutException error)
        {
            throw new AssertionException($"No second garden snapshot. Initial: {initial}. Preview status: {await editor.Locator("#preview-result").TextContentAsync()}", error);
        }
        var json = await nextSnapshot.JsonAsync();
        var position = json!.Value.GetProperty("snapshot").GetProperty("room")
            .GetProperty("npcs")[0].GetProperty("position");
        Assert.That(position.GetProperty("x").GetDouble(), Is.GreaterThan(160));

        await editor.SelectOptionAsync("#preview-verb", "talk");
        await ClickCanvas(editor, position.GetProperty("x").GetDouble(), 265);
        await Assertions.Expect(editor.Locator("#npc-dialogue-name")).ToHaveTextAsync("Ida");
        await Assertions.Expect(editor.Locator("#npc-dialogue")).ToBeVisibleAsync();
        var livePosition = await live.EvaluateAsync<double>($"({Npc}).position.x");
        await live.WaitForFunctionAsync($"initial => Math.abs(({Npc}).position.x - initial) > 8", livePosition);
        await Assertions.Expect(editor.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await Capture(editor, "garden-preview");
        await editor.RunAndWaitForResponseAsync(() => editor.Keyboard.PressAsync("Escape"), IsPreviewSnapshot);
    }

    private static async Task<double> WaitForStop(IPage page, int x)
    {
        var stopped = await page.WaitForFunctionAsync($"({Npc})?.position.x === {x} && !({Npc})?.motion?.route.length && performance.now()");
        return await stopped.JsonValueAsync<double>();
    }

    private static async Task<IPage> EnterGarden(IBrowser browser, string url)
    {
        var page = await Join(browser, url);
        await GoToGarden(page);
        return page;
    }

    private static async Task GoToGarden(IPage page)
    {
        await ClickCanvas(page, 572, 215);
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.room.id === 'garden'");
    }

    private static async Task<IPage> Join(IBrowser browser, string url)
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(url);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        return page;
    }

    private static bool IsPreviewSnapshot(IResponse response) => response.Ok && response.Url.Contains("/preview-snapshot");

    private static async Task UseDoor(IPage page, int x)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickCanvas(page, x, 250);
        await Assertions.Expect(page.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.");
    }

    private static async Task Talk(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Spreche mit", Exact = true }).ClickAsync();
        var point = await page.EvaluateAsync<double[]>($"[({Npc}).position.x, ({Npc}).position.y - 52]");
        await DynamicNavigationTests.ClickCanvas(page, point[0], point[1]);
        await Assertions.Expect(page.Locator("#npc-dialogue-name")).ToHaveTextAsync("Ida");
        await Assertions.Expect(page.Locator("#npc-dialogue")).ToBeVisibleAsync();
    }

    private static async Task Capture(IPage page, string name)
    {
        if (Environment.GetEnvironmentVariable("NPC_PATROL_EVIDENCE") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png") });
    }
}

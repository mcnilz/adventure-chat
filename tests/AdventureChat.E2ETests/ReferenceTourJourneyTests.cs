using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class ReferenceTourJourneyTests
{
    private static TestServerFixture CreateServer()
    {
        var server = new TestServerFixture(authoring: true, externalWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(server.TempRoot, "world", Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        var manifest = Path.Combine(server.TempRoot, "world/world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("startRoom: lounge", "startRoom: tour-map"));
        return server;
    }

    private static async Task Walk(IPage page, int x, int y)
    {
        Assert.That(await page.EvaluateAsync<bool>(
            "async p => (await window.__ADVENTURE_GAME__.setTarget(p)).accepted", new { x, y }), Is.True);
        await page.WaitForFunctionAsync("p => {const c=window.__ADVENTURE_GAME__.character; return c.state==='idle' && Math.hypot(c.position.x-p.x,c.position.y-p.y)<2}", new { x, y });
    }

    private static async Task Next(IPage page, string room)
    {
        Assert.That(await page.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('next')"), Is.True);
        await page.WaitForFunctionAsync("id=>window.__ADVENTURE_GAME__.room.id===id", room);
    }

    private static Task Capture(IPage page, string name) => page.ScreenshotAsync(new()
    {
        Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reference-tour-" + name + ".png")
    });

    [Test]
    public async Task TwoParticipants_WalkChatScrollAndCompleteTheReferenceTour()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        var b = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 800, Height = 700 } });
        foreach (var (page, id) in new[] { (a, "mara"), (b, "bruno") })
        {
            await page.GotoAsync(server.BaseUrl);
            await page.WaitForFunctionAsync("!document.querySelector('#world-enter').disabled");
            await page.FillAsync("#player-name", id);
            await page.SelectOptionAsync("#character-select", id);
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus==='connected'");
        }
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        await Capture(a, "map-left");
        await Walk(a, 650, 90);
        await Capture(a, "map-middle");
        await Walk(a, 1220, 180);
        await Capture(a, "map-right");
        // At the right camera boundary x=640; exercise an actual pointer event after scrolling.
        var canvas = a.Locator("#game-canvas");
        var bounds = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = bounds.Width * 460 / 640, Y = bounds.Height * 165 / 360 } });
        await a.WaitForFunctionAsync("Math.hypot(window.__ADVENTURE_GAME__.character.position.x-1100,window.__ADVENTURE_GAME__.character.position.y-165)<2");
        await a.FillAsync("#chat-input", "Auf dem neuen Kartenweg");
        await a.ClickAsync("#chat-send");
        await b.WaitForFunctionAsync("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].speechBubble?.text==='Auf dem neuen Kartenweg'");
        await Walk(a, 1220, 180);
        await Next(a, "tour-depth");
        await Walk(a, 320, 140);
        await Capture(a, "depth-back");
        await Walk(a, 570, 285);
        await Next(a, "tour-combined");
        await Walk(a, 225, 140);
        await Capture(a, "panorama-left");
        await Walk(a, 600, 325);
        await Capture(a, "panorama-middle");
        await Walk(a, 990, 140);
        await Capture(a, "panorama-right");
        await Walk(a, 1130, 300);
        await Next(a, "tour-map");
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.appearance.id"), Is.EqualTo("mara"));
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('back')"), Is.True);
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id==='garden'");
    }

    [Test]
    public async Task Editor_PreservesTourArtAndGeometryThroughSaveReloadAndPreview()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await page.WaitForFunctionAsync("window.__ROOM_EDITOR__ !== undefined");
        foreach (var (id, x, y) in new[] { ("tour-map", 330, 220), ("tour-depth", 320, 140), ("tour-combined", 225, 140) })
        {
            await page.EvaluateAsync("id=>window.__ROOM_EDITOR__.loadRoom(id)", id);
            var before = await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
            Assert.That(await page.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"), Is.True);
            await page.ReloadAsync();
            await page.WaitForFunctionAsync("window.__ROOM_EDITOR__ !== undefined");
            await page.EvaluateAsync("id=>window.__ROOM_EDITOR__.loadRoom(id)", id);
            Assert.That(await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(before));
            await page.ClickAsync("#btn-toggle-mode");
            var response = page.WaitForResponseAsync(r => r.Url.Contains("/api/editor/preview-path") && r.Request.Method == "POST");
            var canvas = page.Locator("#editor-canvas");
            var bounds = (await canvas.BoundingBoxAsync())!;
            await canvas.ClickAsync(new() { Position = new() { X = bounds.Width * x / 640, Y = bounds.Height * y / 360 } });
            Assert.That((await response).Ok, Is.True);
            await Capture(page, "editor-" + id);
            await page.ClickAsync("#btn-toggle-mode");
        }
    }
}

using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class CharacterLifecycleTests
{
    [Test]
    public async Task Reconnect_PreservesNameAndFigureWithFreshSession()
    {
        await using var server = new TestServerFixture(characterWorld: true);
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(proxy.BaseUrl);
        await page.FillAsync("#player-name", "Reconnect Mara");
        await page.WaitForFunctionAsync("!document.querySelector('#world-enter').disabled");
        await page.SelectOptionAsync("#character-select", "tall"); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var oldSession = await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        Assert.That(await page.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('door')"), Is.True);
        proxy.Interrupt();
        await page.WaitForFunctionAsync("id => window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== id", oldSession);
        Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.name"), Is.EqualTo("Reconnect Mara"));
        Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.appearance.id"), Is.EqualTo("tall"));
        Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.id"), Is.EqualTo("hall"));
    }

    [Test]
    public async Task EditorPreview_UsesCatalogDefaultAndWorldResources()
    {
        await using var server = new TestServerFixture(characterWorld: true, authoring: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl + "/editor.html?worldId=characters-a");
        await page.WaitForFunctionAsync("window.__ROOM_EDITOR__ !== undefined");
        await page.SelectOptionAsync("#room-select", "hall"); await page.ClickAsync("#btn-toggle-mode");
        await page.WaitForFunctionAsync("""
            () => {
              const pixel = document.querySelector('#editor-canvas').getContext('2d').getImageData(100,270,1,1).data;
              return pixel[0] === 30 && pixel[1] === 160 && pixel[2] === 200;
            }
            """);
        var response = page.WaitForResponseAsync(r => r.Url.Contains("/api/editor/preview-path") && r.Request.Method == "POST");
        var canvas = page.Locator("#editor-canvas"); var bounds = await canvas.BoundingBoxAsync();
        await canvas.ClickAsync(new() { Position = new() { X = bounds!.Width * 200 / 640, Y = bounds.Height * 300 / 360 } });
        Assert.That((await response).Ok, Is.True);
    }

    [Test]
    public async Task MissingPreviewImage_ShowsMessageAndGameKeepsRunning()
    {
        await using var server = new TestServerFixture(characterWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("**/world-assets/**/images/small.png", r => r.AbortAsync());
        await page.GotoAsync(server.BaseUrl);
        await page.GetByText("Vorschaubild vorübergehend nicht verfügbar.").WaitForAsync();
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await page.EvaluateAsync<bool>("(async () => (await window.__ADVENTURE_GAME__.setTarget({x:200,y:300})).accepted)()"), Is.True);
    }
}

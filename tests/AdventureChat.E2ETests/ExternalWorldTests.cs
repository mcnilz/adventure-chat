using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class ExternalWorldTests
{
    [Test]
    public async Task ExternalWorld_CanEnterEditAndReenterAfterRestart()
    {
        await using var server = new TestServerFixture(authoring: true, externalWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.room.id === 'garden' && window.__ADVENTURE_GAME__?.sessionId !== 'local-player'");
        var imageStatus = await page.EvaluateAsync<int>("async () => (await fetch('/assets/images/garden.png')).status");
        Assert.That(imageStatus, Is.EqualTo(200));
        var x = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        Assert.That(x, Is.EqualTo(80));
        var editor = await context.NewPageAsync();
        await editor.GotoAsync(server.BaseUrl + "/editor.html");
        await editor.ClickAsync("#world-enter");
        await editor.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");
        await editor.WaitForFunctionAsync("() => document.getElementById('status-banner')?.textContent?.includes('geladen.') === true");
        var loaded = editor.WaitForResponseAsync(response => response.Url.Contains("/api/editor/rooms/garden?worldId=reference") && response.Status == 200);
        await editor.SelectOptionAsync("#room-select", "garden");
        await loaded;
        await editor.WaitForFunctionAsync("() => document.querySelector('#input-notes')?.value === 'Der lauschige Gartenbereich hinter der Taverne.'");
        await editor.FillAsync("#input-notes", "Externe Welt gespeichert");
        var saveRequest = editor.WaitForRequestAsync(request => request.Method == "POST" && request.Url.Contains("/api/editor/rooms/garden?worldId=reference"));
        await editor.ClickAsync("#btn-save");
        Assert.That((await saveRequest).PostData, Does.Contain("Externe Welt gespeichert"));
        await editor.WaitForFunctionAsync("() => document.getElementById('status-banner')?.textContent?.includes('atomar gespeichert') === true");
        Assert.That(File.ReadAllText(Path.Combine(server.TempRoot, "world", "rooms", "garden.yaml")), Does.Contain("Externe Welt gespeichert"));
        var manifest = Path.Combine(server.TempRoot, "world", "world.yaml");
        Assert.That(File.ReadAllText(manifest), Does.Contain("startRoom: garden"));
        await server.StopAsync();
        await server.StartAsync();
        await page.GotoAsync(server.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.room.id === 'garden' && window.__ADVENTURE_GAME__?.sessionId !== 'local-player'");
        Assert.That(await page.EvaluateAsync<string>("async () => (await (await fetch('/api/rooms/garden')).json()).notes"), Is.EqualTo("Externe Welt gespeichert"));
    }
}

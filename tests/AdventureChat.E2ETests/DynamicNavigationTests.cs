using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class DynamicNavigationTests
{
    [Test]
    public async Task TwoPlayers_SeeAtomicDoorChangeAndStoppedRouteWithLocalFeedback()
    {
        await using var server = CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(); var b = await browser.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        await SaveEvidence(a, "closed");
        Assert.That((await a.EvaluateAsync<JsonElement>("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})")).GetProperty("accepted").GetBoolean(), Is.False);
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickCanvas(a, 290, 250);
        foreach (var page in new[] { a, b })
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 330 && window.__ADVENTURE_GAME__.room.exits.length === 1");
        await b.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        await SaveEvidence(b, "open");
        await b.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:100,y:300})");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk' && window.__ADVENTURE_GAME__.character.position.x < 470");
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickCanvas(a, 340, 250);
        try { await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.room.exits.length === 0 && window.__ADVENTURE_GAME__.character.speechBubble?.text.includes('versperrt')", null, new() { Timeout = 5000 }); }
        catch (TimeoutException)
        {
            Assert.Fail("A: " + await a.EvaluateAsync<string>("JSON.stringify({p:window.__ADVENTURE_GAME__.character,exits:window.__ADVENTURE_GAME__.room.exits,objects:window.__ADVENTURE_GAME__.room.objects})") + " B: " + await b.EvaluateAsync<string>("JSON.stringify(window.__ADVENTURE_GAME__.character)"));
        }
        var stoppedX = await b.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x");
        Assert.That(stoppedX, Is.GreaterThan(320));
        await SaveEvidence(b, "stopped");
        Assert.That(await a.EvaluateAsync<string?>("window.__ADVENTURE_GAME__.character.speechBubble?.text ?? null"), Is.Null);
        Assert.That(await b.Locator("#chat-messages").InnerTextAsync(), Does.Not.Contain("versperrt"));
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickCanvas(a, 290, 250);
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.exits.length === 1");
        Assert.That(await b.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(stoppedX));
        await b.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        Assert.That(await b.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('next')"), Is.True);
        Assert.That(await b.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('back')"), Is.True);
        Assert.That(await b.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.exits.length"), Is.EqualTo(1));
    }

    internal static TestServerFixture CreateServer()
    {
        var server = new TestServerFixture(authoring: true, foregroundWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/navigation"));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(server.TempRoot, "world", Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
        }
        return server;
    }

    internal static async Task ClickCanvas(IPage page, double x, double y)
    {
        var canvas = page.Locator("canvas").First;
        var box = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = (float)(x * box.Width / 640), Y = (float)(y * box.Height / 360) } });
    }

    private static async Task SaveEvidence(IPage page, string name)
    {
        var directory = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../.scratch/adventure-chat/evidence/21"));
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
}

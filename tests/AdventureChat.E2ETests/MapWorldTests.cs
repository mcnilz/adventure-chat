using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class MapWorldTests
{
    [Test]
    public async Task ReferenceFigure_RendersFourDirectionsAndFixedSizesAtFootpoint()
    {
        await using var server = new TestServerFixture(mapWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        var measurements = await page.EvaluateAsync<int[]>("""
            async () => {
                const { CharacterRenderer } = await import('/js/rendering/character-renderer.js');
                const renderer = new CharacterRenderer();
                const canvas = document.createElement('canvas'); canvas.width = 200; canvas.height = 200;
                const ctx = canvas.getContext('2d');
                const widths = [];
                const pictures = [];
                for (const height of [32, 48]) {
                    for (const facing of ['up', 'right', 'down', 'left']) {
                        const frames = [];
                        for (const state of ['idle', 'walk']) {
                            ctx.clearRect(0, 0, 200, 200);
                            renderer.drawCharacter(ctx, {position:{x:100,y:100}, facing, state, walkFrame:0, name:'Tester'}, true,
                                {perspective:'top-down',actorHeight:height});
                            const pixels = ctx.getImageData(0,0,200,200).data;
                            const xs = [], ys = [];
                            for (let y=0; y<200; y++) for (let x=0; x<200; x++) {
                                const i=(y*200+x)*4;
                                if (pixels[i]===56 && pixels[i+1]===123 && pixels[i+2]===135) { xs.push(x); ys.push(y); }
                            }
                            if (!xs.length || Math.max(...ys)>=100 || Math.min(...ys)<100-height) throw new Error('Figure lost footpoint');
                            if (facing==='down' && state==='idle') widths.push(Math.max(...xs)-Math.min(...xs)+1);
                            frames.push(canvas.toDataURL());
                        }
                        if (frames[0]===frames[1]) throw new Error('Walking frame matches standing frame');
                        pictures.push(frames[0]);
                    }
                }
                if (new Set(pictures).size!==8) throw new Error('Directions or sizes are indistinguishable');
                return widths;
            }
            """);
        Assert.That(measurements, Is.EqualTo(new[] { 20, 30 }));
    }

    [Test]
    public async Task Map_TwoBrowsersMoveChatAndReturnFromAdventureRoom()
    {
        await using var server = new TestServerFixture(mapWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var first = await browser.NewContextAsync();
        await using var second = await browser.NewContextAsync();
        var a = await first.NewPageAsync();
        var b = await second.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl);
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        Assert.That(await a.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.perspective"), Is.EqualTo("top-down"));
        Assert.That(await a.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.speed"), Is.EqualTo(100));
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:110,y:300})");
        await b.WaitForFunctionAsync("() => [...window.__ADVENTURE_GAME__.remotePlayers.values()][0].position.x > 80");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'");
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:65,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'");
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.changeRoom('to-lounge')");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'lounge'");
        Assert.That(await a.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.speed"), Is.EqualTo(130));
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.changeRoom('to-map')");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'map'");
        await a.FillAsync("#chat-input", "Hallo Karte");
        await a.PressAsync("#chat-input", "Enter");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === 'Hallo Karte')");
        await a.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "map-world.png") });
    }

    [Test]
    public async Task Editor_SavesDisplaySettingsAndReloads()
    {
        await using var server = new TestServerFixture(authoring: true, mapWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl + "/editor.html");
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => document.querySelector('#input-id').value === 'garden'");
        await page.SelectOptionAsync("#room-select", "map");
        await page.WaitForFunctionAsync("() => document.querySelector('#input-actor-height').value === '32'");
        await page.FillAsync("#input-actor-height", "48");
        await page.Locator("#input-actor-height").BlurAsync();
        await page.FillAsync("#input-walk-speed", "80");
        await page.Locator("#input-walk-speed").BlurAsync();
        await page.ClickAsync("#btn-save");
        await page.WaitForFunctionAsync("() => document.querySelector('#status-banner').textContent.includes('atomar gespeichert')");
        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#input-id').value === 'garden'");
        await page.SelectOptionAsync("#room-select", "map");
        await page.WaitForFunctionAsync("() => document.querySelector('#input-actor-height').value === '48'");
        Assert.That(await page.InputValueAsync("#input-walk-speed"), Is.EqualTo("80"));
        Assert.That(await page.InputValueAsync("#input-perspective"), Is.EqualTo("top-down"));
        await page.ClickAsync("#btn-toggle-mode");
        var routeResponse = page.WaitForResponseAsync(r => r.Url.Contains("/api/editor/preview-path") && r.Request.Method == "POST");
        var canvas = page.Locator("#editor-canvas");
        var bounds = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = bounds.Width * 570 / 640, Y = bounds.Height * 300 / 360 } });
        var response = await routeResponse;
        Assert.That(response.Ok, Is.True);
        var route = System.Text.Json.Nodes.JsonNode.Parse(await response.TextAsync())!;
        Assert.That(route["success"]!.GetValue<bool>(), Is.True);
        Assert.That(route["waypoints"]!.AsArray().Count, Is.GreaterThan(2));
        await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "map-editor.png") });
    }
}

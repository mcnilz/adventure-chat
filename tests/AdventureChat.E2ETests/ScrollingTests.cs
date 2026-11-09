using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class ScrollingTests
{
    [Test]
    public async Task Editor_PannedSpawnEditSavesRoomCoordinatesAndPreviewFollows()
    {
        await using var server = new TestServerFixture(authoring:true, scrollWorld:true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new(){Headless=true});
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl+"/editor.html");
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => document.querySelector('#input-id').value === 'gallery'");
        await page.FillAsync("#input-width", "1200");
        await page.Locator("#input-width").BlurAsync();
        Assert.That(await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().size.width"), Is.EqualTo(1200));
        await page.FillAsync("#input-width", "1280");
        await page.Locator("#input-width").BlurAsync();
        await page.Locator("#editor-camera-pan").FillAsync("640");
        var canvas = page.Locator("#editor-canvas");
        var box = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new(){Position=new(){X=box.Width*560/640,Y=box.Height*300/360}});
        await page.WaitForFunctionAsync("() => document.querySelector('#inspector-spawn-id').value === 'right'");
        Assert.That(double.Parse(await page.InputValueAsync("#inspector-spawn-x")),Is.EqualTo(1200).Within(1));
        await page.FillAsync("#inspector-spawn-x","1190");
        await page.Locator("#inspector-spawn-x").BlurAsync();
        await Assertions.Expect(page.Locator("#inspector-spawn-x")).ToHaveAttributeAsync("max", "1280");
        Assert.That(await page.Locator("#inspector-spawn-x").EvaluateAsync<bool>("input => input.checkValidity()"), Is.True);
        await page.FillAsync("#inspector-spawn-x", "1400");
        await Assertions.Expect(page.Locator("#inspector-spawn-x")).ToHaveValueAsync("1280");
        await page.FillAsync("#inspector-spawn-x", "-1");
        await Assertions.Expect(page.Locator("#inspector-spawn-x")).ToHaveValueAsync("0");
        await page.FillAsync("#inspector-spawn-x", "1190");
        await page.ClickAsync("#btn-save");
        await page.WaitForFunctionAsync("() => document.querySelector('#status-banner').textContent.includes('atomar gespeichert')");
        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#input-id').value === 'gallery'");
        Assert.That(await page.InputValueAsync("#input-viewport-width"),Is.EqualTo("640"));
        var saved = await page.EvaluateAsync<double>("() => window.__ROOM_EDITOR__.getState().getRoom().spawns.find(s=>s.id==='right').position.x");
        Assert.That(saved,Is.EqualTo(1190));
        await page.ClickAsync("#btn-toggle-mode");
        var responseTask = page.WaitForResponseAsync(r=>r.Url.Contains("/api/editor/preview-path") && r.Request.Method=="POST");
        box=(await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new(){Position=new(){X=box.Width*600/640,Y=box.Height*300/360}});
        Assert.That((await responseTask).Ok,Is.True);
        await page.WaitForTimeoutAsync(2500);
        await page.ScreenshotAsync(new(){Path=Path.Combine(TestContext.CurrentContext.WorkDirectory,"scroll-editor.png")});
        Assert.That(await page.Locator("#editor-camera-pan").IsDisabledAsync(),Is.True);
        await page.ClickAsync("#btn-toggle-mode");
        await page.ClickAsync("#btn-new-room");
        await page.FillAsync("#input-id", "new-gallery");
        await page.FillAsync("#input-bg", "assets/images/gallery.png");
        await page.FillAsync("#input-width", "1280");
        await page.FillAsync("#input-viewport-width", "640");
        await page.Locator("#input-viewport-width").BlurAsync();
        await page.ClickAsync("#btn-save");
        await page.WaitForFunctionAsync("() => document.querySelector('#status-banner').textContent.includes('atomar gespeichert')");
        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('#input-id').value === 'gallery'");
        await page.SelectOptionAsync("#room-select", "new-gallery");
        await page.WaitForFunctionAsync("() => document.querySelector('#input-width').value === '1280'");
        Assert.That(await page.InputValueAsync("#input-viewport-width"),Is.EqualTo("640"));
    }

    [Test]
    public async Task BroadRooms_TwoViewsChatDoorsAndReconnect()
    {
        await using var server = new TestServerFixture(scrollWorld: true);
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync();
        var b = await browser.NewPageAsync();
        foreach (var page in new[] {a,b})
        {
            await page.GotoAsync(page == a ? proxy.BaseUrl : server.BaseUrl);
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.sessionId !== 'local-player'");
        }
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:1200,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x > 1199");
        Assert.That(await a.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.screenToLogical(document.querySelector('canvas').getBoundingClientRect().left,0).x"), Is.EqualTo(640));
        Assert.That(await b.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.screenToLogical(document.querySelector('canvas').getBoundingClientRect().left,0).x"), Is.Zero);
        await a.FillAsync("#chat-input", "Hallo anderes Ende");
        await a.PressAsync("#chat-input", "Enter");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === 'Hallo anderes Ende')");
        // Real pointer input at the right door uses the shifted coordinates.
        var canvas = a.Locator("#game-canvas");
        var bounds = (await canvas.BoundingBoxAsync())!;
        await canvas.HoverAsync(new() {Position=new(){X=bounds.Width*595/640,Y=bounds.Height*280/360}});
        Assert.That(await a.Locator(".action-text").TextContentAsync(), Does.Contain("map"));
        await canvas.ClickAsync(new(){Position=new(){X=bounds.Width*595/640,Y=bounds.Height*280/360}});
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'map'");
        Assert.That(await a.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.perspective"), Is.EqualTo("top-down"));
        Assert.That(await a.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.screenToLogical(document.querySelector('canvas').getBoundingClientRect().left,0).x"), Is.EqualTo(640));
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:80,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x < 81");
        await a.ScreenshotAsync(new(){Path=Path.Combine(TestContext.CurrentContext.WorkDirectory,"scroll-map.png")});
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:45,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'");
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.changeRoom('left')");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'gallery'");
        await a.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:1200,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x > 1199");
        var oldSession = await a.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.sessionId");
        proxy.Interrupt();
        await a.WaitForFunctionAsync("oldId => window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== oldId && window.__ADVENTURE_GAME__.character.position.x === 80",oldSession);
        Assert.That(await a.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.screenToLogical(document.querySelector('canvas').getBoundingClientRect().left,0).x"), Is.Zero);
    }

    [Test]
    public async Task Camera_ClampsFollowsDeadZoneAndConvertsScaledInput()
    {
        await using var server = new TestServerFixture();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        var result = await page.EvaluateAsync<double[]>("""
            async () => {
                const { HorizontalCamera, screenToRoom } = await import('/js/rendering/camera.js');
                const camera = new HorizontalCamera();
                const room = {width:1600,height:360,viewport:{width:640,height:360,scrollMode:'horizontal'}};
                camera.reset(room, 800);
                const values = [camera.offset];
                camera.follow(room, 850); values.push(camera.offset);
                camera.follow(room, 1000); values.push(camera.offset);
                const point = screenToRoom(250, 100, {left:90,top:10,width:320,height:180},room,camera.offset);
                values.push(point.x,point.y);
                camera.follow(room,1600); values.push(camera.offset);
                camera.follow(room,0); values.push(camera.offset);
                camera.reset({...room,width:300},200); values.push(camera.offset);
                return values;
            }
            """);
        Assert.That(result, Is.EqualTo(new double[] {480,480,584,904,180,960,0,0}));
        var safeWidth = await page.EvaluateAsync<int>("""
            async () => {
                const { viewSize } = await import('/js/rendering/camera.js');
                return viewSize({width:1280,height:360,viewport:{width:0,height:360,scrollMode:'horizontal'}}).width;
            }
            """);
        Assert.That(safeWidth, Is.EqualTo(1280), "Invalid draft values must keep the editor usable until save validation.");
    }
}

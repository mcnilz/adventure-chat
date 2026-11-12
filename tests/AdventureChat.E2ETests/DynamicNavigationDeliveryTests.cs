using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class DynamicNavigationDeliveryTests
{
    [Test]
    public async Task NavigationSnapshot_CatchesUpThrottledAnimationAfterServerReachedTarget()
    {
        await using var server = DynamicNavigationTests.CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.AddInitScriptAsync("window.requestAnimationFrame = callback => window.setTimeout(() => callback(performance.now()), 1500)");
        await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page.EvaluateAsync("""
            async () => {
                window.controller = new signalR.HubConnectionBuilder().withUrl('/hub/room').build();
                await controller.start(); await controller.invoke('JoinRoom',{roomId:'hall',displayName:'Controller',worldId:'navigation'});
                await controller.invoke('RequestObjectAction',{roomId:'hall',objectId:'door',actionId:'open',expectedRevision:0});
                await window.__ADVENTURE_GAME__.setTarget({x:500,y:300});
                await new Promise(resolve=>setTimeout(resolve,3400));
                await controller.invoke('RequestObjectAction',{roomId:'hall',objectId:'door',actionId:'close',expectedRevision:1});
            }
            """);
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.exits.length===0");
        Assert.That(await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(500).Within(1));
        Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.state"), Is.EqualTo("idle"));
        await page.EvaluateAsync("controller.stop()");
    }

    [Test]
    public async Task LaterVisualRevision_RepairsNavigationAndRouteEvenWhenEarlierEventArrivesLast()
    {
        await using var server = DynamicNavigationTests.CreateServer();
        File.AppendAllText(Path.Combine(server.TempRoot, "world/rooms/hall.yaml"), """

          - id: switch
            image: assets/images/table.png
            position: {x: 40, y: 180}
            size: {width: 20, height: 20}
            depthY: 200
            interaction:
              name: Schalter
              initialState: off
              usePoint: {x: 100, y: 300}
              useRadius: 200
              states:
                - {id: off, name: Aus, image: assets/images/table.png, position: {x: 40, y: 180}, size: {width: 20, height: 20}, depthY: 200}
                - {id: on, name: An, image: assets/images/table.png, position: {x: 60, y: 180}, size: {width: 20, height: 20}, depthY: 200}
              actions: [{id: use, name: Benutzen, from: off, to: on}]
        """);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        // Delay at the public SignalR event boundary, without accessing private game state.
        await page.AddInitScriptAsync("""
            Object.defineProperty(window, 'signalR', {configurable:true, set(value) {
                Object.defineProperty(window,'signalR',{value,writable:true,configurable:true});
                const original = value.HubConnection.prototype.on;
                value.HubConnection.prototype.on = function(name, handler) {
                    if (name.toLowerCase() !== 'objectstateschanged') return original.call(this,name,handler);
                    let held;
                    return original.call(this,name, event => {
                        if (event.revision === 2) { held=event; return; }
                        handler(event);
                        if (held) { const delayed=held; held=null; setTimeout(()=>handler(delayed),20); }
                    });
                };
            }});
            """);
        await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page.EvaluateAsync("""
            async () => {
                window.controller = new signalR.HubConnectionBuilder().withUrl('/hub/room').build();
                await controller.start(); await controller.invoke('JoinRoom',{roomId:'hall',displayName:'Controller',worldId:'navigation'});
                await controller.invoke('RequestObjectAction',{roomId:'hall',objectId:'door',actionId:'open',expectedRevision:0});
            }
            """);
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.exits.length === 1");
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:100,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x < 470");
        await page.EvaluateAsync("""
            async () => {
                await controller.invoke('RequestObjectAction',{roomId:'hall',objectId:'door',actionId:'close',expectedRevision:1});
                await controller.invoke('RequestObjectAction',{roomId:'hall',objectId:'switch',actionId:'use',expectedRevision:2});
            }
            """);
        await page.WaitForFunctionAsync("() => { const g=window.__ADVENTURE_GAME__; return g.room.exits.length===0 && g.room.objects[2].position.x===60 && g.character.state==='idle' && g.character.speechBubble?.text.includes('versperrt'); }");
        Assert.That(await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.GreaterThan(320));
        await page.EvaluateAsync("controller.stop()");
    }
}

using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class InteractiveObjectTests
{
    [Test]
    public async Task Hub_SharedActionIsSeenByOtherAndLatePlayers()
    {
        await using var server = new TestServerFixture(foregroundWorld: true, multipleWorlds: true, authoring: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/foreground"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.4.0", "1.6.0"));
        var hall = Path.Combine(world, "rooms", "hall.yaml");
        File.WriteAllText(hall, File.ReadAllText(hall).Replace("perspective: top-down", "perspective: top-down\nviewport: { width: 320, height: 360, scrollMode: horizontal }").Replace(
            "  - { id: table, image: assets/images/table.png, position: {x: 200, y: 200}, size: {width: 120, height: 120}, depthY: 280 }",
            "  " + """
            - id: table
              image: assets/images/table.png
              position: {x: 200, y: 200}
              size: {width: 120, height: 120}
              depthY: 280
              interaction:
                name: Table
                initialState: closed
                usePoint: {x: 100, y: 300}
                useRadius: 200
                states:
                  - {id: closed, name: Closed, image: assets/images/table.png, position: {x: 200, y: 200}, size: {width: 120, height: 120}, depthY: 280}
                  - {id: open, name: Open, image: assets/images/table.png, position: {x: 240, y: 200}, size: {width: 120, height: 120}, depthY: 280}
                actions:
                  - {id: open, name: Open, from: closed, to: open, effects: [{room: next, object: gate, state: open}]}
                  - {id: close, name: Close, from: open, to: closed, effects: [{room: next, object: gate, state: closed}]}
            """.Replace("\n", "\n  ")));
        var next = Path.Combine(world, "rooms", "next.yaml");
        File.AppendAllText(next, """

            objects:
              - id: gate
                image: assets/images/table.png
                position: {x: 200, y: 200}
                size: {width: 120, height: 120}
                depthY: 280
                interaction:
                  name: Gate
                  initialState: closed
                  usePoint: {x: 100, y: 300}
                  useRadius: 40
                  states:
                    - {id: closed, name: Closed, image: assets/images/table.png, position: {x: 200, y: 200}, size: {width: 120, height: 120}, depthY: 280}
                    - {id: open, name: Open, image: assets/images/small.png, position: {x: 240, y: 200}, size: {width: 120, height: 120}, depthY: 280}
                  actions:
                    - {id: close, name: Close, from: open, to: closed}
              - id: table
                image: assets/images/table.png
                position: {x: 400, y: 200}
                size: {width: 80, height: 80}
                depthY: 280
                interaction:
                  name: Other table
                  initialState: closed
                  usePoint: {x: 100, y: 300}
                  useRadius: 40
                  states:
                    - {id: closed, name: Closed, image: assets/images/table.png, position: {x: 400, y: 200}, size: {width: 80, height: 80}, depthY: 280}
                    - {id: open, name: Open, image: assets/images/table.png, position: {x: 440, y: 200}, size: {width: 80, height: 80}, depthY: 280}
                  actions:
                    - {id: open, name: Open, from: closed, to: open}
            """);
        var other = Path.Combine(server.TempRoot, "other");
        foreach (var file in Directory.GetFiles(world, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(other, Path.GetRelativePath(world, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var otherManifest = Path.Combine(other, "world.yaml");
        File.WriteAllText(otherManifest, File.ReadAllText(otherManifest).Replace("id: foreground", "id: other"));
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 800, Height = 600 } });
        await page.GotoAsync(proxy.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.signalR && window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var result = await page.EvaluateAsync<System.Text.Json.JsonElement>("""
            async () => {
                const connections = [];
                const open = async () => { const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); connections.push(c); await c.start(); return c; };
                const join = c => c.invoke('JoinRoom', {roomId:'hall', displayName:'Probe', worldId:'foreground'});
                try {
                    const a = await open(), b = await open(), remote = await open(), foreign = await open();
                    const first = await join(a); await join(b);
                    await foreign.invoke('JoinRoom', {roomId:'hall', displayName:'Other', worldId:'other'});
                    await join(remote); await remote.invoke('ChangeRoom', {exitId:'door'});
                    let update, remoteUpdate, foreignEvents = 0;
                    foreign.on('ObjectStatesChanged', () => { foreignEvents++; });
                    b.on('ObjectStatesChanged', x => { update = x; });
                    remote.on('ObjectStatesChanged', x => { remoteUpdate = x; });
                    const [action, competing] = await Promise.all([a, b].map(c => c.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'open', expectedRevision:first.snapshot.objectRevision})));
                    await new Promise(resolve => setTimeout(resolve, 200));
                    const late = await join(await open());
                    const duplicate = await a.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'open', expectedRevision:first.snapshot.objectRevision});
                    const unauthenticated = await open();
                    const noSession = await unauthenticated.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'close', expectedRevision:update.revision});
                    const foreignRoom = await a.invoke('RequestObjectAction', {roomId:'next', objectId:'gate', actionId:'close', expectedRevision:update.revision});
                    const unknownAction = await a.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'unknown', expectedRevision:update.revision});
                    const staleRoom = await remote.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'open', expectedRevision:remoteUpdate.revision});
                    const otherLate = await open();
                    const otherSnapshot = await otherLate.invoke('JoinRoom', {roomId:'hall', displayName:'Other late', worldId:'other'});
                    return {action, competing, update, remoteUpdate, snapshot:late.snapshot, duplicate, foreignEvents, otherSnapshot:otherSnapshot.snapshot, noSession, foreignRoom, unknownAction, staleRoom};
                } finally { await Promise.all(connections.map(c => c.stop())); }
            }
            """);
        Assert.That(new[] { result.GetProperty("action").GetProperty("success").GetBoolean(), result.GetProperty("competing").GetProperty("success").GetBoolean() }.Count(x => x), Is.EqualTo(1));
        Assert.That(result.GetProperty("update").GetProperty("states")[0].GetProperty("stateId").GetString(), Is.EqualTo("open"));
        Assert.That(result.GetProperty("remoteUpdate").GetProperty("states")[0].GetProperty("stateId").GetString(), Is.EqualTo("open"));
        Assert.That(result.GetProperty("remoteUpdate").GetProperty("revision").GetInt64(), Is.EqualTo(result.GetProperty("update").GetProperty("revision").GetInt64()));
        Assert.That(result.GetProperty("snapshot").GetProperty("objectStates")[0].GetProperty("stateId").GetString(), Is.EqualTo("open"));
        Assert.That(result.GetProperty("foreignEvents").GetInt32(), Is.Zero);
        Assert.That(result.GetProperty("otherSnapshot").GetProperty("objectStates")[0].GetProperty("stateId").GetString(), Is.EqualTo("closed"));
        Assert.That(result.GetProperty("duplicate").GetProperty("success").GetBoolean(), Is.False);
        foreach (var name in new[] { "noSession", "foreignRoom", "unknownAction", "staleRoom" })
            Assert.That(result.GetProperty(name).GetProperty("success").GetBoolean(), Is.False, name);
        var second = await browser.NewPageAsync();
        await second.GotoAsync(server.BaseUrl);
        await second.Locator("#world-select").SelectOptionAsync("foreground");
        await second.ClickAsync("#world-enter");
        await second.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.room.objects[0].position.x === 240");
        Assert.That(await second.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('door')"), Is.True);
        Assert.That(await second.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'gate').position.x"), Is.EqualTo(240));
        Assert.That(await second.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'gate').image"), Is.EqualTo("/world-assets/foreground/images/small.png"));
        Assert.That(await second.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('door')"), Is.True);
        Assert.That(await second.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'table').position.x"), Is.EqualTo(240));
        var oldSession = await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        proxy.Interrupt();
        await page.WaitForFunctionAsync("old => window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== old && window.__ADVENTURE_GAME__.room.objects[0].position.x === 240", oldSession);
        await page.Locator("#game-scale").ClickAsync();
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 240");
        await page.WaitForTimeoutAsync(200);
        var openPixel = await page.EvaluateAsync<int[]>("() => [...document.querySelector('#game-canvas').getContext('2d').getImageData(230,250,1,1).data]");
        await page.Locator("#interaction-object").SelectOptionAsync("table");
        await page.Locator("#interaction-action").SelectOptionAsync("close");
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:400,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x > 390");
        await page.Locator("#interaction-submit").ClickAsync();
        await page.GetByText("Zu weit entfernt. Bitte näher herangehen.", new() { Exact = true }).First.WaitForAsync();
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:260,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && Math.abs(window.__ADVENTURE_GAME__.character.position.x - 260) < 2");
        var offsetBefore = await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset");
        Assert.That(offsetBefore, Is.GreaterThan(0));
        await page.Locator("#interaction-submit").ClickAsync();
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 200");
        await second.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 200");
        await page.WaitForTimeoutAsync(100);
        var offsetAfter = await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset");
        Assert.That(offsetAfter, Is.EqualTo(offsetBefore));
        var closedPixel = await page.EvaluateAsync<int[]>("() => [...document.querySelector('#game-canvas').getContext('2d').getImageData(230-window.__ADVENTURE_GAME__.renderer.camera.offset,250,1,1).data]");
        Assert.That(closedPixel, Is.EqualTo(new[] { 240, 20, 40, 255 }));
        Assert.That(openPixel, Is.Not.EqualTo(closedPixel));
        Assert.That(await page.Locator("#interaction-feedback").InnerTextAsync(), Does.Contain("Close"));
        await page.Locator("#interaction-action").SelectOptionAsync("open");
        await page.Locator("#interaction-submit").ClickAsync();
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 240");
        var author = await browser.NewPageAsync();
        await author.GotoAsync(server.BaseUrl);
        var reset = await author.EvaluateAsync<System.Text.Json.JsonElement>("""
            async () => {
                const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build();
                await c.start();
                await c.invoke('JoinRoom', {roomId:'hall', displayName:'Probe', worldId:'foreground'});
                let notified = false; c.on('WorldReset', () => { notified = true; });
                const status = await (await fetch('/api/editor/status?worldId=foreground')).json();
                const room = await (await fetch('/api/editor/rooms/hall?worldId=foreground')).json();
                const original = await (await fetch('/api/editor/rooms/hall/yaml?worldId=foreground')).text();
                const invalid = structuredClone(room); invalid.objects[0].interaction.initialState = 'missing';
                const bad = await fetch('/api/editor/rooms/hall?worldId=foreground', {method:'POST', headers:{'Content-Type':'application/json','X-Author-Token':status.token}, body:JSON.stringify(invalid)});
                const afterInvalid = await (await fetch('/api/editor/rooms/hall/yaml?worldId=foreground')).text();
                room.notes = 'Edited';
                const save = await fetch('/api/editor/rooms/hall?worldId=foreground', {method:'POST', headers:{'Content-Type':'application/json','X-Author-Token':status.token}, body:JSON.stringify(room)});
                await new Promise(resolve => setTimeout(resolve, 200));
                const late = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); await late.start();
                const joined = await late.invoke('JoinRoom', {roomId:'hall', displayName:'Late', worldId:'foreground'});
                await c.stop(); await late.stop();
                return {saved:save.ok, rejected:!bad.ok, preserved:original===afterInvalid, notified, state:joined.snapshot.objectStates[0].stateId};
            }
            """);
        Assert.That(reset.GetProperty("saved").GetBoolean(), Is.True);
        Assert.That(reset.GetProperty("rejected").GetBoolean(), Is.True);
        Assert.That(reset.GetProperty("preserved").GetBoolean(), Is.True);
        Assert.That(reset.GetProperty("notified").GetBoolean(), Is.True);
        Assert.That(reset.GetProperty("state").GetString(), Is.EqualTo("closed"));
        var reopened = await author.EvaluateAsync<bool>("""
            async () => {
                const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); await c.start();
                const join = await c.invoke('JoinRoom', {roomId:'hall', displayName:'Before restart', worldId:'foreground'});
                const result = await c.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'open', expectedRevision:join.snapshot.objectRevision});
                await c.stop(); return result.success;
            }
            """);
        Assert.That(reopened, Is.True);
        await server.StopAsync();
        await server.StartAsync();
        await author.GotoAsync(server.BaseUrl);
        var restartedState = await author.EvaluateAsync<string>("""
            async () => {
                const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); await c.start();
                const join = await c.invoke('JoinRoom', {roomId:'hall', displayName:'After restart', worldId:'foreground'});
                await c.stop(); return join.snapshot.objectStates[0].stateId;
            }
            """);
        Assert.That(restartedState, Is.EqualTo("closed"));
        var editor = await browser.NewPageAsync();
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=foreground");
        await editor.WaitForFunctionAsync("window.__ROOM_EDITOR__?.getState().getRoom().id === 'hall'");
        await editor.SelectOptionAsync("#object-list", "table");
        await editor.FillAsync("#object-depthY", "290");
        await editor.Locator("#object-depthY").BlurAsync();
        Assert.That(await editor.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"), Is.True);
        await editor.EvaluateAsync("window.__ROOM_EDITOR__.loadRoom('hall')");
        Assert.That(await editor.EvaluateAsync<int>("window.__ROOM_EDITOR__.getState().getRoom().objects[0].interaction.states.length"), Is.EqualTo(2));
    }
}

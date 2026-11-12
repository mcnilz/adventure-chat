using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class InteractiveObjectTests
{
    [Test]
    public async Task InterruptedApproach_ResetsVerbAndSpeaksLocally()
    {
        await using var server = new TestServerFixture(foregroundWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
        }
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(proxy.BaseUrl); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        var canvas = page.Locator("#game-canvas"); var box = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = (float)(box.Width * 230 / 640), Y = (float)(box.Height * 230 / 360) } });
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk'");
        var oldSession = await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        proxy.Interrupt();
        await page.WaitForFunctionAsync("() => document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true' && window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Die Verbindung wurde unterbrochen.'", null, new() { Timeout = 8000 });
        await page.WaitForFunctionAsync("old => window.__ADVENTURE_GAME__.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== old", oldSession);
        Assert.That(await page.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x"), Is.EqualTo(200));
    }

    [Test]
    public async Task LegacyGraphicCoveringRoom_AllowsLocalFailureWithoutLeavingObject()
    {
        await using var server = new TestServerFixture(foregroundWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
        }
        var hall = Path.Combine(world, "rooms", "hall.yaml");
        File.AppendAllText(hall, "\n  - { id: bild, image: assets/images/table.png, position: { x: 0, y: 0 }, size: { width: 640, height: 360 }, depthY: 360 }\n");
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        var canvas = page.Locator("#game-canvas"); var box = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = (float)(box.Width * 150 / 640), Y = (float)(box.Height * 200 / 360) } });
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.'", null, new() { Timeout = 8000 });
        Assert.That(await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(100).Within(2));
        Assert.That(await page.Locator("#verb-bar [data-verb=walk]").GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
    }

    [Test]
    public async Task MultipleUsePoints_TryAnotherWhenNearestIsUnreachable()
    {
        await using var server = new TestServerFixture(foregroundWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
        }
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.6.0", "1.7.0").Replace("startSpawn: entry", "startSpawn: east"));
        var hall = Path.Combine(world, "rooms", "hall.yaml");
        var yaml = File.ReadAllText(hall)
            .Replace("spawns: [{ id: entry, position: { x: 100, y: 300 } }]", "spawns: [{ id: entry, position: { x: 100, y: 300 } }, { id: east, position: { x: 400, y: 300 } }]")
            .Replace("exits:\n", "obstacles:\n  - id: divider\n    points: [{x: 300, y: 100.1}, {x: 340, y: 100.1}, {x: 340, y: 349.9}, {x: 300, y: 349.9}]\nexits:\n")
            .Replace("    depthY: 280\n    interaction:", "    depthY: 280\n    usePoints: [{x: 280, y: 300}, {x: 560, y: 300}]\n    interaction:");
        var oldPoint = "      usePoint: { x: 100, y: 300 }\n";
        yaml = yaml.Remove(yaml.IndexOf(oldPoint, StringComparison.Ordinal), oldPoint.Length);
        File.WriteAllText(hall, yaml);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        var canvas = page.Locator("#game-canvas"); var box = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = (float)(box.Width * 230 / 640), Y = (float)(box.Height * 230 / 360) } });
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 240");
        Assert.That(await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(560).Within(2));
    }

    [Test]
    public async Task MultipleUsePoints_ChooseNearbyPointAndKeepFailurePrivate()
    {
        await using var server = new TestServerFixture(foregroundWorld: true, authoring: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
        }
        var manifest = Path.Combine(world, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.6.0", "1.7.0"));
        var hall = Path.Combine(world, "rooms", "hall.yaml");
        var yaml = File.ReadAllText(hall).Replace("    depthY: 280\n    interaction:", "    depthY: 280\n    usePoints: [{x: 100, y: 300}, {x: 350, y: 300}]\n    interaction:");
        var oldPoint = "      usePoint: { x: 100, y: 300 }\n";
        yaml = yaml.Remove(yaml.IndexOf(oldPoint, StringComparison.Ordinal), oldPoint.Length);
        File.WriteAllText(hall, yaml + "\n  - { id: kiste, image: assets/images/table.png, position: { x: 450, y: 200 }, size: { width: 40, height: 40 }, depthY: 280, usePoints: [{x: 430, y: 300}, {x: 520, y: 300}] }\n  - { id: randobjekt, image: assets/images/table.png, position: { x: 450, y: 320 }, size: { width: 40, height: 30 }, depthY: 340 }\n");
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(); var b = await browser.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        async Task ClickChair(IPage page, int x)
        {
            var canvas = page.Locator("#game-canvas"); var box = (await canvas.BoundingBoxAsync())!;
            await canvas.ClickAsync(new() { Position = new() { X = (float)(box.Width * x / 640), Y = (float)(box.Height * 230 / 360) } });
        }
        await b.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        await b.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickChair(b, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 240");
        Assert.That(await b.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(350).Within(2));
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:500,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 490");
        await a.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickChair(a, 270);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.'");
        Assert.That(await a.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(350).Within(2));
        Assert.That(await b.EvaluateAsync<bool>("[...window.__ADVENTURE_GAME__.remotePlayers.values()].every(p => !p.speechBubble)"), Is.True);
        Assert.That(await a.Locator("#verb-bar [data-verb=walk]").GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
        await a.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickChair(a, 460);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && Math.abs(window.__ADVENTURE_GAME__.character.position.x - 430) < 2");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.' && document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true'");
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:100,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x < 110");
        await a.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        var edgeCanvas = a.Locator("#game-canvas"); var edgeBox = (await edgeCanvas.BoundingBoxAsync())!;
        await edgeCanvas.ClickAsync(new() { Position = new() { X = (float)(edgeBox.Width * 460 / 640), Y = (float)(edgeBox.Height * 330 / 360) } });
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && Math.abs(window.__ADVENTURE_GAME__.character.position.x - 442) < 2");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.' && document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true'");
        var author = await browser.NewPageAsync(); await author.GotoAsync(server.BaseUrl);
        var saved = await author.EvaluateAsync<System.Text.Json.JsonElement>("""
            async () => {
                const status = await (await fetch('/api/editor/status?worldId=interactions')).json();
                const room = await (await fetch('/api/editor/rooms/hall?worldId=interactions')).json();
                const before = await (await fetch('/api/editor/rooms/hall/yaml?worldId=interactions')).text();
                const invalid = structuredClone(room); invalid.objects[0].usePoints = [];
                const options = body => ({method:'POST', headers:{'Content-Type':'application/json','X-Author-Token':status.token}, body:JSON.stringify(body)});
                const rejected = await fetch('/api/editor/rooms/hall?worldId=interactions', options(invalid));
                const after = await (await fetch('/api/editor/rooms/hall/yaml?worldId=interactions')).text();
                room.notes = 'Roundtrip';
                const accepted = await fetch('/api/editor/rooms/hall?worldId=interactions', options(room));
                const reopened = await (await fetch('/api/editor/rooms/hall?worldId=interactions')).json();
                return {rejected:!rejected.ok, unchanged:before===after, accepted:accepted.ok, points:reopened.objects[0].usePoints.length};
            }
            """);
        Assert.That(saved.GetProperty("rejected").GetBoolean(), Is.True);
        Assert.That(saved.GetProperty("unchanged").GetBoolean(), Is.True);
        Assert.That(saved.GetProperty("accepted").GetBoolean(), Is.True);
        Assert.That(saved.GetProperty("points").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public async Task Verbs_ActOnObjectsClickedInTheScene()
    {
        await using var server = new TestServerFixture(foregroundWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/interactions"));
        var world = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(world, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var hall = Path.Combine(world, "rooms", "hall.yaml");
        var extraActions = string.Join("\n", Enumerable.Range(1, 24).Select(i => $"        - {{ id: inspect-{i}, name: 'Untersuchen Alternative {i}', from: off, to: off }}"));
        File.AppendAllText(hall, $"\n{extraActions}\n  - {{ id: cover, image: assets/images/table.png, position: {{ x: 350, y: 200 }}, size: {{ width: 40, height: 40 }}, depthY: 320 }}\n");
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(); var b = await browser.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl);
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        async Task ClickAt(IPage page, int x, int y)
        {
            var canvas = page.Locator("#game-canvas"); var box = (await canvas.BoundingBoxAsync())!;
            var position = new Position { X = (float)(box.Width * x / 640), Y = (float)(box.Height * y / 360) };
            await canvas.HoverAsync(new() { Position = position });
            await canvas.ClickAsync(new() { Position = position });
        }
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:400,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 390");
        await a.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickAt(a, 230, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.'", null, new() { Timeout = 8000 });
        Assert.That(await a.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.LessThan(110));
        Assert.That(await b.EvaluateAsync<bool>("[...window.__ADVENTURE_GAME__.remotePlayers.values()].every(p => !p.speechBubble)"), Is.True);
        Assert.That(await a.Locator("#verb-bar [data-verb=walk]").GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
        await a.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickAt(a, 230, 230);
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 240");
        await a.WaitForFunctionAsync("() => document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true'");
        await b.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickAt(b, 270, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 200");
        Assert.That(await a.EvaluateAsync<int>("window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(100));
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:400,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 390");
        await a.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickAt(a, 230, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk'");
        await a.GetByRole(AriaRole.Button, new() { Name = "Gehe zu", Exact = true }).ClickAsync();
        await ClickAt(a, 400, 300);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 390");
        Assert.That(await b.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x"), Is.EqualTo(200));
        await a.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickAt(a, 230, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk'");
        await b.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickAt(b, 370, 220);
        await b.GetByRole(AriaRole.Button, new() { Name = "Untersuchen Alternative 1", Exact = true }).ClickAsync();
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 240");
        Assert.That(await a.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.LessThan(110));
        await a.WaitForFunctionAsync("() => document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true'");
        await b.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickAt(b, 270, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 200");
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:400,y:300})");
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 390");
        await a.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickAt(a, 230, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk'");
        await b.GetByRole(AriaRole.Button, new() { Name = "Ziehen", Exact = true }).ClickAsync();
        await ClickAt(b, 230, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Der Objektzustand hat sich geändert. Bitte erneut versuchen.'");
        Assert.That(await a.Locator("#verb-bar [data-verb=walk]").GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
        Assert.That(await a.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x"), Is.EqualTo(240));
        await b.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickAt(b, 270, 230);
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'chair').position.x === 200");
        await b.GetByRole(AriaRole.Button, new() { Name = "Gehe zu", Exact = true }).ClickAsync();
        await ClickAt(b, 100, 300);
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'next'");
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickAt(a, 360, 220);
        Assert.That(await a.GetByRole(AriaRole.Button, new() { Name = "Einschalten", Exact = true }).CountAsync(), Is.Zero);
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickAt(a, 370, 220);
        Assert.That(await a.Locator("#action-text").InnerTextAsync(), Is.EqualTo("Benutzen Schalter"));
        Assert.That(await a.Locator("#action-choices").EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"), Is.True);
        var previousRevision = await a.EvaluateAsync<long>("window.__ADVENTURE_GAME__.state.objectRevision");
        await a.GetByRole(AriaRole.Button, new() { Name = "Untersuchen Alternative 24", Exact = true }).ClickAsync();
        await a.WaitForFunctionAsync("revision => window.__ADVENTURE_GAME__.state.objectRevision > revision", previousRevision);
        await a.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickAt(a, 370, 220);
        await a.GetByRole(AriaRole.Button, new() { Name = "Einschalten", Exact = true }).ClickAsync();
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'gate').position.x === 260");
    }

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
        async Task ClickTable()
        {
            var canvas = page.Locator("#game-canvas");
            var box = (await canvas.BoundingBoxAsync())!;
            var coords = await page.EvaluateAsync<double[]>("""
                () => { const g = window.__ADVENTURE_GAME__, o = g.room.objects.find(x => x.id === 'table');
                    return [o.position.x + o.size.width / 4 - g.renderer.camera.offset, o.position.y + o.size.height / 2]; }
                """);
            var position = new Position { X = (float)(coords[0] * box.Width / 320), Y = (float)(coords[1] * box.Height / 360) };
            await canvas.HoverAsync(new() { Position = position });
            await canvas.ClickAsync(new() { Position = position });
        }
        await page.GotoAsync(proxy.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.signalR && window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await page.Locator("#action-bar select").CountAsync(), Is.Zero);
        foreach (var verb in new[] { "Gehe zu", "Ziehen", "Drücken", "Benutzen" })
            Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = verb, Exact = true }).CountAsync(), Is.EqualTo(1));
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
                    const distant = await open(); await join(distant);
                    await distant.invoke('RequestMove', {target:{x:400,y:300}});
                    await new Promise(resolve => setTimeout(resolve, 2500));
                    const tooFar = await distant.invoke('RequestObjectAction', {roomId:'hall', objectId:'table', actionId:'close', expectedRevision:update.revision});
                    const otherLate = await open();
                    const otherSnapshot = await otherLate.invoke('JoinRoom', {roomId:'hall', displayName:'Other late', worldId:'other'});
                    return {action, competing, update, remoteUpdate, snapshot:late.snapshot, duplicate, foreignEvents, otherSnapshot:otherSnapshot.snapshot, noSession, foreignRoom, unknownAction, staleRoom, tooFar};
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
        Assert.That(result.GetProperty("tooFar").GetProperty("errorCode").GetString(), Is.EqualTo("too_far"));
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
        await page.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:400,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 390");
        await ClickTable();
        Assert.That(await page.Locator("#action-text").InnerTextAsync(), Is.EqualTo("Benutzen Table"));
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 200");
        await second.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 200");
        Assert.That(await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.LessThan(110));
        await page.WaitForFunctionAsync("() => document.querySelector('#verb-bar [data-verb=walk]').getAttribute('aria-pressed') === 'true'");
        await page.WaitForTimeoutAsync(100);
        var closedPixel = await page.EvaluateAsync<int[]>("() => [...document.querySelector('#game-canvas').getContext('2d').getImageData(230-window.__ADVENTURE_GAME__.renderer.camera.offset,250,1,1).data]");
        Assert.That(closedPixel, Is.EqualTo(new[] { 240, 20, 40, 255 }));
        Assert.That(openPixel, Is.Not.EqualTo(closedPixel));
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:260,y:300})");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle' && Math.abs(window.__ADVENTURE_GAME__.character.position.x - 260) < 2");
        var offsetBefore = await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset");
        Assert.That(offsetBefore, Is.GreaterThan(0));
        await page.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickTable();
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.objects[0].position.x === 240");
        await page.WaitForTimeoutAsync(100);
        var offsetAfter = await page.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset");
        Assert.That(offsetAfter, Is.EqualTo(offsetBefore));
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

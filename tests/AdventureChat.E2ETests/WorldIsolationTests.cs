using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class WorldIsolationTests
{
    [Test]
    public async Task Browser_InitialWorldConnectionFailure_OffersRetry()
    {
        await using var server = new TestServerFixture(externalWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("**/hub/room/negotiate*", route => route.AbortAsync());
        await page.GotoAsync(server.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.GetByText("Erneut versuchen", new() { Exact = true }).WaitForAsync();
        Assert.That(await page.Locator("#game-container").IsVisibleAsync(), Is.False);
    }

    [Test]
    public async Task Browser_ReconnectInChosenWorld_ReturnsToStartWithoutGhosts()
    {
        await using var server = new TestServerFixture(multipleWorlds: true);
        await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync();
        var peer = await browser.NewPageAsync();
        var foreign = await browser.NewPageAsync();
        await foreign.GotoAsync(server.BaseUrl);
        await foreign.SelectOptionAsync("#world-select", "reference");
        await foreign.ClickAsync("#world-enter");
        await foreign.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        foreach (var page in new[] { a, peer })
        {
            await page.GotoAsync(page == a ? proxy.BaseUrl : server.BaseUrl);
            await page.SelectOptionAsync("#world-select", "other");
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        var oldSession = await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        // Garden's exit is reachable directly from its start spawn.
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('to-lounge')"), Is.True);
        await peer.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0");
        proxy.Interrupt();
        await a.WaitForFunctionAsync("oldId => window.__ADVENTURE_GAME__?.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== oldId && window.__ADVENTURE_GAME__.room.id === 'garden'", oldSession);
        await peer.WaitForFunctionAsync("oldId => window.__ADVENTURE_GAME__.remotePlayers.size === 1 && !window.__ADVENTURE_GAME__.remotePlayers.has(oldId)", oldSession);
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.backgroundUrl"), Is.EqualTo("/world-assets/other/images/garden.png"));
        Assert.That(await foreign.EvaluateAsync<int>("window.__ADVENTURE_GAME__.remotePlayers.size"), Is.Zero);
        await a.RouteAsync("**/api/worlds/other/start", route => route.FulfillAsync(new() { Status = 404 }));
        proxy.Interrupt();
        await a.Locator("#world-select").WaitForAsync();
        await peer.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0");
    }

    [Test]
    public async Task Browser_SelectLeaveAndSwitchWorld_ClearsPresenceAndChat()
    {
        await using var server = new TestServerFixture(multipleWorlds: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync();
        var b = await browser.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl);
            await page.Locator("#world-select").SelectOptionAsync("reference");
            await page.Locator("#world-enter").ClickAsync();
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        }
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await a.Locator("#chat-input").FillAsync("Nachricht nur in Referenzwelt");
        await a.Locator("#chat-send").ClickAsync();
        await b.GetByText("Nachricht nur in Referenzwelt", new() { Exact = true }).WaitForAsync();
        await a.Locator("#world-leave").ClickAsync();
        await a.Locator("#world-select").WaitForAsync();
        await b.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0");
        await a.Locator("#world-select").SelectOptionAsync("other");
        await a.Locator("#world-enter").ClickAsync();
        await a.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await a.Locator("#chat-messages").InnerTextAsync(), Does.Not.Contain("Nachricht nur in Referenzwelt"));
        Assert.That(await a.EvaluateAsync<int>("window.__ADVENTURE_GAME__.remotePlayers.size"), Is.Zero);
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.backgroundUrl"), Is.EqualTo("/world-assets/other/images/garden.png"));
    }

    [Test]
    public async Task Hub_IdenticalRoomIds_IsolateSnapshotsChatMovementAndGroups()
    {
        await using var server = new TestServerFixture(multipleWorlds: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.signalR && window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var result = await page.EvaluateAsync<System.Text.Json.JsonElement>("""
            async () => {
                const connections = [];
                const open = async () => {
                    const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build();
                    connections.push(c); await c.start(); return c;
                };
                try {
                    const a = await open(), b = await open(), peer = await open();
                    const join = (c, worldId) => c.invoke('JoinRoom', {roomId:'garden', displayName:'Probe', worldId});
                    const first = await join(a, 'reference');
                    const other = await join(b, 'other');
                    const same = await join(peer, 'reference');
                    if (!first.success || !other.success || !same.success) return {joined:false};
                    let foreign = 0, own = 0;
                    b.on('ChatMessageReceived', () => foreign++);
                    b.on('PlayerMoved', () => foreign++);
                    peer.on('ChatMessageReceived', () => own++);
                    peer.on('PlayerMoved', () => own++);
                    await a.invoke('SendChatMessage', {text:'Only reference'});
                    await a.invoke('RequestMove', {target:{x:120,y:320}});
                    const repeated = await join(a, 'other');
                    const unknown = await open();
                    const rejected = await join(unknown, 'missing');
                    const late = await open();
                    const snapshot = await join(late, 'other');
                    await new Promise(resolve => setTimeout(resolve, 200));
                    const ownBeforeSwitch = own;
                    await a.invoke('LeaveWorld');
                    const switched = await join(a, 'other');
                    let oldWorldEvents = 0;
                    a.on('ChatMessageReceived', () => oldWorldEvents++);
                    await peer.invoke('SendChatMessage', {text:'After switching worlds'});
                    await new Promise(resolve => setTimeout(resolve, 200));
                    return {joined:true, first:first.snapshot.players.length, other:other.snapshot.players.length,
                        same:same.snapshot.players.length, foreign, own:ownBeforeSwitch, repeated:repeated.success,
                        switched:switched.success, oldWorldEvents,
                        unknown:rejected.success, history:snapshot.snapshot.recentMessages.length,
                        firstBackground:first.snapshot.room.backgroundUrl, otherBackground:other.snapshot.room.backgroundUrl};
                } finally { await Promise.all(connections.map(c => c.stop())); }
            }
            """);
        Assert.That(result.GetProperty("joined").GetBoolean(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("other").GetInt32(), Is.EqualTo(1));
            Assert.That(result.GetProperty("same").GetInt32(), Is.EqualTo(result.GetProperty("first").GetInt32() + 1));
            Assert.That(result.GetProperty("foreign").GetInt32(), Is.Zero);
            Assert.That(result.GetProperty("own").GetInt32(), Is.EqualTo(2));
            Assert.That(result.GetProperty("repeated").GetBoolean(), Is.False);
            Assert.That(result.GetProperty("unknown").GetBoolean(), Is.False);
            Assert.That(result.GetProperty("history").GetInt32(), Is.Zero);
            Assert.That(result.GetProperty("switched").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("oldWorldEvents").GetInt32(), Is.Zero);
            Assert.That(result.GetProperty("firstBackground").GetString(), Is.EqualTo("/world-assets/reference/images/garden.png"));
            Assert.That(result.GetProperty("otherBackground").GetString(), Is.EqualTo("/world-assets/other/images/garden.png"));
        });
    }
}

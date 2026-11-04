using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class WorldIsolationTests
{
    [Test]
    public async Task Hub_IdenticalRoomIds_IsolateSnapshotsChatMovementAndGroups()
    {
        await using var server = new TestServerFixture(multipleWorlds: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
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
                    return {joined:true, first:first.snapshot.players.length, other:other.snapshot.players.length,
                        same:same.snapshot.players.length, foreign, own, repeated:repeated.success,
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
            Assert.That(result.GetProperty("firstBackground").GetString(), Is.EqualTo("/world-assets/reference/images/garden.png"));
            Assert.That(result.GetProperty("otherBackground").GetString(), Is.EqualTo("/world-assets/other/images/garden.png"));
        });
    }
}

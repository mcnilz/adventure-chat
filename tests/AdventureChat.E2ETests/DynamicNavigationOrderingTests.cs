using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class DynamicNavigationOrderingTests
{
    [Test]
    public async Task ConcurrentCommands_LateEntryAndFreshReconnectUseConfirmedNavigation()
    {
        await using var server = DynamicNavigationTests.CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const connections = [];
                const open = async () => { const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); connections.push(c); await c.start(); return c; };
                const join = c => c.invoke('JoinRoom', {roomId:'hall',displayName:'Probe',worldId:'navigation'});
                const action = (c,id,revision) => c.invoke('RequestObjectAction', {roomId:'hall',objectId:'door',actionId:id,expectedRevision:revision});
                try {
                    const a = await open(), b = await open(); await join(a); const first = await join(b);
                    const opened = await action(a,'open',0);
                    const [move, close] = await Promise.all([b.invoke('RequestMove',{target:{x:500,y:300}}),action(a,'close',1)]);
                    const late = await join(await open());
                    await action(a,'open',2);
                    await b.invoke('RequestMove',{target:{x:500,y:300}});
                    await new Promise(resolve => setTimeout(resolve,3300));
                    const [transition, secondClose] = await Promise.all([b.invoke('ChangeRoom',{exitId:'next'}),action(a,'close',3)]);
                    await b.stop(); await b.start(); const reconnected = await join(b);
                    const blocked = await b.invoke('RequestMove',{target:{x:500,y:300}});
                    return {opened,move,close,late:late.snapshot,transition,secondClose,first:first.snapshot,reconnected:reconnected.snapshot,blocked};
                } finally { await Promise.all(connections.map(c => c.stop())); }
            }
            """);
        Assert.That(result.GetProperty("opened").GetProperty("success").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("close").GetProperty("success").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("late").GetProperty("objectStates")[0].GetProperty("stateId").GetString(), Is.EqualTo("closed"));
        Assert.That(result.GetProperty("late").GetProperty("room").GetProperty("exits").GetArrayLength(), Is.Zero);
        Assert.That(result.GetProperty("secondClose").GetProperty("success").GetBoolean(), Is.True);
        var reconnected = result.GetProperty("reconnected");
        Assert.That(reconnected.GetProperty("yourSessionId").GetString(), Is.Not.EqualTo(result.GetProperty("first").GetProperty("yourSessionId").GetString()));
        Assert.That(reconnected.GetProperty("players").EnumerateArray().All(p => p.GetProperty("position").GetProperty("x").GetDouble() <= 500), Is.True);
        Assert.That(reconnected.GetProperty("objectStates")[0].GetProperty("stateId").GetString(), Is.EqualTo("closed"));
        Assert.That(result.GetProperty("blocked").GetProperty("success").GetBoolean(), Is.False);
        if (result.GetProperty("transition").GetProperty("success").GetBoolean())
            Assert.That(result.GetProperty("transition").GetProperty("snapshot").GetProperty("roomId").GetString(), Is.EqualTo("next"));
    }
}

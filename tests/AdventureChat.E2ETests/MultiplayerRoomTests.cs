using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class MultiplayerRoomTests
{
    [Test]
    public async Task FrozenBackgroundTab_CatchesUpWithServerAfterResume()
    {
        await using var first = await _browser.NewContextAsync();
        await using var second = await _browser.NewContextAsync();
        var page1 = await first.NewPageAsync();
        await page1.GotoAsync($"{_server.BaseUrl}?name=First");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var page2 = await second.NewPageAsync();
        await page2.GotoAsync($"{_server.BaseUrl}?name=Second");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page1.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x: 220, y: 300})");
        Assert.That(await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.state"), Is.EqualTo("walk"));
        await page2.BringToFrontAsync();
        var cdp = await first.NewCDPSessionAsync(page1);
        await cdp.SendAsync("Page.setWebLifecycleState", new Dictionary<string, object> { ["state"] = "frozen" });
        await Task.Delay(1100);
        await cdp.SendAsync("Page.setWebLifecycleState", new Dictionary<string, object> { ["state"] = "active" });
        await page1.BringToFrontAsync();
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'");
        Assert.That(await page1.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(220).Within(1));
        await page2.WaitForFunctionAsync("() => [...window.__ADVENTURE_GAME__.remotePlayers.values()].some(p => p.name === 'First' && p.state === 'idle' && Math.abs(p.position.x - 220) < 1)");
    }
    [Test]
    public async Task JoinWhileInitialSnapshotIsInFlight_IsAppliedAfterSnapshot()
    {
        await using var first = await _browser.NewContextAsync();
        await using var second = await _browser.NewContextAsync();
        var page1 = await first.NewPageAsync();
        await page1.AddInitScriptAsync("""
            (() => {
                let library;
                Object.defineProperty(window, 'signalR', {
                    configurable: true,
                    get: () => library,
                    set: value => {
                        library = value;
                        const original = value.HubConnection.prototype.invoke;
                        value.HubConnection.prototype.invoke = async function(method, ...args) {
                            const result = await original.call(this, method, ...args);
                            if (method === 'JoinRoom' && result.success) {
                                window.joinHeld = true;
                                await new Promise(resolve => { window.releaseJoin = resolve; });
                            }
                            return result;
                        };
                    }
                });
            })();
            """);
        await page1.GotoAsync($"{_server.BaseUrl}?name=First");
        await page1.WaitForFunctionAsync("() => window.joinHeld === true");
        var page2 = await second.NewPageAsync();
        await page2.GotoAsync($"{_server.BaseUrl}?name=Second");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await page1.EvaluateAsync("() => window.releaseJoin()");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.remotePlayers.size === 1");
    }
    [Test]
    public async Task SignalR_RepeatedJoinAndForgedProgress_CannotChangeSession()
    {
        await using var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForFunctionAsync("() => window.signalR && window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var result = await page.EvaluateAsync<System.Text.Json.JsonElement>("""
            async () => {
                const connection = new window.signalR.HubConnectionBuilder().withUrl('/hub/room').build();
                await connection.start();
                const first = await connection.invoke('JoinRoom', {roomId: 'lounge', displayName: 'Probe'});
                const second = await connection.invoke('JoinRoom', {roomId: 'garden', displayName: 'Probe'});
                await connection.invoke('UpdateProgress', {x: 540, y: 300}, 'right', 'idle');
                const transition = await connection.invoke('ChangeRoom', {exitId: 'to-garden', targetRoomId: 'garden', targetSpawnId: 'forged'});
                await connection.stop();
                return {first: first.success, second: second.success, transition: transition.success};
            }
            """);
        Assert.That(result.GetProperty("first").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("second").GetBoolean(), Is.False);
        Assert.That(result.GetProperty("transition").GetBoolean(), Is.False);
    }
    private TestServerFixture _server = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _server = new TestServerFixture();
        await _server.StartAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
        if (_server != null)
        {
            await _server.DisposeAsync();
        }
    }

    [Test]
    public async Task TwoBrowserContexts_SeeBothCharacters_WithDistinctNames()
    {
        await using var context1 = await _browser.NewContextAsync();
        await using var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForSelectorAsync("#game-canvas");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForSelectorAsync("#game-canvas");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Page 1 should see 1 remote player (Elaine)
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        // Page 2 should see 1 remote player (Guybrush)
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        var p1LocalName = await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.name");
        var p1RemoteName = await page1.EvaluateAsync<string>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].name");

        var p2LocalName = await page2.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.name");
        var p2RemoteName = await page2.EvaluateAsync<string>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].name");

        Assert.That(p1LocalName, Is.EqualTo("Guybrush"));
        Assert.That(p1RemoteName, Is.EqualTo("Elaine"));
        Assert.That(p2LocalName, Is.EqualTo("Elaine"));
        Assert.That(p2RemoteName, Is.EqualTo("Guybrush"));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task MovementInBrowser1_IsObservedSmoothlyInBrowser2()
    {
        var context1 = await _browser.NewContextAsync();
        var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Guybrush moves to (500, 300) on right side of room
        await page1.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 500, y: 300 })");

        // Elaine's browser should see Guybrush change state to 'walk' and start moving
        await page2.WaitForFunctionAsync("() => { const r = Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0]; return r && r.state === 'walk'; }", null, new PageWaitForFunctionOptions
        {
            Timeout = 3000
        });

        // Wait until movement finishes in both browsers
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'", null, new PageWaitForFunctionOptions
        {
            Timeout = 6000
        });

        await page2.WaitForFunctionAsync("() => { const r = Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0]; return r && r.state === 'idle'; }", null, new PageWaitForFunctionOptions
        {
            Timeout = 6000
        });

        var p2GuybrushX = await page2.EvaluateAsync<double>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].position.x");
        var p2GuybrushY = await page2.EvaluateAsync<double>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].position.y");

        Assert.That(p2GuybrushX, Is.EqualTo(500).Within(2.0));
        Assert.That(p2GuybrushY, Is.EqualTo(300).Within(2.0));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task DisconnectingBrowser1_RemovesCharacterFromBrowser2()
    {
        var context1 = await _browser.NewContextAsync();
        var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Close page1 / context1
        await page1.CloseAsync();
        await context1.DisposeAsync();

        // Page2 should see 0 remote players
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000
        });

        var remoteCount = await page2.EvaluateAsync<int>("() => window.__ADVENTURE_GAME__.remotePlayers.size");
        Assert.That(remoteCount, Is.EqualTo(0));

        await context2.DisposeAsync();
    }

    [Test]
    public async Task Reconnecting_LeavesNoDuplicateGhosts()
    {
        await using var proxy = new InterruptibleProxy(_server.BaseUrl);
        await using var context1 = await _browser.NewContextAsync();
        await using var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{proxy.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        var oldSession = await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.sessionId");
        await page1.EvaluateAsync("() => { window.reconnectObserved = false; const game = window.__ADVENTURE_GAME__; const previous = game.onConnectionStatusChanged; game.onConnectionStatusChanged = status => { if (status === 'connecting') window.reconnectObserved = true; previous?.(status); }; }");
        proxy.Interrupt();
        await page1.WaitForFunctionAsync("oldId => window.__ADVENTURE_GAME__.connectionStatus === 'connected' && window.__ADVENTURE_GAME__.sessionId !== oldId", oldSession, new PageWaitForFunctionOptions { Timeout = 30000 });
        Assert.That(await page1.EvaluateAsync<bool>("() => window.reconnectObserved"), Is.True);

        // In page2, there should still be exactly 1 remote player (no duplicate ghost)
        await page2.WaitForFunctionAsync("oldId => window.__ADVENTURE_GAME__.remotePlayers.size === 1 && !window.__ADVENTURE_GAME__.remotePlayers.has(oldId)", oldSession, new PageWaitForFunctionOptions
        {
            Timeout = 30000
        });

        var remoteCountInP2 = await page2.EvaluateAsync<int>("() => window.__ADVENTURE_GAME__.remotePlayers.size");
        Assert.That(remoteCountInP2, Is.EqualTo(1));

        var remoteCountInP1 = await page1.EvaluateAsync<int>("() => window.__ADVENTURE_GAME__.remotePlayers.size");
        Assert.That(remoteCountInP1, Is.EqualTo(1));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task LateJoiningParticipant_ReceivesActiveMovement_InSnapshot()
    {
        var context1 = await _browser.NewContextAsync();
        var page1 = await context1.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForSelectorAsync("#game-canvas");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Guybrush starts long path walk towards far right (550, 320)
        await page1.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 550, y: 320 })");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'walk'");

        // Now Elaine joins while Guybrush is already walking
        var context2 = await _browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForSelectorAsync("#game-canvas");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Elaine receives Guybrush and sees him in state 'walk' or moving along waypoints
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Wait until movement completes
        await page2.WaitForFunctionAsync("() => { const r = Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0]; return r && r.state === 'idle'; }", null, new PageWaitForFunctionOptions
        {
            Timeout = 7000
        });

        var p2GuybrushX = await page2.EvaluateAsync<double>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].position.x");
        var p2GuybrushY = await page2.EvaluateAsync<double>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].position.y");

        Assert.That(p2GuybrushX, Is.EqualTo(550).Within(2.0));
        Assert.That(p2GuybrushY, Is.EqualTo(320).Within(2.0));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }
}

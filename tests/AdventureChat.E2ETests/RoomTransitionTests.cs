using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class RoomTransitionTests
{
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
    public async Task Player_ClickingExit_NavigatesToExitAndTransitionsToTargetRoom()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        var initialRoomId = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.id");
        Assert.That(initialRoomId, Is.EqualTo("lounge"));

        // Click near the exit to garden (x: 580, y: 300)
        var canvas = await page.QuerySelectorAsync("#game-canvas");
        Assert.That(canvas, Is.Not.Null);

        var box = await canvas!.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null);

        var clickX = (float)(box!.X + (box.Width * (580.0 / 640.0)));
        var clickY = (float)(box.Y + (box.Height * (300.0 / 360.0)));

        await page.Mouse.ClickAsync(clickX, clickY);

        // Character should start walking towards exit and then transition to 'garden'
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'garden'", new PageWaitForFunctionOptions
        {
            Timeout = 10000
        });

        var newRoomId = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.id");
        Assert.That(newRoomId, Is.EqualTo("garden"));

        var newRoomName = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.name");
        Assert.That(newRoomName, Is.EqualTo("Tavernen-Garten"));

        // Character should spawn at garden spawn (x: 80, y: 300)
        var playerX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var playerY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");
        Assert.That(playerX, Is.InRange(70, 90));
        Assert.That(playerY, Is.InRange(290, 310));

        // System message for room change should appear in chat
        await page.WaitForSelectorAsync("#chat-messages .chat-system-message");
        var systemMsgs = await page.InnerTextAsync("#chat-messages");
        Assert.That(systemMsgs, Does.Contain("Raum gewechselt: Tavernen-Garten"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task ActionBar_HoverOverDoorway_DisplaysLucasfilmActionSentence()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        var canvas = await page.QuerySelectorAsync("#game-canvas");
        Assert.That(canvas, Is.Not.Null);

        var box = await canvas!.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null);

        // 1. Hover over empty floor (x=200, y=280)
        var floorX = (float)(box!.X + (box.Width * (200.0 / 640.0)));
        var floorY = (float)(box.Y + (box.Height * (280.0 / 360.0)));
        await page.Mouse.MoveAsync(floorX, floorY);

        await page.WaitForFunctionAsync("() => document.getElementById('action-text').innerText === 'Walk to'");
        var floorAction = await page.InnerTextAsync("#action-text");
        Assert.That(floorAction, Is.EqualTo("Walk to"));

        // 2. Hover over the doorway to garden (x=570, y=160 - door archway)
        var doorX = (float)(box.X + (box.Width * (570.0 / 640.0)));
        var doorY = (float)(box.Y + (box.Height * (160.0 / 360.0)));
        await page.Mouse.MoveAsync(doorX, doorY);

        await page.WaitForFunctionAsync("() => document.getElementById('action-text').innerText.includes('Go to Tavernen-Garten')");
        var doorAction = await page.InnerTextAsync("#action-text");
        Assert.That(doorAction, Is.EqualTo("Go to Tavernen-Garten"));

        // 3. Clicking on the door arch should trigger the transition
        await page.Mouse.ClickAsync(doorX, doorY);

        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'garden'", new PageWaitForFunctionOptions
        {
            Timeout = 10000
        });

        // 4. In garden, hover over tavern door on the left (x=60, y=160)
        var gDoorX = (float)(box.X + (box.Width * (60.0 / 640.0)));
        var gDoorY = (float)(box.Y + (box.Height * (160.0 / 360.0)));
        await page.Mouse.MoveAsync(gDoorX, gDoorY);

        await page.WaitForFunctionAsync("() => document.getElementById('action-text').innerText.includes('Go to Lounge')");
        var gardenDoorAction = await page.InnerTextAsync("#action-text");
        Assert.That(gardenDoorAction, Is.EqualTo("Go to Lounge"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Multiplayer_RoomTransition_NotifiesParticipantsAndIsolatesChat()
    {
        var context1 = await _browser.NewContextAsync();
        var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Both players should see each other in Lounge
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Guybrush (page1) transitions to Garden
        await page1.EvaluateAsync("() => window.__ADVENTURE_GAME__.changeRoom('garden')");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'garden'");

        // Elaine (page2) in Lounge should see Guybrush leave
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0");

        // Guybrush in Garden sends a message
        await page1.FillAsync("#chat-input", "I am in the quiet garden now.");
        await page1.ClickAsync("#chat-send");

        // Guybrush sees the message
        await page1.WaitForSelectorAsync("#chat-messages .chat-message");
        var p1Chat = await page1.InnerTextAsync("#chat-messages");
        Assert.That(p1Chat, Does.Contain("I am in the quiet garden now."));

        // Elaine in Lounge should NOT receive Guybrush's garden message
        await page2.WaitForTimeoutAsync(500);
        var p2Chat = await page2.InnerTextAsync("#chat-messages");
        Assert.That(p2Chat, Does.Not.Contain("I am in the quiet garden now."));

        // Elaine in Lounge sends a message
        await page2.FillAsync("#chat-input", "Enjoying my grog inside the tavern.");
        await page2.ClickAsync("#chat-send");

        // Guybrush in Garden should NOT receive Elaine's lounge message
        await page1.WaitForTimeoutAsync(500);
        var p1ChatAfter = await page1.InnerTextAsync("#chat-messages");
        Assert.That(p1ChatAfter, Does.Not.Contain("Enjoying my grog inside the tavern."));

        // Guybrush returns to Lounge
        await page1.EvaluateAsync("() => window.__ADVENTURE_GAME__.changeRoom('lounge')");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'lounge'");

        // Both players see each other again in Lounge
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Guybrush chats in Lounge
        await page1.FillAsync("#chat-input", "I am back from the garden!");
        await page1.ClickAsync("#chat-send");

        // Elaine now receives Guybrush's message
        await page2.WaitForFunctionAsync("() => document.getElementById('chat-messages').innerText.includes('I am back from the garden!')");
        var p2ChatFinal = await page2.InnerTextAsync("#chat-messages");
        Assert.That(p2ChatFinal, Does.Contain("I am back from the garden!"));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }
}

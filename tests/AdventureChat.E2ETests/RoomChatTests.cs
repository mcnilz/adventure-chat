using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class RoomChatTests
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
    public async Task ChatMessage_AppearsInBothBrowsers_WithCorrectSenderAndText()
    {
        var context1 = await _browser.NewContextAsync();
        var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Send a message from Guybrush
        await page1.FillAsync("#chat-input", "Hello Elaine! Welcome to the SCUMM bar.");
        await page1.ClickAsync("#chat-send");

        // Message should appear in page1 chat history
        await page1.WaitForSelectorAsync("#chat-messages .chat-message");
        var p1MsgText = await page1.InnerTextAsync("#chat-messages .chat-message:last-child");
        Assert.That(p1MsgText, Does.Contain("Guybrush:"));
        Assert.That(p1MsgText, Does.Contain("Hello Elaine! Welcome to the SCUMM bar."));

        // Message should appear in page2 chat history
        await page2.WaitForSelectorAsync("#chat-messages .chat-message");
        var p2MsgText = await page2.InnerTextAsync("#chat-messages .chat-message:last-child");
        Assert.That(p2MsgText, Does.Contain("Guybrush:"));
        Assert.That(p2MsgText, Does.Contain("Hello Elaine! Welcome to the SCUMM bar."));

        // Verify input field is cleared after sending
        var inputVal = await page1.InputValueAsync("#chat-input");
        Assert.That(inputVal, Is.Empty);

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task ChatMessage_TriggersSpeechBubble_OnBothLocalAndRemoteCharacters()
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

        // Send chat message from Guybrush
        await page1.FillAsync("#chat-input", "Look behind you, a three-headed monkey!");
        await page1.PressAsync("#chat-input", "Enter");

        // Verify speech bubble is set on Guybrush in page 1
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.speechBubble != null");
        var p1BubbleText = await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.speechBubble.text");
        Assert.That(p1BubbleText, Is.EqualTo("Look behind you, a three-headed monkey!"));

        // Verify speech bubble is set on remote character (Guybrush) in page 2
        await page2.WaitForFunctionAsync("() => { const r = Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0]; return r && r.speechBubble != null; }");
        var p2RemoteBubbleText = await page2.EvaluateAsync<string>("() => Array.from(window.__ADVENTURE_GAME__.remotePlayers.values())[0].speechBubble.text");
        Assert.That(p2RemoteBubbleText, Is.EqualTo("Look behind you, a three-headed monkey!"));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task ChatMessage_HtmlAndScriptTags_AreRenderedAsPlainText_XssSafe()
    {
        var context1 = await _browser.NewContextAsync();
        var context2 = await _browser.NewContextAsync();

        var page1 = await context1.NewPageAsync();
        var page2 = await context2.NewPageAsync();

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Send XSS payload
        var xssPayload = "<script>window.__XSS_PWNED__ = true;</script><b>Injected HTML</b>";
        await page1.FillAsync("#chat-input", xssPayload);
        await page1.ClickAsync("#chat-send");

        await page2.WaitForSelectorAsync("#chat-messages .chat-message");

        // Verify payload is rendered strictly as text
        var p2ChatText = await page2.InnerTextAsync("#chat-messages .chat-message:last-child .chat-text");
        Assert.That(p2ChatText, Is.EqualTo(xssPayload));

        // Verify no script executed or injected in DOM
        var hasScriptTag = await page2.EvaluateAsync<bool>("() => document.querySelector('#chat-messages script') != null");
        var xssVariable = await page2.EvaluateAsync<bool>("() => window.__XSS_PWNED__ === true");
        Assert.That(hasScriptTag, Is.False, "Script element must not exist in DOM");
        Assert.That(xssVariable, Is.False, "XSS script must not execute");

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task ChatTypingAndClicking_DoesNotMoveCharacter()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Initial position
        var initialPos = await page.EvaluateAsync<dynamic>("() => ({ x: window.__ADVENTURE_GAME__.character.position.x, y: window.__ADVENTURE_GAME__.character.position.y })");
        double initialX = (double)initialPos.x;
        double initialY = (double)initialPos.y;

        // Click and type in chat input
        await page.ClickAsync("#chat-input");
        await page.FillAsync("#chat-input", "Just testing character position...");
        await page.ClickAsync("#chat-send");

        await page.WaitForSelectorAsync("#chat-messages .chat-message");

        // Character position should remain unchanged and idle
        var currentPos = await page.EvaluateAsync<dynamic>("() => ({ x: window.__ADVENTURE_GAME__.character.position.x, y: window.__ADVENTURE_GAME__.character.position.y, state: window.__ADVENTURE_GAME__.character.state })");
        double currentX = (double)currentPos.x;
        double currentY = (double)currentPos.y;
        string state = (string)currentPos.state;

        Assert.That(currentX, Is.EqualTo(initialX));
        Assert.That(currentY, Is.EqualTo(initialY));
        Assert.That(state, Is.EqualTo("idle"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task NewParticipant_ReceivesRecentRoomHistory_UponJoining()
    {
        var context1 = await _browser.NewContextAsync();
        var page1 = await context1.NewPageAsync();

        page1.Console += (_, msg) => TestContext.Progress.WriteLine($"[P1 Console] {msg.Type}: {msg.Text}");

        await page1.GotoAsync($"{_server.BaseUrl}?name=Guybrush");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Guybrush sends two unique messages before Elaine joins
        var msg1 = $"History Test Msg 1 - {Guid.NewGuid():N}";
        var msg2 = $"History Test Msg 2 - {Guid.NewGuid():N}";

        await page1.FillAsync("#chat-input", msg1);
        await page1.PressAsync("#chat-input", "Enter");
        await page1.WaitForFunctionAsync($"() => window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === '{msg1}')");

        await page1.FillAsync("#chat-input", msg2);
        await page1.PressAsync("#chat-input", "Enter");
        await page1.WaitForFunctionAsync($"() => window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === '{msg2}')");

        // Elaine joins afterwards
        var context2 = await _browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();

        await page2.GotoAsync($"{_server.BaseUrl}?name=Elaine");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Elaine should see both previous messages in her chat history
        await page2.WaitForFunctionAsync($"() => window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === '{msg1}') && window.__ADVENTURE_GAME__.chatMessages.some(m => m.text === '{msg2}')");
        var messages = await page2.EvaluateAsync<string[]>("() => Array.from(document.querySelectorAll('#chat-messages .chat-message')).map(el => el.textContent)");

        Assert.That(messages.Any(m => m.Contains(msg1)), Is.True);
        Assert.That(messages.Any(m => m.Contains(msg2)), Is.True);

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }
}

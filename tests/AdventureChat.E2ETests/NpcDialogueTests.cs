using AdventureChat.Server.Models;
using AdventureChat.Server.Services;
using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcDialogueTests
{
    [Test]
    public async Task AnotherVisitorRingingTheBellRefreshesAnOpenConversation()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var speaker = await JoinWorld(browser, server.BaseUrl, "mara");
        var visitor = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Speak(speaker);
        var introduction = speaker.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" });
        var bellQuestion = speaker.GetByRole(AriaRole.Button, new() { Name = "Hast du die Klingel gehört?" });
        await Assertions.Expect(introduction).ToBeVisibleAsync();
        await Assertions.Expect(bellQuestion).ToBeHiddenAsync(new() { Timeout = 1000 });

        await visitor.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickCanvas(visitor, 203, 240);
        await Assertions.Expect(visitor.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.", new() { Timeout = 5000 });

        await introduction.ClickAsync();
        await Assertions.Expect(speaker.Locator("#npc-dialogue-notice")).ToContainTextAsync("Gespräch hat sich geändert");
        await Assertions.Expect(bellQuestion).ToBeVisibleAsync();
        await introduction.ClickAsync();
        await speaker.Keyboard.PressAsync(".");
        await Assertions.Expect(speaker.Locator("#npc-reply")).ToHaveTextAsync("Ich bin Nela und helfe dir gern.");
        await Assertions.Expect(visitor.Locator("#npc-reply")).ToBeEmptyAsync();
        if (Environment.GetEnvironmentVariable("NPC_STATE_DIALOGUE_EVIDENCE") is { Length: > 0 } evidence)
        {
            Directory.CreateDirectory(evidence);
            await speaker.Locator("#game-container").ScreenshotAsync(new() { Path = Path.Combine(evidence, "lounge-bell-dialogue.png") });
        }
        await speaker.Keyboard.PressAsync(".");

        await visitor.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickCanvas(visitor, 203, 240);
        await visitor.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'bell').image.endsWith('lounge-bell-ready.png')");
        await speaker.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" }).ClickAsync();
        await speaker.Keyboard.PressAsync(".");
        await Assertions.Expect(speaker.Locator("#npc-reply")).ToHaveTextAsync("Durch die Tür rechts erreichst du den Tavernen-Garten.");
        await speaker.Keyboard.PressAsync(".");
        await Assertions.Expect(bellQuestion).ToBeHiddenAsync();
    }

    [Test]
    public async Task EditorPreviewUsesItsOwnBellStateForTheConversation()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var live = await JoinWorld(browser, server.BaseUrl, "mara");
        var editor = await browser.NewPageAsync();
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await editor.SelectOptionAsync("#room-select", "lounge");
        await Assertions.Expect(editor.Locator("#object-list option[value=bell]")).ToHaveCountAsync(1);
        await editor.ClickAsync("#btn-toggle-mode");
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Lounge");

        var canvas = editor.Locator("#editor-canvas");
        var bounds = (await canvas.BoundingBoxAsync())!;
        Task ClickPreview(int x, int y) => canvas.ClickAsync(new() { Position = new() { X = x * bounds.Width / 640, Y = y * bounds.Height / 360 } });
        await editor.SelectOptionAsync("#preview-verb", "push");
        await ClickPreview(203, 240);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Aktion ausgeführt.");
        await editor.SelectOptionAsync("#preview-verb", "talk", new() { Timeout = 3000 });
        await ClickPreview(112, 225);
        var question = new PageGetByRoleOptions { Name = "Hast du die Klingel gehört?" };
        await Assertions.Expect(editor.GetByRole(AriaRole.Button, question)).ToBeVisibleAsync();
        await editor.GetByRole(AriaRole.Button, question).ClickAsync();
        await ClickPreview(210, 330);
        await Assertions.Expect(editor.Locator("#npc-reply")).ToContainTextAsync("Ja, ich habe deinen Ruf gehört.");
        await Assertions.Expect(editor.Locator("#npc-dialogue")).ToBeVisibleAsync();

        await Speak(live);
        await Assertions.Expect(live.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await Assertions.Expect(live.GetByRole(AriaRole.Button, question)).ToBeHiddenAsync();
    }

    [Test]
    public async Task FirstMatchingReplyWinsAndAnEmptyConversationCanBeClosed()
    {
        await using var server = CreateReferenceCopy();
        var path = Path.Combine(server.TempRoot, "world/rooms/lounge.yaml");
        var serializer = new RoomYamlSerializer();
        var draft = serializer.Deserialize(File.ReadAllText(path));
        draft.Npcs![0] = draft.Npcs[0] with { Tour = null, FollowDistance = null };
        var options = draft.Npcs!.Single().Options;
        var ready = new DialogueCondition("bell", "ready");
        for (var index = 0; index < options.Count; index++) options[index] = options[index] with { When = ready };
        options[0] = options[0] with { Replies = [new(ready, "Erste passende Antwort."), new(ready, "Zweite passende Antwort.")] };
        File.WriteAllText(path, serializer.Serialize(draft));
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var speaker = await JoinWorld(browser, server.BaseUrl, "mara");
        var visitor = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Speak(speaker);
        var introduction = speaker.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" });
        await introduction.ClickAsync();
        await speaker.Keyboard.PressAsync(".");
        await Assertions.Expect(speaker.Locator("#npc-reply")).ToHaveTextAsync("Erste passende Antwort.");
        await speaker.Keyboard.PressAsync(".");

        await visitor.GetByRole(AriaRole.Button, new() { Name = "Drücken", Exact = true }).ClickAsync();
        await ClickCanvas(visitor, 203, 240);
        await Assertions.Expect(visitor.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.");
        await introduction.ClickAsync();
        await Assertions.Expect(speaker.Locator("#npc-dialogue-notice")).ToHaveTextAsync("Im Moment gibt es keine Fragen.");
        await Assertions.Expect(introduction).ToBeHiddenAsync();
        await speaker.GetByRole(AriaRole.Button, new() { Name = "Gespräch beenden" }).ClickAsync();
        await Assertions.Expect(speaker.Locator("#verb-bar")).ToBeVisibleAsync();
        await Speak(speaker);
        await Assertions.Expect(speaker.Locator("#npc-dialogue-notice")).ToHaveTextAsync("Im Moment gibt es keine Fragen.");
        await speaker.Keyboard.PressAsync("Escape");
        await Assertions.Expect(speaker.Locator("#npc-dialogue")).ToBeHiddenAsync();
    }

    private const string WhitePixels = """
        ([x, y, width, height]) => {
          const pixels = document.querySelector('#game-canvas').getContext('2d').getImageData(x, y, width, height).data;
          let white = 0;
          for (let i = 0; i < pixels.length; i += 4) {
            if (pixels[i] === 255 && pixels[i + 1] === 255 && pixels[i + 2] === 255 && pixels[i + 3] === 255) white++;
          }
          return white;
        }
        """;

    [Test]
    public async Task VisitorsSpeakIndependentlyAndCanWalkAway()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await JoinWorld(browser, server.BaseUrl, "mara");
        var second = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Speak(first);
        await Assertions.Expect(first.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await Assertions.Expect(first.Locator("#verb-bar")).ToBeHiddenAsync();
        await Assertions.Expect(first.Locator("#action-text")).ToHaveTextAsync("Spreche mit Nela");
        await Assertions.Expect(first.Locator("#npc-dialogue-name")).ToHaveTextAsync("Nela");
        await first.WaitForFunctionAsync("Math.hypot(window.__ADVENTURE_GAME__.character.position.x - 146, window.__ADVENTURE_GAME__.character.position.y - 294) < 1");
        await first.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" }).ClickAsync();
        Assert.That(await first.Locator("#npc-reply").TextContentAsync(), Is.Empty);
        await Assertions.Expect(first.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" })).ToBeDisabledAsync();
        int[] questionArea = [100, 137, 100, 6];
        int[] answerArea = [25, 108, 70, 25];
        await first.WaitForFunctionAsync($"bounds => ({WhitePixels})(bounds) > 250", questionArea);
        await Assertions.Expect(first.Locator("#npc-reply")).ToHaveTextAsync("Ich bin Nela.");
        await first.WaitForFunctionAsync($"bounds => ({WhitePixels})(bounds) > 250", answerArea);
        Assert.That(await second.EvaluateAsync<int>(WhitePixels, answerArea), Is.LessThan(200));
        if (Environment.GetEnvironmentVariable("NPC_DIALOGUE_EVIDENCE") is { Length: > 0 } evidence)
        {
            Directory.CreateDirectory(evidence);
            await first.Locator("#game-container").ScreenshotAsync(new() { Path = Path.Combine(evidence, "lounge-dialogue.png") });
        }
        await ClickCanvas(first, 210, 330);
        await Assertions.Expect(first.Locator("#npc-reply")).ToHaveTextAsync("Ich passe auf die Lounge auf.", new() { Timeout = 1000 });
        await first.Keyboard.PressAsync(".");
        await Assertions.Expect(first.Locator("#npc-reply")).ToHaveTextAsync("", new() { Timeout = 1000 });
        Assert.That(await first.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.character.state === 'idle'"), Is.True);
        await Assertions.Expect(second.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await Speak(second);
        await second.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" }).ClickAsync();
        await second.Keyboard.PressAsync(".");
        await Assertions.Expect(second.Locator("#npc-reply")).ToHaveTextAsync("Durch die Tür rechts erreichst du den Tavernen-Garten.", new() { Timeout = 1000 });
        await second.Locator("#chat-input").PressAsync(".");
        await Assertions.Expect(second.Locator("#chat-input")).ToHaveValueAsync(".");
        await Assertions.Expect(second.Locator("#npc-reply")).ToHaveTextAsync("Durch die Tür rechts erreichst du den Tavernen-Garten.");
        await Assertions.Expect(first.Locator("#chat-messages")).Not.ToContainTextAsync("Ich passe auf");
        await Assertions.Expect(second.Locator("#chat-messages")).Not.ToContainTextAsync("erreichst du");

        await first.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" }).ClickAsync();
        await first.WaitForFunctionAsync($"bounds => ({WhitePixels})(bounds) > 250", questionArea);
        await first.Keyboard.PressAsync("Escape");
        await Assertions.Expect(first.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await Assertions.Expect(first.Locator("#verb-bar")).ToBeVisibleAsync();
        await first.WaitForFunctionAsync($"bounds => ({WhitePixels})(bounds) < 200", questionArea, new() { Timeout = 1000 });
        await ClickCanvas(first, 210, 330);
        await first.WaitForFunctionAsync("Math.abs(window.__ADVENTURE_GAME__.character.position.x - 210) < 2");
        await ClickCanvas(first, 572, 215);
        await first.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'garden'");
        await ClickCanvas(first, 66, 225);
        await first.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'lounge'");
        Assert.That(await first.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.npcs.length"), Is.EqualTo(1));
        Assert.That(await first.Locator("#npc-reply").TextContentAsync(), Is.Empty);
        await Assertions.Expect(second.Locator("#npc-dialogue")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ClosedPassagePreventsConversationUntilTheNpcIsReachable()
    {
        await using var server = CreateServer();
        var world = Path.Combine(server.TempRoot, "world");
        var reference = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        var manifest = Path.Combine(world, "world.yaml");
        File.Copy(Path.Combine(reference, "assets/images/bruno.png"), Path.Combine(world, "assets/images/bruno.png"));
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("1.8.0", "1.11.0", StringComparison.Ordinal) + """

            defaultCharacterId: bruno
            characters:
              - { id: bruno, name: Bruno, image: assets/images/bruno.png, frameWidth: 128, frameHeight: 128 }
            """);
        var longReply = new string('界', 125);
        File.AppendAllText(Path.Combine(world, "rooms/hall.yaml"), $$"""

            npcs:
              - id: guard
                name: Wache
                characterId: bruno
                position: { x: 485, y: 300 }
                talkPoint: { x: 453, y: 300 }
                options:
                  - { id: hello, text: Hallo, reply: {{longReply}} }
                  - { id: bye, text: Auf Wiedersehen, reply: Bis bald }
            """);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await JoinWorld(browser, server.BaseUrl, "bruno");

        await Speak(page, 485, 275, "Wache");
        await Assertions.Expect(page.Locator("#action-feedback")).ToHaveTextAsync("Der Weg zur Figur ist nicht erreichbar.");
        await Assertions.Expect(page.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Benutzen", Exact = true }).ClickAsync();
        await ClickCanvas(page, 290, 250);
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.exits.length === 1");

        await Speak(page, 485, 275, "Wache");
        await Assertions.Expect(page.Locator("#npc-dialogue-name")).ToHaveTextAsync("Wache");
        await Assertions.Expect(page.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Hallo", Exact = true }).ClickAsync();
        await page.Keyboard.PressAsync(".");
        await Assertions.Expect(page.Locator("#npc-reply")).ToHaveTextAsync(longReply[..110]);
        await page.Keyboard.PressAsync(".");
        await Assertions.Expect(page.Locator("#npc-reply")).ToHaveTextAsync(longReply[110..]);
    }

    [Test]
    public async Task RoomChangeInterruptsTheApproachAndReportsItLocally()
    {
        await using var server = CreateReferenceCopy();
        var lounge = Path.Combine(server.TempRoot, "world/rooms/lounge.yaml");
        // Keep the real exit usable for a short stretch of the approach.
        File.WriteAllText(lounge, File.ReadAllText(lounge).Replace("radius: 28", "radius: 100", StringComparison.Ordinal));
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await JoinWorld(browser, server.BaseUrl, "mara");
        await page.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x: 572, y: 244})");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'idle' && window.__ADVENTURE_GAME__.character.position.x > 570");

        await Speak(page);
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'walk'");
        await ClickCanvas(page, 572, 215);
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'garden'");
        await Assertions.Expect(page.Locator("#action-feedback")).ToHaveTextAsync("Gespräch abgebrochen. Du hast einen Ausgang gewählt.");
        await Assertions.Expect(page.Locator("#npc-dialogue")).ToBeHiddenAsync();
    }

    private static async Task<IPage> JoinWorld(IBrowser browser, string url, string character)
    {
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await page.GotoAsync(url);
        await page.FillAsync("#player-name", character);
        await page.SelectOptionAsync("#character-select", character);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.sessionId !== 'local-player' && window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        return page;
    }

    private static async Task Speak(IPage page, int x = 112, int y = 225, string name = "Nela")
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Spreche mit", Exact = true }).ClickAsync(new() { Timeout = 5000 });
        var canvas = page.Locator("#game-canvas");
        var box = (await canvas.BoundingBoxAsync())!;
        await canvas.HoverAsync(new() { Position = new() { X = x * box.Width / 640, Y = y * box.Height / 360 } });
        await Assertions.Expect(page.Locator("#action-text")).ToHaveTextAsync($"Spreche mit {name}");
        await ClickCanvas(page, x, y);
    }
}

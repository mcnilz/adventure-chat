using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class NpcDialogueTests
{
    [Test]
    public async Task VisitorsSpeakIndependentlyAndCanWalkAway()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await JoinWorld(browser, server.BaseUrl, "mara");

        await Speak(first);
        await Assertions.Expect(first.Locator("#npc-dialogue")).ToBeVisibleAsync();
        await Assertions.Expect(first.Locator("#npc-dialogue-name")).ToHaveTextAsync("Nela");
        await first.WaitForFunctionAsync("Math.hypot(window.__ADVENTURE_GAME__.character.position.x - 146, window.__ADVENTURE_GAME__.character.position.y - 294) < 1");
        await first.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" }).ClickAsync();
        if (Environment.GetEnvironmentVariable("NPC_DIALOGUE_EVIDENCE") is { Length: > 0 } evidence)
        {
            Directory.CreateDirectory(evidence);
            await first.Locator("#game-container").ScreenshotAsync(new() { Path = Path.Combine(evidence, "lounge-dialogue.png") });
        }
        var second = await JoinWorld(browser, server.BaseUrl, "bruno");
        await Assertions.Expect(second.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await Speak(second);
        await second.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" }).ClickAsync();
        await Assertions.Expect(first.Locator("#npc-reply")).ToHaveTextAsync("Ich bin Nela. Ich passe auf die Lounge auf.");
        await Assertions.Expect(second.Locator("#npc-reply")).ToHaveTextAsync("Durch die Tür rechts erreichst du den Tavernen-Garten.");
        await Assertions.Expect(first.Locator("#chat-messages")).Not.ToContainTextAsync("Ich passe auf");
        await Assertions.Expect(second.Locator("#chat-messages")).Not.ToContainTextAsync("erreichst du");

        await first.Keyboard.PressAsync("Escape");
        await Assertions.Expect(first.Locator("#npc-dialogue")).ToBeHiddenAsync();
        await ClickCanvas(first, 210, 330);
        await first.WaitForFunctionAsync("Math.abs(window.__ADVENTURE_GAME__.character.position.x - 210) < 2");
        await ClickCanvas(first, 572, 215);
        await first.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'garden'");
        await ClickCanvas(first, 66, 225);
        await first.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'lounge'");
        Assert.That(await first.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.npcs.length"), Is.EqualTo(1));
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
        File.AppendAllText(Path.Combine(world, "rooms/hall.yaml"), """

            npcs:
              - id: guard
                name: Wache
                characterId: bruno
                position: { x: 485, y: 300 }
                talkPoint: { x: 453, y: 300 }
                options:
                  - { id: hello, text: Hallo, reply: Willkommen }
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

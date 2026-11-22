using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.NpcDialogueTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class SpeechAnimationTests
{
    internal const string Sample = """
        async ([selector, x, y, width, height, duration]) => {
          const context = document.querySelector(selector).getContext('2d');
          const start = performance.now();
          const frames = new Set();
          do {
            await new Promise(requestAnimationFrame);
            frames.add(context.getImageData(x, y, width, height).data.join(','));
          } while (performance.now() - start < duration);
          return frames.size;
        }
        """;

    [Test]
    public async Task SpeechPreservesTheWalkingPoseAndLeavesTheBackViewAlone()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        var result = await page.EvaluateAsync<int[]>("""
            async () => {
              const { CharacterRenderer } = await import('/js/rendering/character-renderer.js');
              const { characters } = await (await fetch('/api/worlds/reference/characters')).json();
              const appearance = characters.find(character => character.id === 'nela');
              const renderer = new CharacterRenderer();
              await Promise.all([appearance.imageUrl, appearance.speechImageUrl, appearance.waveImageUrl]
                .map(url => renderer.prepare(url)));
              const canvas = document.createElement('canvas');
              canvas.width = canvas.height = 160;
              const context = canvas.getContext('2d');
              const character = { appearance, position: { x: 80, y: 145 },
                state: 'walk', facing: 'right', walkFrame: 2 };
              const draw = speech => {
                context.clearRect(0, 0, 160, 160);
                renderer.drawCharacter(context, character, true, { actorHeight: 128 }, false, speech);
              };
              const crop = (y, height) => context.getImageData(20, y, 120, height).data.join(',');
              draw();
              const head = crop(15, 40), body = crop(55, 100);
              draw({ elapsed: 0, gesture: 'wave' });
              const changedLips = crop(15, 40) !== head;
              const sameWalkingBody = crop(55, 100) === body;
              character.facing = 'up';
              draw();
              const back = crop(15, 135);
              draw({ elapsed: 0, gesture: null });
              return [changedLips, sameWalkingBody, crop(15, 135) === back].map(Number);
            }
            """);
        Assert.That(result, Is.EqualTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task RoomChatMovesTheMouthInBothViewsAndStopsWhenTheBubbleExpires()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var speaker = await JoinWorld(browser, server.BaseUrl, "mara");
        var observer = await JoinWorld(browser, server.BaseUrl, "bruno");
        await ClickCanvas(speaker, 205, 329);
        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'idle' && Math.abs(window.__ADVENTURE_GAME__.character.position.x - 205) < 2");
        await observer.WaitForFunctionAsync("[...window.__ADVENTURE_GAME__.remotePlayers.values()].some(p => p.state === 'idle' && Math.abs(p.position.x - 205) < 2)");

        await speaker.FillAsync("#chat-input", "Hallo aus der Lounge!");
        await speaker.PressAsync("#chat-input", "Enter");
        await Assertions.Expect(observer.Locator("#chat-messages")).ToContainTextAsync("Hallo aus der Lounge!");
        object[] lips = ["#game-canvas", 194, 219, 27, 18, 450];
        var phases = await Task.WhenAll(speaker.EvaluateAsync<int>(Sample, lips), observer.EvaluateAsync<int>(Sample, lips));
        Assert.That(phases, Is.All.GreaterThan(1));

        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble === null");
        phases = await Task.WhenAll(speaker.EvaluateAsync<int>(Sample, lips), observer.EvaluateAsync<int>(Sample, lips));
        Assert.That(phases, Is.All.EqualTo(1));

        await ClickCanvas(speaker, 561, 329);
        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'walk'");
        await speaker.FillAsync("#chat-input", "Ich laufe zur Tür.");
        await speaker.PressAsync("#chat-input", "Enter");
        await speaker.WaitForFunctionAsync("""
            () => {
              const actor = window.__ADVENTURE_GAME__.character;
              if (actor.state !== 'walk' || actor.facing !== 'right') return false;
              const context = document.querySelector('#game-canvas').getContext('2d');
              const pixels = context.getImageData(Math.round(actor.position.x) + 8, Math.round(actor.position.y) - 108, 12, 12).data;
              for (let index = 0; index < pixels.length; index += 4) {
                if (pixels[index] === 73 && pixels[index+1] === 41 && pixels[index+2] === 43) return true;
              }
              return false;
            }
            """, null, new() { Timeout = 3000 });
        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'idle'");
        await speaker.FillAsync("#chat-input", "Bis gleich im Garten.");
        await speaker.PressAsync("#chat-input", "Enter");
        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Bis gleich im Garten.'");
        await ClickCanvas(speaker, 572, 215);
        await speaker.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'garden'");
        Assert.That(await speaker.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.character.speechBubble === null"), Is.True);
        Assert.That(await observer.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.id"), Is.EqualTo("lounge"));
    }

    [Test]
    public async Task NelaIsAvailableAsAnNpcButCannotBeSelectedForEntry()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        await Assertions.Expect(page.Locator("#character-select option")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("#character-select option[value=nela]")).ToHaveCountAsync(0);
        var rejected = await page.EvaluateAsync<bool>("""
            async () => {
              const connection = new signalR.HubConnectionBuilder().withUrl('/hub/room').build();
              await connection.start();
              try {
                const result = await connection.invoke('JoinRoom',
                  { worldId: 'reference', roomId: 'lounge', displayName: 'Tester', characterId: 'nela' });
                return !result.success;
              } finally { await connection.stop(); }
            }
            """);
        Assert.That(rejected, Is.True);
    }

    [Test]
    public async Task NelaWavesOnlyInThePrivateReplyThatRequestsItAndStopsWhenSkipped()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var first = await JoinWorld(browser, server.BaseUrl, "mara");
        var second = await JoinWorld(browser, server.BaseUrl, "bruno");
        await Speak(first);
        await Speak(second);
        await second.GetByRole(AriaRole.Button, new() { Name = "Wo ist der Garten?" }).ClickAsync();
        await second.Keyboard.PressAsync(".");
        await Assertions.Expect(second.Locator("#npc-reply")).ToContainTextAsync("Tavernen-Garten");

        await first.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" }).ClickAsync();
        await first.Keyboard.PressAsync(".");
        await Assertions.Expect(first.Locator("#npc-reply")).ToHaveTextAsync("Ich bin Nela.");
        object[] lips = ["#game-canvas", 109, 184, 8, 5, 350];
        object[] hand = ["#game-canvas", 70, 186, 20, 27, 350];
        var changes = await Task.WhenAll(first.EvaluateAsync<int>(Sample, lips), first.EvaluateAsync<int>(Sample, hand),
            second.EvaluateAsync<int>(Sample, lips), second.EvaluateAsync<int>(Sample, hand));
        Assert.That(changes[0], Is.GreaterThan(1), "Private gestured reply has mouth phases.");
        Assert.That(changes[1], Is.GreaterThan(1), "The hand moves during the first reply.");
        Assert.That(changes[2], Is.GreaterThan(1), "The other visitor sees their own reply's mouth phases.");
        Assert.That(changes[3], Is.EqualTo(1), "The other reply has no gesture.");

        await first.Keyboard.PressAsync(".");
        await Assertions.Expect(first.Locator("#npc-reply")).ToContainTextAsync("passe auf");
        Assert.That(await first.EvaluateAsync<int>(Sample, hand), Is.EqualTo(1), "Skipping ends the gesture.");
        await first.Keyboard.PressAsync("Escape");
        Assert.That(await first.EvaluateAsync<int>(Sample, lips), Is.EqualTo(1), "Closing ends the mouth phases.");
    }

    [Test]
    public async Task EditorPreviewDrawsTheSameGestureAndMouthPhases()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await page.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await page.SelectOptionAsync("#room-select", "lounge");
        await Assertions.Expect(page.Locator("#object-list option[value=bell]")).ToHaveCountAsync(1);
        await page.RunAndWaitForResponseAsync(() => page.ClickAsync("#btn-toggle-mode"),
            response => response.Url.Contains("/preview-snapshot", StringComparison.Ordinal) && response.Ok);
        await Assertions.Expect(page.Locator("#preview-result")).ToHaveTextAsync("Lounge");
        await page.SelectOptionAsync("#preview-verb", "talk");
        var canvas = page.Locator("#editor-canvas");
        var bounds = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = 112 * bounds.Width / 640, Y = 225 * bounds.Height / 360 } });
        await page.GetByRole(AriaRole.Button, new() { Name = "Wer bist du?" }).ClickAsync();
        await page.Keyboard.PressAsync(".");
        await Assertions.Expect(page.Locator("#npc-reply")).ToHaveTextAsync("Ich bin Nela.");
        var changes = await Task.WhenAll(
            page.EvaluateAsync<int>(Sample, new object[] { "#editor-canvas", 109, 184, 8, 5, 350 }),
            page.EvaluateAsync<int>(Sample, new object[] { "#editor-canvas", 70, 186, 20, 27, 350 }));
        Assert.That(changes, Is.All.GreaterThan(1));
    }
}

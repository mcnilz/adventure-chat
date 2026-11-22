using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class ObjectActorAnimationTests
{
    private const string MaraSeated = """
        () => {
          const { remotePlayers } = window.__ADVENTURE_GAME__;
          return [...remotePlayers.values()].some(actor => actor.name === 'mara' && actor.facing === 'down'
            && actor.position.x === 218 && actor.position.y === 246 && actor.actorAnimation?.durationMs === null);
        }
        """;
    private const string MaraGestures = """
        () => [...window.__ADVENTURE_GAME__.remotePlayers.values()].some(actor => actor.name === 'mara'
          && actor.facing === 'up' && actor.position.x === 175 && actor.position.y === 130
          && actor.actorAnimation?.durationMs === 1500)
        """;

    [Test]
    public async Task SitAndGesture_AreVisibleToOthersAndRespectOccupancyCancellationAndArrival()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var mara = await JoinWorkshop(browser, server.BaseUrl, "mara");
        var bruno = await JoinWorkshop(browser, server.BaseUrl, "bruno");

        await Act(mara, "Benutzen", 169, 194);
        await mara.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text === 'Das geht nicht.'");
        await ObjectShows(mara, "chair", "workshop-chair.png", 148);
        await Act(mara, "Ziehen", 169, 194);
        await ObjectShows(bruno, "chair", "workshop-chair.png", 204);
        await Sit(mara);
        await bruno.WaitForFunctionAsync(MaraSeated);
        await Assertions.Expect(mara.Locator("#action-choices")).ToHaveCountAsync(0);
        await HasPixel(bruno, 204, 198, 30, 26, 229, 189, 148);
        await Capture(bruno, "held-pose");
        await Sit(bruno);
        await bruno.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text.includes('bereits belegt')");
        await bruno.WaitForFunctionAsync(MaraSeated);

        await Act(bruno, "Drücken", 224, 214);
        await ObjectShows(mara, "chair", "workshop-chair.png", 148);
        await mara.WaitForFunctionAsync("!window.__ADVENTURE_GAME__.character.actorAnimation");
        await Act(mara, "Ziehen", 169, 194);
        await ObjectShows(bruno, "chair", "workshop-chair.png", 204);
        await Sit(mara);
        await bruno.WaitForFunctionAsync(MaraSeated);
        await Sit(mara);
        await bruno.WaitForFunctionAsync(MaraSeated);
        await Assertions.Expect(mara.Locator("#verb-bar button[data-verb=walk]")).ToHaveAttributeAsync("aria-pressed", "true");
        await ClickCanvas(mara, 247, 260);
        await WaitAt(mara, 247, 260);
        await Sit(bruno);
        await bruno.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.actorAnimation?.durationMs === null");

        await Act(mara, "Benutzen", 132, 52);
        await mara.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.state === 'walk'");
        await Act(mara, "Benutzen", 256, 280);
        await WaitAt(mara, 175, 130);
        await Task.Delay(1600);
        await ObjectShows(bruno, "board", "empty.png", 96);

        await Act(mara, "Gehe zu", 248, 154);
        await WaitAt(mara, 248, 154);
        await Act(mara, "Benutzen", 132, 52);
        await bruno.WaitForFunctionAsync(MaraGestures);
        await ObjectShows(bruno, "board", "empty.png", 96);
        await ClickCanvas(mara, 250, 152);
        await WaitAt(mara, 250, 152);
        await Task.Delay(1600);
        await ObjectShows(bruno, "board", "empty.png", 96);

        await Act(mara, "Benutzen", 132, 52);
        await bruno.WaitForFunctionAsync(MaraGestures);
        await HasPixel(bruno, 188, 67, 13, 13, 229, 189, 148);
        await Capture(bruno, "raised-hand");
        await ObjectShows(bruno, "board", "written.png", 96);
        await Capture(bruno, "board-written");
        await bruno.CloseAsync();
        var late = await JoinWorkshop(browser, server.BaseUrl, "bruno");
        await ObjectShows(late, "board", "written.png", 96);
        await Sit(late);
        await late.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.actorAnimation?.durationMs === null");
    }

    [Test]
    public async Task Editor_SavesReloadsAndPreviewsAnAnimationAtTheSelectedPoint()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var editor = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } });
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await editor.SelectOptionAsync("#room-select", "workshop");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("'Werkstatt' geladen");
        await editor.SelectOptionAsync("#object-list", "board");
        var duration = editor.GetByLabel("Gestendauer (ms)", new() { Exact = true });
        await duration.FillAsync("75");
        await duration.PressAsync("Tab");
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#error-list")).ToContainTextAsync("200–5000");
        await duration.FillAsync("1800");
        await duration.PressAsync("Tab");
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("erfolgreich atomar gespeichert");
        await editor.ReloadAsync();
        await editor.SelectOptionAsync("#room-select", "workshop");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("'Werkstatt' geladen");
        await editor.SelectOptionAsync("#object-list", "board");
        await Assertions.Expect(duration).ToHaveValueAsync("1800");
        await Assertions.Expect(editor.GetByLabel("Blickrichtung der Animation")).ToHaveValueAsync("up");

        await editor.ClickAsync("#btn-toggle-mode");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("use");
        await ClickCanvas(editor, 495, 77);
        await HasPixel(editor, 551, 132, 1, 3, 232, 177, 91);
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("pull");
        await ClickCanvas(editor, 169, 194);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Aktion ausgeführt.");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("use");
        await ClickCanvas(editor, 224, 214);
        await HasPixel(editor, 204, 198, 30, 26, 229, 189, 148);
        await ClickCanvas(editor, 132, 52);
        await HasPixel(editor, 188, 67, 13, 13, 229, 189, 148);
        await HasPixel(editor, 118, 30, 38, 21, 240, 220, 164);
    }

    private static async Task Act(IPage page, string verb, int x, int y)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = verb, Exact = true }).ClickAsync();
        await ClickCanvas(page, x, y);
    }

    private static async Task Sit(IPage page)
    {
        await Act(page, "Benutzen", 224, 214);
    }

    private static Task ObjectShows(IPage page, string id, string image, int x) => page.WaitForFunctionAsync("""
        ([id, image, x]) => {
          const object = window.__ADVENTURE_GAME__.room.objects.find(candidate => candidate.id === id);
          return object?.image.endsWith(image) && object.position.x === x;
        }
        """, new object[] { id, image, x });

    private static Task WaitAt(IPage page, int x, int y) => page.WaitForFunctionAsync("""
        ([x, y]) => {
          const actor = window.__ADVENTURE_GAME__.character;
          return actor.state === 'idle' && !actor.actorAnimation && Math.hypot(actor.position.x - x, actor.position.y - y) < 2;
        }
        """, new[] { x, y });

    private static Task HasPixel(IPage page, int x, int y, int width, int height, int red, int green, int blue) =>
        page.WaitForFunctionAsync("""
            ([x, y, w, h, r, g, b]) => {
              const canvas = document.querySelector('canvas');
              const pixels = canvas.getContext('2d').getImageData(x, y, w, h).data;
              for (let i = 0; i < pixels.length; i += 4)
                if (pixels[i] === r && pixels[i + 1] === g && pixels[i + 2] === b) return true;
              return false;
            }
            """, new[] { x, y, width, height, red, green, blue }, new() { Timeout = 8000 });
}

using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class ObjectTransitionTests
{
    private const string PlateHighlight = """
        () => {
          const pixels = document.querySelector('canvas').getContext('2d').getImageData(551, 130, 1, 76).data;
          for (let i = 0; i < pixels.length; i += 4) {
            if (pixels[i] === 232 && pixels[i + 1] === 177 && pixels[i + 2] === 91) return 130 + i / 4;
          }
          return -1;
        }
        """;
    private const string ChairLeftEdge = """
        () => {
          const pixels = document.querySelector('canvas').getContext('2d').getImageData(145, 195, 100, 1).data;
          for (let i = 0; i < pixels.length; i += 4) {
            if (pixels[i] === 240 && pixels[i + 1] === 20 && pixels[i + 2] === 40) return 145 + i / 4;
          }
          return -1;
        }
        """;

    [Test]
    public async Task LiftButtons_AnimateBothDirectionsAndShowTheCurrentProgressToNewVisitors()
    {
        await using var server = CreateReferenceCopy();
        var manifest = Path.Combine(server.TempRoot, "world", "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("startRoom: lounge", "startRoom: workshop"));
        var workshop = Path.Combine(server.TempRoot, "world", "rooms", "workshop.yaml");
        File.WriteAllText(workshop, File.ReadAllText(workshop).Replace("durationMs: 1800", "durationMs: 4500"));
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var pages = await Task.WhenAll(Prepare(browser, server.BaseUrl, "Mara"), Prepare(browser, server.BaseUrl, "Bruno"),
            Prepare(browser, server.BaseUrl, "Besuch"));
        var a = pages[0];
        var b = pages[1];
        var late = pages[2];
        await Task.WhenAll(Enter(a), Enter(b));
        await Task.WhenAll(WalkToButton(a, 495), WalkToButton(b, 512));
        await Task.WhenAll(AtHeight(a, 179), AtHeight(b, 179));

        await PressButton(a, 495);
        await b.WaitForFunctionAsync("""
            window.__ADVENTURE_GAME__.room.objects.find(object => object.id === 'lift-plate').position.y === 132
            """, null, new() { Timeout = 4000 });
        await AtIntermediateHeight(b);
        await Task.WhenAll(PressButton(b, 530), Enter(late));
        await b.WaitForFunctionAsync("window.__ADVENTURE_GAME__.character.speechBubble?.text.includes('bewegt sich noch')");
        await AtIntermediateHeight(late);
        await Task.WhenAll(AtHeight(a, 134), AtHeight(b, 134), AtHeight(late, 134));
        await Capture(a, "lift-raised");

        await PressButton(b, 530);
        await AtIntermediateHeight(a);
        await Task.WhenAll(AtHeight(a, 179), AtHeight(b, 179));
        await Capture(a, "lift-lowered");
        var after = await Prepare(browser, server.BaseUrl, "Nach der Fahrt");
        await Enter(after);
        await AtHeight(after, 179);

        await Task.WhenAll(PressButton(a, 495, "Benutzen"), PressButton(b, 495, "Benutzen"));
        await Task.WhenAll(AtHeight(a, 134), AtHeight(b, 134));
        await PressButton(b, 530, "Benutzen");
        await Task.WhenAll(AtHeight(a, 179), AtHeight(b, 179));
    }

    [Test]
    public async Task Editor_UpgradesExplicitlyAndReusesDirectionalTransitionsOnAnotherObject()
    {
        await using var server = CreateServer();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var editor = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } });
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=navigation");
        await SelectChair(editor);
        await editor.GetByRole(AriaRole.Button, new() { Name = "Übergang hinzufügen", Exact = true }).ClickAsync();
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#error-list")).ToContainTextAsync("1.9.0");
        await editor.GetByLabel("Weltformat", new() { Exact = true }).SelectOptionAsync("1.9.0");
        var duration = editor.GetByLabel("Übergang 1 Dauer (ms)", new() { Exact = true });
        await duration.FillAsync("20");
        await duration.PressAsync("Tab");
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#error-list")).ToContainTextAsync("100–5000");
        await duration.FillAsync("950");
        await duration.PressAsync("Tab");
        await editor.GetByRole(AriaRole.Button, new() { Name = "Übergang hinzufügen", Exact = true }).ClickAsync();
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("erfolgreich atomar gespeichert");
        await editor.ReloadAsync();
        await SelectChair(editor);
        await Assertions.Expect(duration).ToHaveValueAsync("950");
        await Assertions.Expect(editor.GetByLabel("Übergang 2 Dauer (ms)", new() { Exact = true })).ToHaveValueAsync("600");
        await Assertions.Expect(editor.GetByLabel("Übergang 1 von", new() { Exact = true })).ToHaveValueAsync("table");
        await Assertions.Expect(editor.GetByLabel("Übergang 2 von", new() { Exact = true })).ToHaveValueAsync("pulled");
        await editor.ClickAsync("#btn-toggle-mode");
        await ChairAt(editor, 150);
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("pull");
        await ClickCanvas(editor, 155, 197);
        await ChairInMotion(editor);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Aktion ausgeführt.");
        await ChairAt(editor, 200);
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("push");
        await ClickCanvas(editor, 205, 197);
        await ChairInMotion(editor);
        await ChairAt(editor, 150);
    }

    private static async Task SelectChair(IPage editor)
    {
        await Assertions.Expect(editor.Locator("#room-select")).ToHaveValueAsync("hall");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("Dynamische Durchgänge' geladen");
        await editor.SelectOptionAsync("#object-list", "chair");
    }

    private static async Task<IPage> Prepare(IBrowser browser, string url, string name)
    {
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await page.GotoAsync(url);
        await page.FillAsync("#player-name", name);
        await Assertions.Expect(page.Locator("#world-enter")).ToBeEnabledAsync();
        return page;
    }

    private static async Task Enter(IPage page)
    {
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
    }

    private static async Task WalkToButton(IPage page, int x)
    {
        await ClickCanvas(page, x, 116);
        await page.WaitForFunctionAsync("""
            x => {
              const actor = window.__ADVENTURE_GAME__.character;
              return actor.state === 'idle' && Math.hypot(actor.position.x - x, actor.position.y - 116) < 2;
            }
            """, x);
    }

    private static async Task PressButton(IPage page, int x, string verb = "Drücken")
    {
        await page.GetByRole(AriaRole.Button, new() { Name = verb, Exact = true }).ClickAsync();
        await ClickCanvas(page, x, 79);
    }

    private static Task AtHeight(IPage page, int y) =>
        page.WaitForFunctionAsync($"expected => ({PlateHighlight})() === expected", y);

    private static Task AtIntermediateHeight(IPage page) =>
        page.WaitForFunctionAsync($"() => {{ const height = ({PlateHighlight})(); return height > 138 && height < 175; }}",
            null, new() { Timeout = 6000 });

    private static Task ChairAt(IPage editor, int x) =>
        editor.WaitForFunctionAsync($"expected => ({ChairLeftEdge})() === expected", x);

    private static Task ChairInMotion(IPage editor) =>
        editor.WaitForFunctionAsync($"() => {{ const x = ({ChairLeftEdge})(); return x > 155 && x < 195; }}",
            null, new() { Timeout = 4000 });
}

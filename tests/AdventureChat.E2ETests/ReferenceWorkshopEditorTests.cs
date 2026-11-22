using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class ReferenceWorkshopEditorTests
{
    [Test]
    public async Task WorkshopEditor_SavesTheSceneAndPreviewsItsActionsAndSafetyInIsolation()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var editor = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 1000 } });
        await editor.Clock.InstallAsync(new());
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await SelectWorkshop(editor);
        await editor.SelectOptionAsync("#object-list", "gate");
        await Assertions.Expect(editor.GetByLabel("Ausgang sperren: Gartentor")).ToBeCheckedAsync();
        await editor.SelectOptionAsync("#object-list", "switch");
        await Assertions.Expect(editor.GetByLabel("Zielobjekt")).ToHaveValueAsync("workshop/gate");
        await editor.SelectOptionAsync("#object-list", "chair");
        await editor.GetByLabel("Zustand ansehen").SelectOptionAsync("pulled");
        await editor.GetByLabel("Zustandsname").FillAsync("Vom Tisch weg");
        await editor.GetByLabel("Zustandsname").PressAsync("Tab");
        var draft = await editor.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
        await editor.ClickAsync("#btn-save");
        await Assertions.Expect(editor.Locator("#status-banner")).ToContainTextAsync("erfolgreich atomar gespeichert");
        await editor.ReloadAsync();
        await SelectWorkshop(editor);
        Assert.That(await editor.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(draft));
        await editor.ClickAsync("#btn-toggle-mode");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("walk");
        var step = editor.WaitForResponseAsync(candidate => candidate.Url.Contains("/preview-path"));
        await ClickCanvas(editor, 222, 230);
        await (await step).FinishedAsync();
        await editor.Clock.RunForAsync(2500);
        await PreviewAction(editor, "pull", ChairAtTable, "Diese Änderung würde eine Figur überdecken.");

        await editor.ClickAsync("#btn-toggle-mode");
        await editor.ClickAsync("#btn-toggle-mode");
        await PreviewAction(editor, "pull", ChairAtTable);
        await PreviewAction(editor, "close", ReturnDoor);
        await PreviewAction(editor, "open", ReturnDoor);
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("walk");
        await ClickCanvas(editor, ReturnDoor.X, ReturnDoor.Y);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Raum: Tavernen-Garten");
        await ClickCanvas(editor, WorkshopEntrance.X, WorkshopEntrance.Y);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Raum: Werkstatt");
        await PreviewAction(editor, "use", SwitchPanel);
        await Capture(editor, "editor-preview");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("walk");
        await ClickCanvas(editor, GateOpening.X, GateOpening.Y);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Raum: Tavernen-Garten");
        await ClickCanvas(editor, WorkshopEntrance.X, WorkshopEntrance.Y);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Raum: Werkstatt");
        await PreviewAction(editor, "close", GateBeam);
        await editor.ClickAsync("#btn-toggle-mode");
        Assert.That(await editor.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(draft));

        var live = await JoinWorkshop(browser, server.BaseUrl, "mara");
        await ExpectGate(live, false);
        Assert.That(await live.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.objects.find(o => o.id === 'door').image"),
            Does.EndWith("door-open.png"));
    }

    private static async Task SelectWorkshop(IPage page)
    {
        await page.SelectOptionAsync("#room-select", "workshop");
        await page.WaitForFunctionAsync("window.__ROOM_EDITOR__?.getState().getRoom().id === 'workshop'");
        await page.Locator("#object-list option[value=gate]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
    }

    private static async Task PreviewAction(IPage page, string verb, (int X, int Y) target,
        string expected = "Aktion ausgeführt.")
    {
        await page.GetByLabel("Vorschauverb").SelectOptionAsync(verb);
        await ClickCanvas(page, target.X, target.Y);
        await Assertions.Expect(page.Locator("#preview-result")).ToHaveTextAsync(expected, new() { Timeout = 15000 });
    }
}

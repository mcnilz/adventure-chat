using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class EditorDraftTests
{
    private TestServerFixture _server = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    [SetUp]
    public async Task Start()
    {
        _server = new TestServerFixture(authoring: true);
        await _server.StartAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    [TearDown]
    public async Task Stop()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_server is not null) await _server.DisposeAsync();
    }

    [Test]
    public async Task InspectorEdit_ProtectsUnsavedChanges_AndSurvivesSaveAndReload()
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        await page.Locator("#editor-canvas").ClickAsync(new() { Position = new() { X = 40, Y = 240 } });
        await Assertions.Expect(page.Locator("#inspector-vertex-x")).ToHaveValueAsync("40");
        await Save(page);

        await page.Locator("#inspector-vertex-x").FillAsync("45");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeVisibleAsync(new() { Timeout = 2000 });

        var warning = new TaskCompletionSource<string>();
        page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
            warning.TrySetResult(dialog.Type);
        };
        await page.CloseAsync(new() { RunBeforeUnload = true });
        Assert.That(await warning.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("beforeunload"));
        Assert.That(page.IsClosed, Is.False);
        await Assertions.Expect(page.Locator("#inspector-vertex-x")).ToHaveValueAsync("45");

        await Save(page);
        await page.ReloadAsync();
        await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("garden");
        await page.SelectOptionAsync("#room-select", "lounge");
        await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("lounge");
        await page.Locator("#editor-canvas").ClickAsync(new() { Position = new() { X = 45, Y = 240 } });
        await Assertions.Expect(page.Locator("#inspector-vertex-x")).ToHaveValueAsync("45");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DelayedSave_DoesNotCleanNewerChanges(bool switchRoom)
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        var sent = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        await page.RouteAsync("**/api/editor/rooms/lounge", async route =>
        {
            if (route.Request.Method != "POST") { await route.ContinueAsync(); return; }
            var response = await route.FetchAsync();
            sent.TrySetResult();
            await release.Task;
            await route.FulfillAsync(new() { Response = response });
        });
        try
        {
            await page.FillAsync("#input-name", "Gesendeter Entwurf");
            await page.ClickAsync("#btn-save");
            await sent.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Assertions.Expect(page.Locator("#btn-save")).ToBeDisabledAsync();
            if (switchRoom)
            {
                page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
                await page.SelectOptionAsync("#room-select", "garden");
                await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("garden");
            }
            await page.FillAsync("#input-name", "Neuere Bearbeitung");
            release.TrySetResult();
            await Assertions.Expect(page.Locator("#btn-save")).ToBeEnabledAsync();
            await Assertions.Expect(page.Locator("#input-name")).ToHaveValueAsync("Neuere Bearbeitung");
            await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeVisibleAsync(new() { Timeout = 2000 });
            var saved = await page.APIRequest.GetAsync($"{_server.BaseUrl}/api/editor/rooms/lounge");
            Assert.That(await saved.TextAsync(), Does.Contain("Gesendeter Entwurf").And.Not.Contain("Neuere Bearbeitung"));
            await page.UnrouteAsync("**/api/editor/rooms/lounge");
            await Save(page);
        }
        finally { release.TrySetResult(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReplacingDraft_RequiresDiscardConfirmation(bool newRoom)
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        await page.FillAsync("#input-name", "Ungespeicherter Entwurf");
        var accept = false;
        var dialogs = new List<string>();
        page.Dialog += async (_, dialog) =>
        {
            dialogs.Add(dialog.Type);
            if (accept) await dialog.AcceptAsync(); else await dialog.DismissAsync();
        };
        async Task ReplaceDraft()
        {
            if (newRoom) await page.ClickAsync("#btn-new-room");
            else await page.SelectOptionAsync("#room-select", "garden");
        }

        await ReplaceDraft();
        await Assertions.Expect(page.Locator("#input-name")).ToHaveValueAsync("Ungespeicherter Entwurf", new() { Timeout = 2000 });
        await Assertions.Expect(page.Locator("#room-select")).ToHaveValueAsync("lounge");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeVisibleAsync();
        Assert.That(dialogs, Is.EqualTo(new[] { "confirm" }));
        accept = true;
        await ReplaceDraft();
        if (newRoom) await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync(new System.Text.RegularExpressions.Regex("^room-"));
        else await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("garden");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
        Assert.That(dialogs, Is.EqualTo(new[] { "confirm", "confirm" }));
    }

    [Test]
    public async Task Selection_UnchangedInputs_AndValidation_KeepRoomClean()
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        var original = await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
        await page.Locator("#editor-canvas").ClickAsync(new() { Position = new() { X = 43, Y = 242 } });
        await Assertions.Expect(page.Locator("#inspector-vertex-x")).ToHaveValueAsync("40", new() { Timeout = 2000 });
        await page.FillAsync("#inspector-vertex-x", "40");
        var name = await page.InputValueAsync("#input-name");
        await page.FillAsync("#input-name", name);
        await page.ClickAsync("#btn-validate");
        await Assertions.Expect(page.Locator("#status-banner")).ToContainTextAsync("Validierung erfolgreich");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
        Assert.That(await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(original));
        var before = await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
        await page.ClickAsync("#btn-toggle-mode");
        await page.ClickAsync("#btn-toggle-mode");
        Assert.That(await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(before));
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedSave_KeepsDraftAndUnsavedStatus(bool networkFailure)
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        await page.Locator("#editor-canvas").ClickAsync(new() { Position = new() { X = 120, Y = 300 } });
        await page.FillAsync("#inspector-spawn-x", "5");
        await page.FillAsync("#inspector-spawn-y", "5");
        if (networkFailure) await page.RouteAsync("**/api/editor/rooms/lounge", route => route.AbortAsync());
        await page.ClickAsync("#btn-save");
        await Assertions.Expect(page.Locator("#status-banner")).ToContainTextAsync(networkFailure ? "Fehler beim Speichern" : "Validierungsfehler");
        await Assertions.Expect(page.Locator("#inspector-spawn-x")).ToHaveValueAsync("5");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeVisibleAsync();
        var warning = new TaskCompletionSource<string>();
        page.Dialog += async (_, dialog) => { await dialog.DismissAsync(); warning.TrySetResult(dialog.Type); };
        await page.CloseAsync(new() { RunBeforeUnload = true });
        Assert.That(await warning.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("beforeunload"));
        Assert.That(page.IsClosed, Is.False);
        var saved = await context.APIRequest.GetAsync($"{_server.BaseUrl}/api/editor/rooms/lounge");
        using var json = System.Text.Json.JsonDocument.Parse(await saved.TextAsync());
        Assert.That(json.RootElement.GetProperty("spawns")[0].GetProperty("position").GetProperty("x").GetInt32(), Is.EqualTo(120));
    }

    [TestCase("spawn", 120)]
    [TestCase("exit", 580)]
    public async Task InspectorRename_KeepsSelectionForFurtherEdits(string entity, int x)
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        await page.Locator("#editor-canvas").ClickAsync(new() { Position = new() { X = x, Y = 300 } });
        if (entity == "exit")
            await Assertions.Expect(page.Locator("#inspector-exit-target-spawn")).ToHaveValueAsync("from-lounge");
        await page.FillAsync($"#inspector-{entity}-id", "renamed");
        await page.FillAsync($"#inspector-{entity}-x", "125");
        await page.FillAsync($"#inspector-{entity}-y", "305");
        await Assertions.Expect(page.Locator($"#inspector-{entity}-id")).ToHaveValueAsync("renamed");
        await Assertions.Expect(page.Locator($"#inspector-{entity}-x")).ToHaveValueAsync("125");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeVisibleAsync();
        if (entity == "exit")
        {
            await page.FillAsync("#inspector-exit-radius", "999");
            await Assertions.Expect(page.Locator("#inspector-exit-radius")).ToHaveValueAsync("200");
            await page.FillAsync("#inspector-exit-radius", "1");
            await Assertions.Expect(page.Locator("#inspector-exit-radius")).ToHaveValueAsync("5");
            await Assertions.Expect(page.Locator("#inspector-exit-target-spawn")).ToHaveValueAsync("from-lounge");
        }
        await page.ClickAsync($"#btn-inspector-delete-{entity}");
        await Assertions.Expect(page.Locator("#inspector-empty")).ToBeVisibleAsync();
    }

    [Test]
    public async Task RoomSwitch_DrawsDelayedBackground_WithoutCanvasInteraction()
    {
        await using var context = await _browser.NewContextAsync();
        var page = await OpenLounge(context);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var image = await context.APIRequest.GetAsync($"{_server.BaseUrl}/assets/images/garden.png");
        Assert.That(image.Ok, Is.True);
        var expected = await page.EvaluateAsync<int[]>("""
            async png => {
                const image = new Image(); image.src = 'data:image/png;base64,' + png; await image.decode();
                const canvas = document.createElement('canvas'); canvas.width = 640; canvas.height = 360;
                const ctx = canvas.getContext('2d'); ctx.drawImage(image, 0, 0, 640, 360);
                return [...ctx.getImageData(13,17,1,1).data];
            }
            """, Convert.ToBase64String(await image.BodyAsync()));
        var fallback = new[] { 26, 32, 44, 255 };
        Assert.That(expected, Is.Not.EqualTo(fallback), "The sample must distinguish the room image from the loading background.");
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/assets/images/garden.png", async route =>
        {
            var response = await route.FetchAsync();
            requested.TrySetResult();
            await release.Task;
            await route.FulfillAsync(new() { Response = response });
        });
        try
        {
            await page.Mouse.MoveAsync(5, 5);
            await page.SelectOptionAsync("#room-select", "garden");
            await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("garden");
            await requested.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var canvas = page.Locator("#editor-canvas");
            Assert.That(await canvas.EvaluateAsync<int[]>("c => [...c.getContext('2d').getImageData(13,17,1,1).data]"), Is.EqualTo(fallback));
            release.TrySetResult();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Assert.That(await canvas.EvaluateAsync<int[]>("c => [...c.getContext('2d').getImageData(13,17,1,1).data]"), Is.EqualTo(expected));
            await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
            await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "editor-room-background.png") });
        }
        finally { release.TrySetResult(); }
    }

    private async Task<IPage> OpenLounge(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await Assertions.Expect(page.Locator("#room-select option")).ToHaveCountAsync(2);
        await page.SelectOptionAsync("#room-select", "lounge");
        await Assertions.Expect(page.Locator("#input-id")).ToHaveValueAsync("lounge");
        return page;
    }

    private static async Task Save(IPage page)
    {
        await page.ClickAsync("#btn-save");
        await Assertions.Expect(page.Locator("#status-banner")).ToContainTextAsync("atomar gespeichert");
        await Assertions.Expect(page.Locator("#unsaved-badge")).ToBeHiddenAsync();
    }
}

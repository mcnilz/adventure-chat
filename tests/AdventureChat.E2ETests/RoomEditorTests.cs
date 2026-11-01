using System.Net;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class RoomEditorTests
{
    private TestServerFixture _server = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private readonly List<string> _createdRoomFiles = new();

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
        foreach (var file in _createdRoomFiles)
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); } catch { /* ignore */ }
            }
        }

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
    public async Task EditorPage_LoadsAndDisplaysExistingRooms()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");

        var hasEditor = await page.EvaluateAsync<bool>("() => window.__ROOM_EDITOR__ !== undefined");
        Assert.That(hasEditor, Is.True, "window.__ROOM_EDITOR__ should be initialized.");

        // Wait for rooms to load in dropdown
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        var optionsCount = await page.Locator("#room-select option").CountAsync();
        Assert.That(optionsCount, Is.GreaterThanOrEqualTo(2));

        var optionsTexts = await page.Locator("#room-select option").AllInnerTextsAsync();
        Assert.That(optionsTexts.Any(t => t.Contains("lounge", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(optionsTexts.Any(t => t.Contains("garden", StringComparison.OrdinalIgnoreCase)), Is.True);

        // Select lounge explicitly to check form binding
        await page.SelectOptionAsync("#room-select", "lounge");
        var currentRoomId = await page.InputValueAsync("#input-id");
        Assert.That(currentRoomId, Is.EqualTo("lounge"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_CreateEditAndSaveNewRoom()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Click "Neu"
        await page.ClickAsync("#btn-new-room");

        // Fill form fields
        var customId = $"e2e-room-{Guid.NewGuid():N}";
        await page.FillAsync("#input-id", customId);
        await page.FillAsync("#input-name", "E2E Created Tavern Room");
        await page.FillAsync("#input-bg", "assets/images/lounge.png");
        await page.FillAsync("#input-notes", "Authored by Playwright E2E test.");

        // Click "Speichern"
        await page.ClickAsync("#btn-save");

        // Wait specifically for the save confirmation message in the status banner
        await page.WaitForFunctionAsync("() => document.getElementById('status-banner')?.textContent?.includes('atomar gespeichert') === true", null, new PageWaitForFunctionOptions { Timeout = 10000 });

        var bannerText = await page.InnerTextAsync("#status-banner");
        Assert.That(bannerText, Does.Contain("erfolgreich atomar gespeichert").IgnoreCase);

        // Verify that the new room is now available on regular runtime API
        using var httpClient = new HttpClient();
        var apiResponse = await httpClient.GetAsync($"{_server.BaseUrl}/api/rooms/{customId}");
        Assert.That(apiResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var roomContent = await apiResponse.Content.ReadAsStringAsync();
        Assert.That(roomContent, Does.Contain("E2E Created Tavern Room"));
        Assert.That(roomContent, Does.Contain("Authored by Playwright E2E test."));

        // Register for cleanup
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var contentDir = Path.Combine(current, "content", "rooms", $"{customId}.yaml");
            if (File.Exists(contentDir))
            {
                _createdRoomFiles.Add(contentDir);
                break;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_ValidationRejectsInvalidGeometry()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Click "Neu"
        await page.ClickAsync("#btn-new-room");

        // Set invalid spawn point (outside floor polygon)
        await page.EvaluateAsync("() => window.__ROOM_EDITOR__.getState().addOrUpdateSpawn('invalid-spawn', 5, 5)");

        // Click "Prüfen"
        await page.ClickAsync("#btn-validate");

        // Verify error list is shown
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#error-list li').length > 0");
        var errorText = await page.InnerTextAsync("#error-list");
        Assert.That(errorText, Does.Contain("not located on a walkable area").IgnoreCase);

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_PreviewMode_AllowsWalking()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Switch to preview mode
        await page.ClickAsync("#btn-toggle-mode");

        var mode = await page.EvaluateAsync<string>("() => window.__ROOM_EDITOR__.getMode()");
        Assert.That(mode, Is.EqualTo("preview"));

        // Click on walkable canvas area: logical (200, 300)
        // Canvas is 640x360 rendered at 640x360
        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);

        await page.Mouse.ClickAsync(canvasBounding!.X + 200, canvasBounding.Y + 300);

        // Wait a brief moment for movement
        await page.WaitForTimeoutAsync(500);

        // Switch back to edit mode
        await page.ClickAsync("#btn-toggle-mode");
        var modeAfter = await page.EvaluateAsync<string>("() => window.__ROOM_EDITOR__.getMode()");
        Assert.That(modeAfter, Is.EqualTo("edit"));

        await context.DisposeAsync();
    }
}

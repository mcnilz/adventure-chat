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
        _server = new TestServerFixture(authoring: true);
        await _server.StartAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var file in _createdRoomFiles.ToArray())
        {
            if (File.Exists(file))
            {
                try { File.Delete(file); _createdRoomFiles.Remove(file); } catch { /* ignore */ }
            }
        }
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

    private void RegisterForCleanup(string roomId)
    {
        var file = Path.Combine(_server.TempRoot, "content", "rooms", $"{roomId}.yaml");
        if (File.Exists(file)) _createdRoomFiles.Add(file);
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
        await page.WaitForFunctionAsync("() => document.getElementById('input-id')?.value === 'lounge'");
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
        RegisterForCleanup(customId);

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

    [Test]
    public async Task Editor_Inspector_ShowsAndEditsActiveElement()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Select lounge explicitly
        await page.SelectOptionAsync("#room-select", "lounge");
        await page.WaitForFunctionAsync("() => document.getElementById('input-id')?.value === 'lounge'");

        // Initially no element selected -> inspector shows empty hint
        var isEmptyVisible = await page.IsVisibleAsync("#inspector-empty");
        Assert.That(isEmptyVisible, Is.True);

        // Click near the first walkable vertex of lounge: (40, 240)
        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);

        await page.Mouse.ClickAsync(canvasBounding!.X + 40, canvasBounding.Y + 240);

        // Vertex inspector should now be visible
        await page.WaitForSelectorAsync("#inspector-vertex", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var badgeText = await page.InnerTextAsync("#inspector-vertex-badge");
        Assert.That(badgeText, Does.Contain("Bodenpunkt #1"));

        var initialX = await page.InputValueAsync("#inspector-vertex-x");
        Assert.That(initialX, Is.EqualTo("40"));

        // Edit coordinate in inspector
        await page.FillAsync("#inspector-vertex-x", "45");
        var stateX = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().walkable[0].points[0].x");
        Assert.That(stateX, Is.EqualTo(45));

        // Click on spawn point near (120, 300) in lounge
        await page.Mouse.ClickAsync(canvasBounding.X + 120, canvasBounding.Y + 300);
        await page.WaitForSelectorAsync("#inspector-spawn", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var spawnIdVal = await page.InputValueAsync("#inspector-spawn-id");
        Assert.That(spawnIdVal, Is.EqualTo("entry"));

        // Delete spawn via inspector button
        await page.ClickAsync("#btn-inspector-delete-spawn");
        var spawnsCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().spawns.length");
        Assert.That(spawnsCount, Is.EqualTo(1)); // Was 2 in lounge (entry, from-garden)

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_EdgeClick_SplitsEdgeAndInsertsVertex()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Select lounge explicitly
        await page.SelectOptionAsync("#room-select", "lounge");
        await page.WaitForFunctionAsync("() => document.getElementById('input-id')?.value === 'lounge'");

        var initialVertexCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().walkable[0].points.length");

        // In lounge, bottom edge goes from (600, 345) to (40, 345). Midpoint is (320, 345).
        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);

        // Hover over the edge at (320, 345)
        await page.Mouse.MoveAsync(canvasBounding!.X + 320, canvasBounding.Y + 345);
        await page.WaitForTimeoutAsync(100);

        // Click to split edge and insert point
        await page.Mouse.ClickAsync(canvasBounding.X + 320, canvasBounding.Y + 345);

        var newVertexCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().walkable[0].points.length");
        Assert.That(newVertexCount, Is.EqualTo(initialVertexCount + 1), "A new vertex should be inserted on the clicked edge.");

        // Inspector should show the newly created vertex
        await page.WaitForSelectorAsync("#inspector-vertex", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var xVal = await page.InputValueAsync("#inspector-vertex-x");
        Assert.That(int.Parse(xVal), Is.InRange(315, 325));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_BackgroundPicker_OpensModalAndSelectsImage()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Verify folder hint is visible on main editor page
        var hintPath = await page.InnerTextAsync("#bg-fs-hint-path");
        Assert.That(hintPath, Does.Contain("assets/images").Or.Contain("assets\\images"));

        // Open background modal
        await page.ClickAsync("#btn-open-bg-modal");
        await page.WaitForSelectorAsync(".bg-gallery-card", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        // Verify filesystem path in modal
        var modalFsPath = await page.InnerTextAsync("#modal-fs-path");
        Assert.That(modalFsPath, Does.Contain("assets/images").Or.Contain("assets\\images"));

        // Check that test backgrounds are visible in gallery
        var galleryCards = await page.Locator(".bg-gallery-card").CountAsync();
        Assert.That(galleryCards, Is.GreaterThanOrEqualTo(5)); // lounge, garden, library, courtyard, cafe

        // Verify thumbnail scaling properties
        var firstImgFit = await page.EvaluateAsync<string>(
            "() => window.getComputedStyle(document.querySelector('.bg-gallery-card .bg-thumbnail-img')).objectFit");
        Assert.That(firstImgFit, Is.EqualTo("contain"));

        var imgWidth = await page.EvaluateAsync<double>(
            "() => document.querySelector('.bg-gallery-card .bg-thumbnail-img').getBoundingClientRect().width");
        var cardWidth = await page.EvaluateAsync<double>(
            "() => document.querySelector('.bg-gallery-card').getBoundingClientRect().width");
        Assert.That(imgWidth, Is.LessThanOrEqualTo(cardWidth + 1), "Thumbnail image should be scaled down to fit card width.");

        // Click on library background card
        var libraryCard = page.Locator(".bg-gallery-card[data-path='assets/images/library.png']");
        await libraryCard.ClickAsync();

        // Verify background is updated in input and state
        var bgInput = await page.InputValueAsync("#input-bg");
        Assert.That(bgInput, Is.EqualTo("assets/images/library.png"));

        var stateBg = await page.EvaluateAsync<string>("() => window.__ROOM_EDITOR__.getState().getRoom().background");
        Assert.That(stateBg, Is.EqualTo("assets/images/library.png"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_AddObstacle_CreatesMultiPointAreaAndCanBeMoved()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Click Add Obstacle Tool
        await page.ClickAsync("#tool-obstacle");

        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);

        // Click on canvas at (300, 280) to place a 4-point obstacle area
        await page.Mouse.ClickAsync(canvasBounding!.X + 300, canvasBounding.Y + 280);

        // Verify obstacle created with 4 vertices
        var obsCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().obstacles?.length ?? 0");
        Assert.That(obsCount, Is.GreaterThanOrEqualTo(1));

        var ptsCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().obstacles[0].points.length");
        Assert.That(ptsCount, Is.EqualTo(4));

        var initialPt0X = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().obstacles[0].points[0].x");
        Assert.That(initialPt0X, Is.InRange(250, 280));

        // Inspector should show the whole polygon selected
        await page.WaitForSelectorAsync("#inspector-polygon", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var pointsCountVal = await page.InputValueAsync("#inspector-polygon-points-count");
        Assert.That(pointsCountVal, Does.Contain("4 Eckpunkte"));

        // Drag the obstacle from (300, 280) to (340, 300)
        await page.Mouse.MoveAsync(canvasBounding.X + 300, canvasBounding.Y + 280);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(canvasBounding.X + 340, canvasBounding.Y + 300);
        await page.Mouse.UpAsync();

        // Verify first vertex moved to the right (greater than initial)
        var newPt0X = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().obstacles[0].points[0].x");
        Assert.That(newPt0X, Is.GreaterThan(initialPt0X));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_ValidationErrors_OnlyShownWhenErrorsExist()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // 1. Errors section should be hidden initially
        var isSectionHiddenInitially = await page.EvaluateAsync<bool>("() => document.getElementById('errors-section')?.style.display === 'none'");
        Assert.That(isSectionHiddenInitially, Is.True, "Validation errors section should be hidden initially.");

        // 2. Validate valid room -> section remains hidden
        await page.ClickAsync("#btn-validate");
        await page.WaitForTimeoutAsync(300);
        var isSectionStillHidden = await page.EvaluateAsync<bool>("() => document.getElementById('errors-section')?.style.display === 'none'");
        Assert.That(isSectionStillHidden, Is.True, "Validation errors section should remain hidden when room is valid.");

        // 3. Inject invalid geometry -> section becomes visible
        await page.EvaluateAsync("() => window.__ROOM_EDITOR__.getState().addOrUpdateSpawn('invalid-spawn', 5, 5)");
        await page.ClickAsync("#btn-validate");

        await page.WaitForSelectorAsync("#errors-section", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var isSectionVisible = await page.EvaluateAsync<bool>("() => document.getElementById('errors-section')?.style.display === 'block'");
        Assert.That(isSectionVisible, Is.True, "Validation errors section should be visible when errors exist.");

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_ExitCreation_UsesModalDialogWithoutPrompt()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // Click Add Exit Tool
        await page.ClickAsync("#tool-exit");

        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);

        // Click on canvas to place an exit
        await page.Mouse.ClickAsync(canvasBounding!.X + 250, canvasBounding.Y + 280);

        // Verify Exit Modal dialog is opened
        await page.WaitForSelectorAsync("#modal-exit-dialog[open]");

        // Verify target room and spawn options are available in dropdowns
        var roomOptionsCount = await page.Locator("#modal-exit-room option").CountAsync();
        Assert.That(roomOptionsCount, Is.GreaterThanOrEqualTo(1));

        var spawnOptionsCount = await page.Locator("#modal-exit-target-spawn option").CountAsync();
        Assert.That(spawnOptionsCount, Is.GreaterThanOrEqualTo(1));

        // Confirm exit creation dialog
        await page.ClickAsync("#btn-confirm-exit-modal");

        // Verify exit is added to state
        var exitsCount = await page.EvaluateAsync<int>("() => window.__ROOM_EDITOR__.getState().getRoom().exits?.length ?? 0");
        Assert.That(exitsCount, Is.GreaterThanOrEqualTo(1));

        // Inspector should show Exit details
        await page.WaitForSelectorAsync("#inspector-exit", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
        var inspectorRoomVal = await page.InputValueAsync("#inspector-exit-room");
        Assert.That(inspectorRoomVal, Is.Not.Empty);

        await context.DisposeAsync();
    }

    [Test]
    public async Task Editor_NewlyCreatedRoom_AppearsInExitDialogTargetRoomListWhenSwitchingRooms()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('#room-select option').length >= 2");

        // 1. Create a new room
        await page.ClickAsync("#btn-new-room");

        var newRoomId = $"e2e-target-{Guid.NewGuid():N}";
        await page.FillAsync("#input-id", newRoomId);
        await page.FillAsync("#input-name", "Zielraum E2E");
        await page.FillAsync("#input-bg", "assets/images/lounge.png");

        // Add a spawn point inside walkable area
        await page.ClickAsync("#tool-spawn");
        var canvasBounding = await page.Locator("#editor-canvas").BoundingBoxAsync();
        Assert.That(canvasBounding, Is.Not.Null);
        await page.Mouse.ClickAsync(canvasBounding!.X + 300, canvasBounding.Y + 280);

        // Save room
        await page.ClickAsync("#btn-save");
        await page.WaitForFunctionAsync("() => document.getElementById('unsaved-badge')?.style.display === 'none'");

        RegisterForCleanup(newRoomId);

        // 2. Switch back to lounge
        await page.SelectOptionAsync("#room-select", "lounge");
        await page.WaitForFunctionAsync("() => document.getElementById('input-id')?.value === 'lounge'");

        // 3. Try to add an exit in lounge
        await page.ClickAsync("#tool-exit");
        await page.Mouse.ClickAsync(canvasBounding.X + 220, canvasBounding.Y + 280);

        // Verify Exit dialog opens
        await page.WaitForSelectorAsync("#modal-exit-dialog[open]");

        // Verify the newly created room is in the target room select options
        var roomOptions = await page.Locator("#modal-exit-room option").AllInnerTextsAsync();
        Assert.That(roomOptions.Any(opt => opt.Contains(newRoomId, StringComparison.OrdinalIgnoreCase)), Is.True,
            $"Expected newly saved room '{newRoomId}' to be available in exit target room options, but found: {string.Join(", ", roomOptions)}");

        // Select the new room and verify its spawn point is populated
        await page.SelectOptionAsync("#modal-exit-room", newRoomId);
        await page.WaitForTimeoutAsync(200);

        var spawnOptions = await page.Locator("#modal-exit-target-spawn option").AllInnerTextsAsync();
        Assert.That(spawnOptions.Count, Is.GreaterThanOrEqualTo(1), "Target spawn dropdown should contain spawns of the selected new room.");

        // Close modal
        await page.ClickAsync("#btn-cancel-exit-modal");

        await context.DisposeAsync();
    }
}

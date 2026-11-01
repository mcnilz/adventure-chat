using System.Net.Http.Json;
using AdventureChat.Server.Models;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class WorldAuthoringTests
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
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var contentDir = Path.Combine(current, "content", "rooms", $"{roomId}.yaml");
            if (File.Exists(contentDir) && !_createdRoomFiles.Contains(contentDir))
            {
                _createdRoomFiles.Add(contentDir);
            }
            var serverContentDir = Path.Combine(current, "src", "AdventureChat.Server", "content", "rooms", $"{roomId}.yaml");
            if (File.Exists(serverContentDir) && !_createdRoomFiles.Contains(serverContentDir))
            {
                _createdRoomFiles.Add(serverContentDir);
            }
            current = Directory.GetParent(current)?.FullName;
        }
    }

    [Test]
    public async Task AuthorConnectedRooms_AndWalkBetweenThemInMultiplayer()
    {
        var roomIdA = "custom-hub-" + Guid.NewGuid().ToString("N")[..6];
        var roomIdB = "custom-sanctum-" + Guid.NewGuid().ToString("N")[..6];

        // 1. Define two interconnected custom rooms
        var roomA = new RoomYamlDto
        {
            Version = 1,
            Id = roomIdA,
            Name = "Autoren-Hub",
            Background = "assets/images/lounge.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto
                {
                    Id = "entry",
                    Position = new RoomPointYamlDto { X = 120, Y = 280 }
                },
                new RoomSpawnYamlDto
                {
                    Id = "from-sanctum",
                    Position = new RoomPointYamlDto { X = 460, Y = 280 }
                }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 345 },
                        new RoomPointYamlDto { X = 40, Y = 345 }
                    ]
                }
            ],
            Exits = [
                new RoomExitYamlDto
                {
                    Id = "to-sanctum",
                    Label = "Zum Heiligtum",
                    Position = new RoomPointYamlDto { X = 520, Y = 280 },
                    Radius = 35,
                    Target = new RoomExitTargetYamlDto { Room = roomIdB, Spawn = "from-hub" }
                }
            ]
        };

        var roomB = new RoomYamlDto
        {
            Version = 1,
            Id = roomIdB,
            Name = "Autoren-Heiligtum",
            Background = "assets/images/garden.png",
            Size = new RoomSizeYamlDto { Width = 640, Height = 360 },
            Spawns = [
                new RoomSpawnYamlDto
                {
                    Id = "from-hub",
                    Position = new RoomPointYamlDto { X = 120, Y = 280 }
                },
                new RoomSpawnYamlDto
                {
                    Id = "entry",
                    Position = new RoomPointYamlDto { X = 200, Y = 280 }
                }
            ],
            Walkable = [
                new RoomPolygonYamlDto
                {
                    Id = "floor",
                    Points = [
                        new RoomPointYamlDto { X = 40, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 240 },
                        new RoomPointYamlDto { X = 600, Y = 345 },
                        new RoomPointYamlDto { X = 40, Y = 345 }
                    ]
                }
            ],
            Exits = [
                new RoomExitYamlDto
                {
                    Id = "to-hub",
                    Label = "Zurück zum Hub",
                    Position = new RoomPointYamlDto { X = 520, Y = 280 },
                    Radius = 35,
                    Target = new RoomExitTargetYamlDto { Room = roomIdA, Spawn = "from-sanctum" }
                }
            ]
        };

        // 2. Save both rooms via Editor API
        using var httpClient = new HttpClient { BaseAddress = new Uri(_server.BaseUrl) };
        var respA = await httpClient.PostAsJsonAsync($"/api/editor/rooms/{roomIdA}", roomA);
        Assert.That(respA.IsSuccessStatusCode, Is.True, await respA.Content.ReadAsStringAsync());
        RegisterForCleanup(roomIdA);

        var respB = await httpClient.PostAsJsonAsync($"/api/editor/rooms/{roomIdB}", roomB);
        Assert.That(respB.IsSuccessStatusCode, Is.True, await respB.Content.ReadAsStringAsync());
        RegisterForCleanup(roomIdB);

        // 3. Launch Player 1 (Alice) into Room A
        var context1 = await _browser.NewContextAsync();
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync($"{_server.BaseUrl}?name=Alice&room={roomIdA}");
        await page1.WaitForSelectorAsync("#game-canvas");
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        var roomTitle1 = await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.name");
        Assert.That(roomTitle1, Does.Contain("Autoren-Hub"));

        // 4. Launch Player 2 (Bob) into Room A
        var context2 = await _browser.NewContextAsync();
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync($"{_server.BaseUrl}?name=Bob&room={roomIdA}");
        await page2.WaitForSelectorAsync("#game-canvas");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.connectionStatus === 'connected'");

        // Verify Bob joined and both see each other
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Alice chats
        await page1.FillAsync("#chat-input", "Hallo Bob im neu erstellten Raum!");
        await page1.ClickAsync("#chat-send");

        // Bob receives Alice's chat
        await page2.WaitForSelectorAsync("#chat-messages .chat-message");
        var p2Msg = await page2.InnerTextAsync("#chat-messages .chat-message:last-child");
        Assert.That(p2Msg, Does.Contain("Alice:"));
        Assert.That(p2Msg, Does.Contain("Hallo Bob im neu erstellten Raum!"));

        // 5. Alice clicks exit area to transition to Room B
        var canvas1 = page1.Locator("#game-canvas");
        var box1 = await canvas1.BoundingBoxAsync();
        Assert.That(box1, Is.Not.Null);

        // Click at exit position (520, 280) scaled to bounding box
        var clickX = (float)(box1!.X + (520.0 / 640.0) * box1.Width);
        var clickY = (float)(box1.Y + (280.0 / 360.0) * box1.Height);
        await page1.Mouse.ClickAsync(clickX, clickY);

        // Wait for Alice to transition to Room B
        await page1.WaitForFunctionAsync($"() => window.__ADVENTURE_GAME__.room.id === '{roomIdB}'", new PageWaitForFunctionOptions
        {
            Timeout = 10000
        });
        var newTitle1 = await page1.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.name");
        Assert.That(newTitle1, Does.Contain("Autoren-Heiligtum"));

        // Bob sees Alice leave Room A
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 0");

        // 6. Bob now also walks to exit area and transitions to Room B
        var canvas2 = page2.Locator("#game-canvas");
        var box2 = await canvas2.BoundingBoxAsync();
        Assert.That(box2, Is.Not.Null);

        var clickX2 = (float)(box2!.X + (520.0 / 640.0) * box2.Width);
        var clickY2 = (float)(box2.Y + (280.0 / 360.0) * box2.Height);
        await page2.Mouse.ClickAsync(clickX2, clickY2);

        // Bob arrives in Room B
        await page2.WaitForFunctionAsync($"() => window.__ADVENTURE_GAME__.room.id === '{roomIdB}'", new PageWaitForFunctionOptions
        {
            Timeout = 10000
        });
        var newTitle2 = await page2.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.room.name");
        Assert.That(newTitle2, Does.Contain("Autoren-Heiligtum"));

        // Alice sees Bob enter Room B and Bob sees Alice
        await page1.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");
        await page2.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.remotePlayers.size === 1");

        // Bob sends a message in Room B
        await page2.FillAsync("#chat-input", "Ich bin jetzt auch im Heiligtum!");
        await page2.ClickAsync("#chat-send");

        await page1.WaitForSelectorAsync("#chat-messages .chat-message:last-child");
        var p1ReceivedMsg = await page1.InnerTextAsync("#chat-messages .chat-message:last-child");
        Assert.That(p1ReceivedMsg, Does.Contain("Bob:"));
        Assert.That(p1ReceivedMsg, Does.Contain("Ich bin jetzt auch im Heiligtum!"));

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    [Test]
    public async Task Editor_ValidateWorldButton_ChecksConsistencyAndDisplaysSuccess()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.BaseUrl}/editor.html");
        await page.WaitForSelectorAsync("#editor-canvas");

        // Click Welt prüfen button
        await page.ClickAsync("#btn-validate-world");

        // Verify status banner displays success for default valid world
        await page.WaitForFunctionAsync("() => document.getElementById('status-banner')?.textContent.includes('Weltweit gültig')");
        var bannerText = await page.InnerTextAsync("#status-banner");
        Assert.That(bannerText, Does.Contain("Weltweit gültig"));

        await context.DisposeAsync();
    }
}

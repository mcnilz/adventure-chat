using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class LocalRoomMovementTests
{
    private TestServerFixture _server = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

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
    public async Task PageLoads_AndInitializesCharacterAtSpawn()
    {
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        var hasGame = await page.EvaluateAsync<bool>("() => window.__ADVENTURE_GAME__ !== undefined");
        Assert.That(hasGame, Is.True, "window.__ADVENTURE_GAME__ should be initialized.");

        var posX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var posY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");
        var state = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.state");

        Assert.That(posX, Is.EqualTo(120).Within(1.0));
        Assert.That(posY, Is.EqualTo(300).Within(1.0));
        Assert.That(state, Is.EqualTo("idle"));

        await context.DisposeAsync();
    }

    [Test]
    public async Task ClickOnWalkableFloor_MovesCharacter_AndStopsAtTarget()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        // Target: logical point (400, 300)
        await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 400, y: 300 })");

        var stateWhileWalking = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.state");
        var facing = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");

        Assert.That(stateWhileWalking, Is.EqualTo("walk"));
        Assert.That(facing, Is.EqualTo("right"));

        // Wait for movement to finish (speed is 130px/s, distance is 280px => ~2.2s)
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000
        });

        var finalX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var finalY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        Assert.That(finalX, Is.EqualTo(400).Within(1.0));
        Assert.That(finalY, Is.EqualTo(300).Within(1.0));

        await context.DisposeAsync();
    }

    [Test]
    public async Task NewClickWhileMoving_ReplacesTarget_AndReorientsCharacter()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        // First click: move right to (500, 300)
        await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 500, y: 300 })");
        await page.WaitForTimeoutAsync(500); // Walk a bit

        var intermediateX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        Assert.That(intermediateX, Is.GreaterThan(120));

        // Second click: change mind, move left to (100, 300)
        await page.EvaluateAsync("async () => await window.__ADVENTURE_GAME__.setTarget({ x: 100, y: 300 })");

        var facing = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");
        Assert.That(facing, Is.EqualTo("left"));

        // Wait until target reached
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000
        });

        var finalX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var finalY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        Assert.That(finalX, Is.EqualTo(100).Within(1.0));
        Assert.That(finalY, Is.EqualTo(300).Within(1.0));

        await context.DisposeAsync();
    }

    [Test]
    public async Task ClickOutsideWalkableFloor_AndOnChatContainer_DoesNotMoveCharacter()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        var startX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var startY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        // 1. Click on non-walkable wall area (top of canvas)
        var canvas = await page.QuerySelectorAsync("#game-canvas");
        Assert.That(canvas, Is.Not.Null);

        var box = await canvas!.BoundingBoxAsync();
        Assert.That(box, Is.Not.Null);

        // Click top-left wall area (logical y around 50)
        await page.Mouse.ClickAsync(box!.X + 100, box.Y + 30);
        await page.WaitForTimeoutAsync(200);

        var posXAfterWallClick = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var targetAfterWallClick = await page.EvaluateAsync<object?>("() => window.__ADVENTURE_GAME__.character.target");

        Assert.That(posXAfterWallClick, Is.EqualTo(startX).Within(0.1));
        Assert.That(targetAfterWallClick, Is.Null);

        // 2. Click on chat container overlay
        await page.ClickAsync("#chat-container");
        await page.WaitForTimeoutAsync(200);

        var posXAfterChatClick = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var targetAfterChatClick = await page.EvaluateAsync<object?>("() => window.__ADVENTURE_GAME__.character.target");

        Assert.That(posXAfterChatClick, Is.EqualTo(startX).Within(0.1));
        Assert.That(targetAfterChatClick, Is.Null);

        await context.DisposeAsync();
    }

    [Test]
    public async Task WindowScaling_PreservesLogicalCoordinates()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 800, Height = 450 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        // Check screen to logical mapping at 800x450
        var logicalMidPointSmall = await page.EvaluateAsync<double[]>(@"() => {
            const canvas = document.getElementById('game-canvas');
            const rect = canvas.getBoundingClientRect();
            const p = window.__ADVENTURE_GAME__.screenToLogical(rect.left + rect.width / 2, rect.top + rect.height / 2);
            return [p.x, p.y];
        }");

        Assert.That(logicalMidPointSmall[0], Is.EqualTo(320).Within(1.0));
        Assert.That(logicalMidPointSmall[1], Is.EqualTo(180).Within(1.0));

        // Resize window to 1600x900
        await page.SetViewportSizeAsync(1600, 900);

        var logicalMidPointLarge = await page.EvaluateAsync<double[]>(@"() => {
            const canvas = document.getElementById('game-canvas');
            const rect = canvas.getBoundingClientRect();
            const p = window.__ADVENTURE_GAME__.screenToLogical(rect.left + rect.width / 2, rect.top + rect.height / 2);
            return [p.x, p.y];
        }");

        Assert.That(logicalMidPointLarge[0], Is.EqualTo(320).Within(1.0));
        Assert.That(logicalMidPointLarge[1], Is.EqualTo(180).Within(1.0));

        await context.DisposeAsync();
    }

    [Test]
    public async Task MovingUpAndDown_ChangesFacingToBackAndFrontView()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        // 1. Walk straight up towards back wall (from spawn at 120, 300 to 120, 250)
        await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 120, y: 250 })");

        var facingUp = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");
        var stateUp = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.state");

        Assert.That(facingUp, Is.EqualTo("up"));
        Assert.That(stateUp, Is.EqualTo("walk"));

        // Wait until reached back of the room
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000
        });

        var finalFacingAfterUp = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");
        var finalYAfterUp = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        Assert.That(finalFacingAfterUp, Is.EqualTo("up"), "Character should remain facing up (back view / Rücken) when standing at back wall.");
        Assert.That(finalYAfterUp, Is.EqualTo(250).Within(1.0));

        // 2. Walk straight down towards front (from 120, 250 to 120, 330)
        await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 120, y: 330 })");

        var facingDown = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");
        Assert.That(facingDown, Is.EqualTo("down"));

        // Wait until reached front
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.state === 'idle'", null, new PageWaitForFunctionOptions
        {
            Timeout = 5000
        });

        var finalFacingAfterDown = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.facing");
        var finalYAfterDown = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        Assert.That(finalFacingAfterDown, Is.EqualTo("down"));
        Assert.That(finalYAfterDown, Is.EqualTo(330).Within(1.0));

        await context.DisposeAsync();
    }

    [Test]
    public async Task Pathfinding_NavigatesAroundObstacle_AndNeverEntersObstacle()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        // Target: behind obstacle at (500, 300)
        await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({ x: 500, y: 300 })");

        // While moving, collect sampled positions
        var sampledPoints = new List<(double X, double Y)>();
        for (int i = 0; i < 40; i++)
        {
            var isIdle = await page.EvaluateAsync<bool>("() => window.__ADVENTURE_GAME__.character.state === 'idle'");
            var x = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
            var y = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");
            sampledPoints.Add((x, y));

            if (isIdle) break;
            await page.WaitForTimeoutAsync(100);
        }

        // Final position check
        var finalX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var finalY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        Assert.That(finalX, Is.EqualTo(500).Within(1.0));
        Assert.That(finalY, Is.EqualTo(300).Within(1.0));

        // Obstacle rectangle is [250..390, 255..310]
        // Verify that none of the sampled points ever fell strictly inside the obstacle!
        foreach (var (x, y) in sampledPoints)
        {
            var isInsideObstacle = x > 252 && x < 388 && y > 257 && y < 308;
            Assert.That(isInsideObstacle, Is.False, $"Footpoint ({x:F1}, {y:F1}) penetrated obstacle interior!");
        }

        await context.DisposeAsync();
    }

    [Test]
    public async Task ClickInsideObstacle_IsRejected_AndCharacterDoesNotMove()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForSelectorAsync("#game-canvas");

        var startX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var startY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");

        // Target: center of tavern table (320, 280)
        var result = await page.EvaluateAsync<bool>("""
            async () => {
                const res = await window.__ADVENTURE_GAME__.setTarget({ x: 320, y: 280 });
                return res.accepted;
            }
        """);

        Assert.That(result, Is.False, "Click inside obstacle should be rejected by server.");

        var posX = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x");
        var posY = await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.y");
        var state = await page.EvaluateAsync<string>("() => window.__ADVENTURE_GAME__.character.state");
        var target = await page.EvaluateAsync<object?>("() => window.__ADVENTURE_GAME__.character.target");

        Assert.That(posX, Is.EqualTo(startX).Within(0.1));
        Assert.That(posY, Is.EqualTo(startY).Within(0.1));
        Assert.That(state, Is.EqualTo("idle"));
        Assert.That(target, Is.Null);

        await context.DisposeAsync();
    }
}

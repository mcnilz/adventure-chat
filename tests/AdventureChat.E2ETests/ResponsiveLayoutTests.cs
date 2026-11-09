using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

[TestFixture]
public class ResponsiveLayoutTests
{
    private TestServerFixture _server = null!;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    [OneTimeSetUp]
    public async Task Start()
    {
        _server = new TestServerFixture();
        await _server.StartAsync();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    [OneTimeTearDown]
    public async Task Stop()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _server.DisposeAsync();
    }

    [TestCase(1280, 480)]
    [TestCase(640, 360)]
    [TestCase(800, 900)]
    [TestCase(1920, 1080)]
    public async Task EntireGame_FitsInWindow(int width, int height)
    {
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await AssertFitsWindow(page);
    }

    [Test]
    public async Task Resize_ScalesCanvasChatAndControlsTogether_AndPreservesPointerInput()
    {
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var normal = await Sizes(page);
        Assert.That(normal[0], Is.EqualTo(960).Within(0.1));
        foreach (var size in new[] { (1280, 480), (640, 360), (800, 900), (1920, 1080) })
        {
            await page.SetViewportSizeAsync(size.Item1, size.Item2);
            await NextLayout(page);
            await AssertFitsWindow(page);
            var scaled = await Sizes(page);
            var ratio = scaled[0] / normal[0];
            for (var i = 1; i < normal.Length; i++)
                Assert.That(scaled[i], Is.EqualTo(normal[i] * ratio).Within(0.1), $"Dimension {i} scales uniformly");
            Assert.That(scaled[0], Is.LessThanOrEqualTo(960.1));

            var canvas = (await page.Locator("#game-canvas").BoundingBoxAsync())!;
            // Alternate targets so every actual pointer click must produce movement.
            var x = size.Item1 == 640 || size.Item1 == 1920 ? 160 : 180;
            await page.Mouse.ClickAsync(canvas.X + canvas.Width * x / 640, canvas.Y + canvas.Height * 320 / 360);
            // A browser mouse event rounds CSS pixels; at this scale one CSS pixel spans up to 1.5 room pixels.
            await page.WaitForFunctionAsync("x => { const c = window.__ADVENTURE_GAME__.character; return Math.abs(c.position.x - x) < 2 && Math.abs(c.position.y - 320) < 2 && c.state === 'idle'; }", x, new() { Timeout = 5000 });
            var message = $"Skaliert {size.Item1}x{size.Item2}";
            await page.FillAsync("#chat-input", message);
            await page.ClickAsync("#chat-send");
            await Assertions.Expect(page.Locator(".chat-message").Last).ToContainTextAsync(message);
            Assert.That(await page.EvaluateAsync<double>("() => window.__ADVENTURE_GAME__.character.position.x"), Is.EqualTo(x).Within(2));
            if (size.Item1 == 640)
                await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "scaling-small.png") });
        }
        await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "scaling-normal.png") });
    }

    [Test]
    public async Task ExpandButton_FillsWindow_ResizesAndReturnsToNormal_ByKeyboard()
    {
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.BaseUrl);
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var button = page.Locator("#game-scale");
        await Assertions.Expect(button).ToHaveAccessibleNameAsync("Auf Fenstergröße");
        await button.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(button).ToHaveTextAsync("Normalgröße");
        foreach (var size in new[] { (1920, 1080), (1280, 480), (640, 360), (800, 900), (1920, 1080) })
        {
            await page.SetViewportSizeAsync(size.Item1, size.Item2);
            await NextLayout(page);
            await AssertFitsWindow(page);
            var box = (await page.Locator("#game-container").BoundingBoxAsync())!;
            Assert.That(Math.Min(size.Item1 - 32 - box.Width, size.Item2 - 32 - box.Height), Is.EqualTo(0).Within(0.2));
            Assert.That(box.X + box.Width / 2, Is.EqualTo(size.Item1 / 2.0).Within(0.2));
            Assert.That(box.Y + box.Height / 2, Is.EqualTo(size.Item2 / 2.0).Within(0.2));
        }
        Assert.That((await Sizes(page))[0], Is.GreaterThan(960));
        var canvas = (await page.Locator("#game-canvas").BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(canvas.X + canvas.Width * 180 / 640, canvas.Y + canvas.Height * 320 / 360);
        await page.WaitForFunctionAsync("() => { const c = window.__ADVENTURE_GAME__.character; return Math.abs(c.position.x - 180) < 2 && c.state === 'idle'; }");
        await page.FillAsync("#chat-input", "Vergrößerter Chat");
        await page.ClickAsync("#chat-send");
        await Assertions.Expect(page.Locator(".chat-message").Last).ToContainTextAsync("Vergrößerter Chat");
        await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "scaling-expanded.png") });
        await button.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "false");
        await Assertions.Expect(button).ToHaveTextAsync("Auf Fenstergröße");
        Assert.That((await Sizes(page))[0], Is.EqualTo(960).Within(0.1));
    }

    [Test]
    public async Task DifferentRoomAspectRatios_RefitBothModes_AndKeepWorldChoiceAccessible()
    {
        await using var server = new TestServerFixture(externalWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../tests/Fixtures/reference-v1"));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(server.TempRoot, "world", Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        var garden = Path.Combine(server.TempRoot, "world", "rooms", "garden.yaml");
        File.WriteAllText(garden, File.ReadAllText(garden).Replace("height: 360", "height: 540"));
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 480 }
        });
        var page = await context.NewPageAsync();
        var png = await page.EvaluateAsync<string>("""
            () => {
                const canvas = document.createElement('canvas');
                canvas.width = 640; canvas.height = 540;
                canvas.getContext('2d').fillRect(0, 0, 640, 540);
                return canvas.toDataURL().split(',')[1];
            }
            """);
        await File.WriteAllBytesAsync(Path.Combine(server.TempRoot, "world", "assets", "images", "garden.png"), Convert.FromBase64String(png));
        await server.StartAsync();
        await page.GotoAsync(server.BaseUrl);
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        var normalWidth = (await Sizes(page))[0];
        foreach (var expanded in new[] { false, true })
        {
            if (expanded) await page.ClickAsync("#game-scale");
            await page.EvaluateAsync("() => window.__ADVENTURE_GAME__.setTarget({x:580,y:300})");
            await page.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.character.position.x > 579");
            Assert.That(await page.EvaluateAsync<bool>("() => window.__ADVENTURE_GAME__.changeRoom('to-garden')"), Is.True);
            await page.WaitForFunctionAsync("() => document.querySelector('#game-canvas').height === 540");
            await NextLayout(page);
            await AssertFitsWindow(page);
            var canvas = (await page.Locator("#game-canvas").BoundingBoxAsync())!;
            Assert.That(canvas.Width / canvas.Height, Is.EqualTo(640.0 / 540).Within(0.001));
            Assert.That((await Sizes(page))[0], Is.LessThan(normalWidth));
            var leave = (await page.Locator("#world-leave").BoundingBoxAsync())!;
            Assert.That(leave.Y, Is.GreaterThanOrEqualTo(0));
            Assert.That(leave.Y + leave.Height, Is.LessThan(480));
            Assert.That(await page.EvaluateAsync<bool>("() => window.__ADVENTURE_GAME__.changeRoom('to-lounge')"), Is.True);
            await page.WaitForFunctionAsync("() => document.querySelector('#game-canvas').height === 360");
            await NextLayout(page);
            await AssertFitsWindow(page);
        }
        await page.ClickAsync("#world-leave");
        await Assertions.Expect(page.Locator("#world-enter")).ToBeVisibleAsync();
    }

    private static Task NextLayout(IPage page) => page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private static Task<double[]> Sizes(IPage page) => page.EvaluateAsync<double[]>("""
        () => ['#game-container', '#game-canvas', '#action-bar', '#chat-container', '#chat-input', '#game-scale']
            .flatMap(selector => { const r = document.querySelector(selector).getBoundingClientRect(); return [r.width, r.height]; })
        """);

    private static async Task AssertFitsWindow(IPage page)
    {
        var viewport = page.ViewportSize!;
        foreach (var selector in new[] { "#chat-input", "#chat-send", "#game-container", "#game-canvas",
                     "#action-bar", "#chat-container", "#chat-messages", "#game-scale" })
        {
            var box = (await page.Locator(selector).BoundingBoxAsync())!;
            Assert.That(box.X, Is.GreaterThanOrEqualTo(-0.5), $"{selector} left");
            Assert.That(box.Y, Is.GreaterThanOrEqualTo(-0.5), $"{selector} top");
            Assert.That(box.X + box.Width, Is.LessThanOrEqualTo(viewport.Width + 0.5), $"{selector} right");
            Assert.That(box.Y + box.Height, Is.LessThanOrEqualTo(viewport.Height + 0.5), $"{selector} bottom");
            Assert.That(box.Height, Is.GreaterThan(0), $"{selector} must remain visible");
        }
    }
}

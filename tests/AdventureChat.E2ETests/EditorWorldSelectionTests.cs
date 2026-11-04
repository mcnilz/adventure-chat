using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class EditorWorldSelectionTests
{
    [Test]
    public async Task Editor_SelectedWorld_SaveAndReloadDoesNotChangeOtherWorld()
    {
        await using var server = new TestServerFixture(authoring: true, multipleWorlds: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl + "/editor.html");
        await page.Locator("#world-select").SelectOptionAsync("other");
        var editingImage = page.WaitForResponseAsync(r => r.Url.EndsWith("/world-assets/other/images/garden.png") && r.Status == 200);
        await page.Locator("#world-enter").ClickAsync();
        var editingBytes = await (await editingImage).BodyAsync();
        await page.WaitForFunctionAsync("() => window.__ROOM_EDITOR__?.getState().getRoom().id === 'garden'");
        var previewImage = page.WaitForResponseAsync(r => r.Url.EndsWith("/world-assets/other/images/garden.png") && r.Status == 200);
        await page.ClickAsync("#btn-toggle-mode");
        Assert.That(await (await previewImage).BodyAsync(), Is.EqualTo(editingBytes));
        var firstImage = await page.APIRequest.GetAsync(server.BaseUrl + "/world-assets/reference/images/garden.png");
        Assert.That(editingBytes, Is.Not.EqualTo(await firstImage.BodyAsync()));
        await page.ClickAsync("#btn-toggle-mode");
        await page.Locator("#input-name").FillAsync("Nur andere Welt");
        await page.Locator("#btn-save").ClickAsync();
        await page.WaitForFunctionAsync("() => window.__ROOM_EDITOR__?.getState().isDirty() === false");
        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => window.__ROOM_EDITOR__?.getState().getRoom().name === 'Nur andere Welt'");
        var other = await page.APIRequest.GetAsync(server.BaseUrl + "/api/worlds/other/rooms/garden");
        var first = await page.APIRequest.GetAsync(server.BaseUrl + "/api/worlds/reference/rooms/garden");
        Assert.That(await other.TextAsync(), Does.Contain("Nur andere Welt"));
        Assert.That(await first.TextAsync(), Does.Not.Contain("Nur andere Welt"));
        Assert.That((await page.APIRequest.GetAsync(server.BaseUrl + "/api/editor/rooms?worldId=missing")).Status, Is.EqualTo(404));
    }
}

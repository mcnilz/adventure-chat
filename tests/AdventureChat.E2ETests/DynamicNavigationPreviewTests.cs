using System.Text.Json;
using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class DynamicNavigationPreviewTests
{
    [Test]
    public async Task ReviewDoor_ApproachesAndUnlocksTheDoorwayInGameAndPreview()
    {
        await using var server = DynamicNavigationTests.CreateServer("interaction-review/navigation");
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var game = await browser.NewPageAsync();
        await game.GotoAsync(server.BaseUrl);
        await game.ClickAsync("#world-enter");
        await game.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        await DynamicNavigationTests.ClickCanvas(game, 300, 275);
        Assert.That(await game.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.id"), Is.EqualTo("hall"));
        Assert.That(await game.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.exits.length"), Is.Zero);
        await game.GetByRole(AriaRole.Button, new() { Name = "Öffnen", Exact = true }).ClickAsync();
        await DynamicNavigationTests.ClickCanvas(game, 290, 250);
        await game.WaitForFunctionAsync("""
            () => {
              const game = window.__ADVENTURE_GAME__;
              return game.character.state === 'walk' || game.room.exits.length > 0;
            }
            """, null, new() { Timeout = 5000 });
        Assert.That(await game.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.exits.length"), Is.Zero,
            "The door opened before the player approached it.");
        await game.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.exits.length === 1");
        Assert.That(await game.EvaluateAsync<double>("window.__ADVENTURE_GAME__.character.position.x"), Is.InRange(240, 270));
        var door = await game.EvaluateAsync<JsonElement>("window.__ADVENTURE_GAME__.room.objects.find(object => object.id === 'door')");
        Assert.That(door.GetProperty("image").GetString(), Does.EndWith("/door-open.png"));
        Assert.That(door.GetProperty("position").GetProperty("x").GetInt32(), Is.EqualTo(280));
        await DynamicNavigationTests.ClickCanvas(game, 300, 275);
        await game.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__.room.id === 'next'", null, new() { Timeout = 6000 });

        var editor = await browser.NewPageAsync();
        await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=navigation");
        await editor.Locator("#object-list option[value=door]").WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await editor.ClickAsync("#btn-toggle-mode");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("open");
        var first = editor.WaitForResponseAsync(response => response.Url.Contains("/preview-path") || response.Url.Contains("/preview-action"));
        var action = editor.WaitForResponseAsync(response => response.Url.Contains("/preview-action"));
        await DynamicNavigationTests.ClickCanvas(editor, 290, 250);
        Assert.That((await first).Url, Does.Contain("/preview-path"));
        var arrival = JsonDocument.Parse((await action).Request.PostData!).RootElement.GetProperty("position");
        Assert.That(arrival.GetProperty("x").GetDouble(), Is.InRange(240, 270));
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Aktion ausgeführt.");
        await editor.GetByLabel("Vorschauverb").SelectOptionAsync("walk");
        await DynamicNavigationTests.ClickCanvas(editor, 300, 275);
        await Assertions.Expect(editor.Locator("#preview-result")).ToHaveTextAsync("Raum: Nebenraum");
    }

    [Test]
    public async Task Preview_UsesServerGeometryAndActionsWithoutChangingDraftOrLiveWorld()
    {
        await using var server = DynamicNavigationTests.CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl + "/editor.html?worldId=navigation");
        await page.WaitForFunctionAsync("() => window.__ROOM_EDITOR__?.getState().getRoom().id === 'hall'");
        var draft = await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
        await page.ClickAsync("#btn-toggle-mode");
        var deniedResponse = page.WaitForResponseAsync(r => r.Url.Contains("/preview-path") && r.Request.Method == "POST");
        await DynamicNavigationTests.ClickCanvas(page, 500, 300);
        Assert.That(JsonDocument.Parse(await (await deniedResponse).TextAsync()).RootElement.GetProperty("success").GetBoolean(), Is.False);
        await page.GetByLabel("Vorschauverb").SelectOptionAsync("open");
        var actionResponse = page.WaitForResponseAsync(r => r.Url.Contains("/preview-action") && r.Request.Method == "POST");
        await DynamicNavigationTests.ClickCanvas(page, 290, 250);
        var action = JsonDocument.Parse(await (await actionResponse).TextAsync()).RootElement;
        Assert.That(action.GetProperty("success").GetBoolean(), Is.True, action.ToString());
        Assert.That(action.GetProperty("snapshot").GetProperty("room").GetProperty("exits").GetArrayLength(), Is.EqualTo(1));
        await page.GetByLabel("Vorschauverb").SelectOptionAsync("walk");
        var allowedResponse = page.WaitForResponseAsync(r => r.Url.Contains("/preview-path") && r.Request.Method == "POST");
        await DynamicNavigationTests.ClickCanvas(page, 475, 250);
        Assert.That(JsonDocument.Parse(await (await allowedResponse).TextAsync()).RootElement.GetProperty("success").GetBoolean(), Is.True);
        await DynamicNavigationTests.ClickCanvas(page, 500, 300);
        await Assertions.Expect(page.Locator("#preview-result")).ToHaveTextAsync("Raum: Nebenraum");
        await DynamicNavigationTests.ClickCanvas(page, 100, 300);
        await Assertions.Expect(page.Locator("#preview-result")).ToHaveTextAsync("Raum: Dynamische Durchgänge");
        await page.ClickAsync("#btn-toggle-mode");
        Assert.That(await page.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(draft));
        await page.ClickAsync("#btn-toggle-mode");
        var resetResponse = page.WaitForResponseAsync(r => r.Url.Contains("/preview-path") && r.Request.Method == "POST");
        await DynamicNavigationTests.ClickCanvas(page, 475, 250);
        Assert.That(JsonDocument.Parse(await (await resetResponse).TextAsync()).RootElement.GetProperty("success").GetBoolean(), Is.False);
        var live = await browser.NewPageAsync(); await live.GotoAsync(server.BaseUrl); await live.ClickAsync("#world-enter");
        await live.WaitForFunctionAsync("() => window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await live.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.exits.length"), Is.Zero);
        Assert.That(await live.EvaluateAsync<int>("window.__ADVENTURE_GAME__.room.objects[0].position.x"), Is.EqualTo(280));
    }

    [Test]
    public async Task Preview_RejectsUnsafeClosureAndUnknownStatesThroughPublicRequests()
    {
        await using var server = DynamicNavigationTests.CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const status = await (await fetch('/api/editor/status?worldId=navigation')).json();
                const room = await (await fetch('/api/editor/rooms/hall?worldId=navigation')).json();
                const post = (path, body) => fetch('/api/editor/' + path + '?worldId=navigation', {
                    method:'POST', headers:{'Content-Type':'application/json','X-Author-Token':status.token}, body:JSON.stringify(body)});
                const states = [{roomId:'hall', states:[{objectId:'door',stateId:'open'}]}];
                const blocked = await (await post('preview-action', {room,position:{x:300,y:300},objectId:'door',actionId:'close',states})).json();
                const unknown = await post('preview-path', {room,start:{x:100,y:300},target:{x:500,y:300},states:[{objectId:'door',stateId:'missing'}]});
                const outside = await post('preview-action', {room,position:{x:300,y:300},objectId:'door',actionId:'open',states:[]});
                return {blocked,unknown:unknown.status,outside:outside.status};
            }
            """);
        Assert.That(result.GetProperty("blocked").GetProperty("success").GetBoolean(), Is.False);
        Assert.That(result.GetProperty("blocked").GetProperty("error").GetString(), Does.Contain("überdecken"));
        Assert.That(result.GetProperty("unknown").GetInt32(), Is.EqualTo(400));
        Assert.That(result.GetProperty("outside").GetInt32(), Is.EqualTo(400));
    }
}

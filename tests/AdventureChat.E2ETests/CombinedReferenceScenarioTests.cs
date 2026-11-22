using Microsoft.Playwright;
using NUnit.Framework;
using static AdventureChat.E2ETests.DynamicNavigationTests;
using static AdventureChat.E2ETests.ReferenceWorkshopTests;

namespace AdventureChat.E2ETests;

public class CombinedReferenceScenarioTests
{
    [Test]
    public async Task BellConversationLeadsThroughTheGardenToTheWorkshop()
    {
        await using var server = CreateReferenceCopy();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var guest = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await guest.GotoAsync(server.BaseUrl);
        await guest.FillAsync("#player-name", "Rundgang");
        await Assertions.Expect(guest.Locator("#character-select")).ToHaveValueAsync("mara");
        await guest.ClickAsync("#world-enter");
        await guest.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");

        await Verb(guest, "Spreche mit", (112, 234));
        await Assertions.Expect(Button(guest, "Führung starten")).ToBeVisibleAsync();
        await Assertions.Expect(Button(guest, "Hast du die Klingel gehört?")).ToBeHiddenAsync();
        await Button(guest, "Gespräch beenden").ClickAsync();
        await Assertions.Expect(Button(guest, "Führung beenden")).ToBeHiddenAsync();
        await Verb(guest, "Drücken", (203, 240));
        await Assertions.Expect(guest.Locator("#action-feedback")).ToHaveTextAsync("Aktion ausgeführt.");
        await guest.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 160 && !npc.motion?.route.length)");
        await Verb(guest, "Spreche mit", (160, 269));
        await Button(guest, "Hast du die Klingel gehört?").ClickAsync();
        await guest.Keyboard.PressAsync(".");
        await Assertions.Expect(guest.Locator("#npc-reply")).ToContainTextAsync("den Garten und die Werkstatt zeigen");
        await guest.Keyboard.PressAsync(".");

        var observer = await JoinWorkshop(browser, server.BaseUrl, "bruno");
        await Button(guest, "Führung starten").ClickAsync();
        var message = guest.Locator("#npc-tour-message");
        await Assertions.Expect(message).ToContainTextAsync("Hier ist der Tavernen-Garten.", new() { Timeout = 16000 });
        await Assertions.Expect(message).ToContainTextAsync("Werkstatttafel", new() { Timeout = 22000 });
        await observer.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.npcs.some(npc => npc.name === 'Nela')");
        await Assertions.Expect(message).ToContainTextAsync("Stuhl", new() { Timeout = 14000 });
        await Assertions.Expect(message).ToContainTextAsync("Hubtisch", new() { Timeout = 14000 });
        await Capture(guest, "guided-visit");
        await Assertions.Expect(guest.Locator("#action-feedback"))
            .ToHaveTextAsync("Die Führung ist abgeschlossen.", new() { Timeout = 12000 });
        await guest.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'workshop'");
        await observer.WaitForFunctionAsync("""
            window.__ADVENTURE_GAME__.room.npcs.some(npc => npc.name === 'Nela' && npc.motion?.route.at(-1)?.x === 49)
            """);

        await Verb(guest, "Drücken", (495, 77));
        await observer.WaitForFunctionAsync("""
            window.__ADVENTURE_GAME__.room.objects.find(object => object.id === 'lift-plate').position.y === 132
            """);
        await Verb(guest, "Ziehen", ChairAtTable);
        await ExpectObjectX(observer, "chair", 204);
        await Verb(guest, "Benutzen", (224, 214));
        await ExpectGuestAnimation(observer, "workshop-mara-seated.png", null);
        await Verb(guest, "Benutzen", (132, 52));
        await ExpectGuestAnimation(observer, "workshop-mara-gesture.png", 1500);
        await observer.WaitForFunctionAsync("""
            window.__ADVENTURE_GAME__.room.objects.find(object => object.id === 'board').image.endsWith('-written.png')
            """);
        await ClickCanvas(observer, ReturnDoor.X, ReturnDoor.Y);
        await observer.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id === 'garden'");
        await ClickCanvas(observer, 66, 220);
        await observer.WaitForFunctionAsync("""
            window.__ADVENTURE_GAME__.room.id === 'lounge' &&
            window.__ADVENTURE_GAME__.room.npcs.some(npc => npc.id === 'nela' && npc.position.x === 112 && !npc.motion?.route.length)
            """);
        await Verb(observer, "Spreche mit", (112, 234));
        await Assertions.Expect(Button(observer, "Führung starten")).ToBeVisibleAsync();
    }

    private static ILocator Button(IPage page, string label) =>
        page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });

    private static Task ExpectGuestAnimation(IPage page, string sprite, int? durationMs) =>
        page.WaitForFunctionAsync("""
            ({ sprite, durationMs }) => [...window.__ADVENTURE_GAME__.remotePlayers.values()].some(actor =>
                actor.name === 'Rundgang' && actor.actorAnimation?.image?.endsWith(sprite)
                && actor.actorAnimation.durationMs === durationMs)
            """, new { sprite, durationMs });
}

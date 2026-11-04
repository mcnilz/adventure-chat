using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

namespace AdventureChat.E2ETests;

public class CharacterSelectionTests
{
    [Test]
    public async Task Catalog_TwoBrowsersSeeSelectionAcrossRoomChangeAndWorldSwitch()
    {
        await using var server = new TestServerFixture(characterWorld: true, multipleWorlds: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(); var b = await browser.NewPageAsync();
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl);
            await page.SelectOptionAsync("#world-select", "characters-a");
            await page.WaitForFunctionAsync("document.querySelector('#world-enter').disabled === false");
        }
        await a.FillAsync("#player-name", "Mara");
        await a.SelectOptionAsync("#character-select", "tall");
        await a.WaitForFunctionAsync("document.querySelector('#character-preview').getContext('2d').getImageData(64,50,1,1).data[3] === 255");
        await a.ClickAsync("#world-enter");
        await b.FillAsync("#player-name", "Bruno"); await b.ClickAsync("#world-enter");
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.remotePlayers.size === 1");
        await b.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.remotePlayers.size === 1");
        Assert.That(await b.EvaluateAsync<string>("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].appearance.id"), Is.EqualTo("tall"));
        Assert.That(await a.EvaluateAsync<string>("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].name"), Is.EqualTo("Bruno"));
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('door')"), Is.True);
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.characterId"), Is.EqualTo("tall"));
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('door')"), Is.True);
        await a.ClickAsync("#world-leave");
        await a.SelectOptionAsync("#world-select", "characters-b");
        await a.WaitForFunctionAsync("!document.querySelector('#world-enter').disabled");
        Assert.That(await a.InputValueAsync("#character-select"), Is.EqualTo("hero"));
        await a.ClickAsync("#world-enter");
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.appearance.imageUrl"), Is.EqualTo("/world-assets/characters-b/images/small.png"));
        using var http = new HttpClient();
        var imageA = await http.GetByteArrayAsync(server.BaseUrl + "/world-assets/characters-a/images/small.png");
        var imageB = await http.GetByteArrayAsync(server.BaseUrl + "/world-assets/characters-b/images/small.png");
        Assert.That(imageA, Is.Not.EqualTo(imageB));
    }

    [Test]
    public async Task Hub_RejectsUnknownIdsAndUsesWorldDefault()
    {
        await using var server = new TestServerFixture(characterWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const c = new signalR.HubConnectionBuilder().withUrl('/hub/room').build(); await c.start();
              try {
                const bad = await c.invoke('JoinRoom',{worldId:'characters-a',roomId:'hall',displayName:'Test',characterId:'../other.png'});
                const joined = await c.invoke('JoinRoom',{worldId:'characters-a',roomId:'hall',displayName:'Test'});
                return {bad:bad.success, id:joined.snapshot.players[0].characterId};
              } finally { await c.stop(); }
            }
            """);
        Assert.That(result.GetProperty("bad").GetBoolean(), Is.False);
        Assert.That(result.GetProperty("id").GetString(), Is.EqualTo("hero"));
    }

    [Test]
    public async Task Renderer_BothRastersKeepHeightAndFootpointForEveryDirectionAndFrame()
    {
        await using var server = new TestServerFixture(characterWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var samples = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const {CharacterRenderer} = await import('/js/rendering/character-renderer.js');
              const catalog = await (await fetch('/api/worlds/characters-a/characters')).json();
              const renderer = new CharacterRenderer(); const canvas=document.createElement('canvas'); canvas.width=200; canvas.height=180;
              const ctx=canvas.getContext('2d'); const results=[];
              for (const appearance of catalog.characters) {
                const img=new Image(); img.src=appearance.imageUrl; await img.decode();
                for (const facing of ['right','left','up','down']) for (const state of ['idle','walk']) for (let frame=0;frame<4;frame++) {
                  const character={id:'x',name:'',position:{x:100,y:140},facing,state,walkFrame:frame,appearance};
                  renderer.drawCharacter(ctx,character,true,{actorHeight:48,perspective:'top-down'});
                  await new Promise(r=>setTimeout(r,10)); ctx.clearRect(0,0,200,180);
                  renderer.drawCharacter(ctx,character,true,{actorHeight:48,perspective:'top-down'});
                  const center=[...ctx.getImageData(100,120,1,1).data];
                  const above=[...ctx.getImageData(100,91,1,1).data]; const below=[...ctx.getImageData(100,140,1,1).data];
                  results.push({id:appearance.id,facing,state,frame,center,above,below});
                }
              }
              return results;
            }
            """);
        foreach (var sample in samples.EnumerateArray())
        {
            var facing = sample.GetProperty("facing").GetString();
            var row = facing == "up" ? 1 : facing == "down" ? 2 : 0;
            var frame = sample.GetProperty("state").GetString() == "idle" ? 0 : sample.GetProperty("frame").GetInt32();
            Assert.That(sample.GetProperty("center").EnumerateArray().Select(v => v.GetInt32()), Is.EqualTo(new[] { 30 + frame * 50, 40 + row * 60, 200, 255 }));
            Assert.That(sample.GetProperty("below")[3].GetInt32(), Is.Zero);
            // Name badge is above the sprite; x=100,y=91 is one pixel above its top edge.
            Assert.That(sample.GetProperty("above")[3].GetInt32(), Is.Zero);
        }
    }

    [Test]
    public async Task Entry_NameIsEditableBeforeJoining()
    {
        await using var server = new TestServerFixture(externalWorld: true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        await page.FillAsync("#player-name", "Mara Test");
        await page.ClickAsync("#world-enter");
        await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus === 'connected'");
        Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.name"), Is.EqualTo("Mara Test"));
    }
}

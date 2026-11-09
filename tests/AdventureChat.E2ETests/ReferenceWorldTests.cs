using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

namespace AdventureChat.E2ETests;

public class ReferenceWorldTests
{
    private static TestServerFixture CreateServer()
    {
        var server = new TestServerFixture(authoring: true, externalWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds/reference"));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(server.TempRoot, "world", Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return server;
    }

    private static async Task Walk(IPage page, int x, int y)
    {
        Assert.That(await page.EvaluateAsync<bool>("async p => (await window.__ADVENTURE_GAME__.setTarget(p)).accepted", new { x, y }), Is.True);
        await page.WaitForFunctionAsync("p => {const c=window.__ADVENTURE_GAME__.character; return Math.hypot(c.position.x-p.x,c.position.y-p.y)<2}", new { x, y });
    }

    [Test]
    public async Task TwoFigures_WalkChatChangeRoomsReconnectAndEditorRoundtrip()
    {
        await using var server = CreateServer(); await server.StartAsync();
        await using var proxy = new InterruptibleProxy(server.BaseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(); var b = await browser.NewPageAsync();
        foreach (var (page, id) in new[] { (a, "mara"), (b, "bruno") })
        {
            await page.GotoAsync(proxy.BaseUrl);
            await page.WaitForFunctionAsync("!document.querySelector('#world-enter').disabled");
            await page.FillAsync("#player-name", id);
            await page.SelectOptionAsync("#character-select", id); await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus==='connected'");
        }
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        Assert.That(await a.EvaluateAsync<string>("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].appearance.id"), Is.EqualTo("bruno"));
        await a.FillAsync("#chat-input", "Am Tisch"); await a.ClickAsync("#chat-send");
        await b.WaitForFunctionAsync("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].speechBubble?.text==='Am Tisch'");
        foreach (var page in new[] { a, b })
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-lounge-front-{(page == a ? "mara" : "bruno")}.png") });
            await Walk(page, 330, 258);
            await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-lounge-behind-{(page == a ? "mara" : "bruno")}.png") });
            await Walk(page, 330, 330);
            await Walk(page, 572, 244);
            Assert.That(await page.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('to-garden')"), Is.True);
            await Walk(page, 330, 278);
            await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-garden-behind-{(page == a ? "mara" : "bruno")}.png") });
            await Walk(page, 330, 330);
            await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-garden-{(page == a ? "mara" : "bruno")}.png") });
            await Walk(page, 66, 304);
            Assert.That(await page.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('to-lounge')"), Is.True);
        }
        var oldSession = await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.sessionId");
        proxy.Interrupt();
        await a.WaitForFunctionAsync("id=>window.__ADVENTURE_GAME__.connectionStatus==='connected' && window.__ADVENTURE_GAME__.sessionId!==id", oldSession);
        Assert.That(await a.EvaluateAsync<string>("window.__ADVENTURE_GAME__.character.appearance.id"), Is.EqualTo("mara"));
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        var editor = await browser.NewPageAsync(); await editor.GotoAsync(server.BaseUrl + "/editor.html?worldId=reference");
        await editor.WaitForFunctionAsync("window.__ROOM_EDITOR__ !== undefined");
        foreach (var room in new[] { "lounge", "garden" })
        {
            await editor.EvaluateAsync("id => window.__ROOM_EDITOR__.loadRoom(id)", room);
            var before = await editor.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())");
            Assert.That(await editor.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"), Is.True);
            await editor.EvaluateAsync("id => window.__ROOM_EDITOR__.loadRoom(id)", room);
            Assert.That(await editor.EvaluateAsync<string>("JSON.stringify(window.__ROOM_EDITOR__.getState().getRoom())"), Is.EqualTo(before));
            await editor.ClickAsync("#btn-toggle-mode");
            var response = editor.WaitForResponseAsync(r => r.Url.Contains("/api/editor/preview-path") && r.Request.Method == "POST");
            var canvas = editor.Locator("#editor-canvas");
            var bounds = await canvas.BoundingBoxAsync();
            await canvas.ClickAsync(new() { Position = new() { X = bounds!.Width * 330 / 640, Y = bounds.Height * (room == "lounge" ? 258 : 278) / 360 } });
            Assert.That((await response).Ok, Is.True);
            await editor.WaitForTimeoutAsync(2500);
            await editor.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"reference-editor-{room}.png") });
            await editor.ClickAsync("#btn-toggle-mode");
        }
    }

    [Test]
    public async Task ActualFigures_AllDirectionsAndPhasesKeepTheirFootpoint()
    {
        await using var server = CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var samples = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const {CharacterRenderer}=await import('/js/rendering/character-renderer.js');
              const catalog=await (await fetch('/api/worlds/reference/characters')).json();
              await Promise.all(catalog.characters.map(async c=>{const image=new Image();image.src=c.imageUrl;await image.decode()}));
              const room=await (await fetch('/api/worlds/reference/rooms/lounge')).json();
              const canvas=document.createElement('canvas');canvas.width=256;canvas.height=180;
              const ctx=canvas.getContext('2d'),renderer=new CharacterRenderer(),results=[];
              const sheet=document.createElement('canvas');sheet.id='poses';sheet.width=1280;sheet.height=1440;document.body.replaceChildren(sheet);
              const out=sheet.getContext('2d');out.fillStyle='#d0d8d8';out.fillRect(0,0,1280,1440);
              let row=0;
              for(const appearance of catalog.characters) for(const facing of ['right','left','up','down']) {
                const image=new Image();image.src=appearance.imageUrl;await image.decode();
                const original=document.createElement('canvas');original.width=image.naturalWidth;original.height=image.naturalHeight;
                original.getContext('2d').drawImage(image,0,0);
                for(let phase=0;phase<5;phase++) {
                  const c={id:'x',name:'',position:{x:128,y:160},facing,state:phase===0?'idle':'walk',walkFrame:Math.max(0,phase-1),appearance};
                  renderer.drawCharacter(ctx,c,true,room,false);
                  await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));ctx.clearRect(0,0,256,180);
                  renderer.drawCharacter(ctx,c,true,room,false);
                  const pixels=ctx.getImageData(0,0,256,180).data;let minX=256,maxX=0,minY=180,maxY=0;
                  for(let y=0;y<180;y++)for(let x=0;x<256;x++)if(pixels[(y*256+x)*4+3]>127){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y)}
                  const frame=phase===0?0:phase-1,rowIndex=facing==='up'?1:facing==='down'?2:0;
                  const expected=original.getContext('2d').getImageData(frame*128,rowIndex*128,128,128).data;
                  let nativeDifferences=0;
                  for(let y=0;y<128;y++)for(let x=0;x<128;x++)for(let channel=0;channel<4;channel++) {
                    if(pixels[((y+32)*256+x+64)*4+channel]!==expected[(y*128+(facing==='left'?127-x:x))*4+channel])nativeDifferences++;
                  }
                  results.push({id:appearance.id,facing,phase,minX,maxX,minY,maxY,nativeDifferences});
                  out.drawImage(canvas,phase*256,row*180);out.fillStyle='#172733';out.fillText(appearance.id+' '+facing+' '+(phase===0?'idle':phase-1),phase*256+4,row*180+12);
                }
                row++;
              }
              return results;
            }
            """);
        foreach (var sample in samples.EnumerateArray())
        {
            Assert.That(sample.GetProperty("maxY").GetInt32(), Is.EqualTo(159), sample.ToString());
            Assert.That(sample.GetProperty("minY").GetInt32(), Is.EqualTo(35), "Delivered native art has 125 visible pixels and 3px top padding: " + sample);
            Assert.That(sample.GetProperty("nativeDifferences").GetInt32(), Is.Zero, "Native art must render pixel-for-pixel without scaling: " + sample);
            Assert.That(sample.GetProperty("minX").GetInt32(), Is.GreaterThan(70), sample.ToString());
            Assert.That(sample.GetProperty("maxX").GetInt32(), Is.LessThan(186), sample.ToString());
        }
        await page.Locator("#poses").ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reference-poses.png") });
    }

    [Test]
    public async Task ActualArt_OverlaysWithoutSeamsAndOccludesOnlyBehindTables()
    {
        await using var server = CreateServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        await using var comparison = await page.EvaluateHandleAsync("""
            async () => {
              const {RoomRenderer}=await import('/js/rendering/renderer.js');
              const catalog=await (await fetch('/api/worlds/reference/characters')).json();
              const results=[];
              for(const id of ['lounge','garden']) {
                const room=await (await fetch('/api/worlds/reference/rooms/'+id)).json();
                for(const appearance of catalog.characters) for(const y of [id==='lounge'?258:278,330]) {
                  const canvases=[document.createElement('canvas'),document.createElement('canvas')];
                  const renderers=[new RoomRenderer(canvases[0],room),new RoomRenderer(canvases[1],{...room,objects:[]})];
                  const c={id:'x',name:'',position:{x:330,y},facing:'down',state:'idle',walkFrame:0,appearance};
                  for(let i=0;i<2;i++)renderers[i].render(c);
                  results.push(() => {
                    for(let i=0;i<2;i++)renderers[i].render(c);
                    const pixels=canvases.map(c=>c.getContext('2d').getImageData(0,0,640,360).data);
                    let changed=0;for(let i=0;i<pixels[0].length;i+=4)if(pixels[0][i]!==pixels[1][i]||pixels[0][i+1]!==pixels[1][i+1]||pixels[0][i+2]!==pixels[1][i+2])changed++;
                    return {id,figure:appearance.id,y,changed};
                  });
                }
              }
              return () => results.map(measure => measure());
            }
            """);
        // The actual renderer instances start their own image requests; preloading other Image objects is insufficient.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var samples = await comparison.EvaluateAsync<JsonElement>("measure => measure()");
        foreach (var sample in samples.EnumerateArray())
        {
            var changes = sample.GetProperty("changed").GetInt32();
            if (sample.GetProperty("y").GetInt32() == 330) Assert.That(changes, Is.Zero, sample.ToString());
            else Assert.That(changes, Is.GreaterThan(10), sample.ToString());
        }
    }
}

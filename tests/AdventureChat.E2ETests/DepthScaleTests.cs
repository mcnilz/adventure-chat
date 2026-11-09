using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class DepthScaleTests
{
    [Test]
    public async Task DepthLabels_RemainVisibleWhenScaledHeadReachesTopEdge()
    {
        await using var server = new TestServerFixture(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var positions = await page.EvaluateAsync<double[]>("""
            async () => {
                const {CharacterRenderer}=await import('/js/rendering/character-renderer.js');
                const {SpeechBubbleRenderer}=await import('/js/rendering/speech-bubble-renderer.js');
                const canvas=document.createElement('canvas'); canvas.width=640; canvas.height=360;
                const ctx=canvas.getContext('2d'), ys=[];
                ctx.fillText=(text,x,y)=>ys.push(y);
                const c={name:'Tester',position:{x:320,y:140},speechBubble:{text:'Hallo',startTime:0,duration:6000}};
                const room={actorHeight:64,depthScale:{back:{y:140,factor:2},front:{y:320,factor:1}}};
                new CharacterRenderer().drawName(ctx,c,true,room);
                new SpeechBubbleRenderer().drawSpeechBubble(ctx,c,640,1,128,0,true);
                return ys;
            }
            """);
        Assert.That(positions, Has.Length.EqualTo(2));
        Assert.That(positions.All(y => y >= 10 && y < 360), Is.True);
    }

    private static TestServerFixture CreateDemoServer(string worldId = "scroll")
    {
        var server = new TestServerFixture(authoring: true, scrollWorld: true);
        var source = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../worlds", worldId));
        var target = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);
        }
        var manifest = Path.Combine(target, "world.yaml");
        File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("startRoom: gallery", "startRoom: tour-combined").Replace("startSpawn: left", "startSpawn: entry"));
        return server;
    }

    [Test]
    public async Task ReferenceCatalog_RendersOptionalOverheadSheetAndKeepsAdventureSheet()
    {
        await using var server = CreateDemoServer("reference"); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var differences = await page.EvaluateAsync<int[]>("""
            async () => {
                const {CharacterRenderer}=await import('/js/rendering/character-renderer.js');
                const catalog=await (await fetch('/api/worlds/reference/characters')).json(), results=[];
                const canvas=document.createElement('canvas');canvas.width=256;canvas.height=180;
                const ctx=canvas.getContext('2d'),renderer=new CharacterRenderer();
                const sheet=document.createElement('canvas');sheet.width=512;sheet.height=360;sheet.id='overhead';document.body.replaceChildren(sheet);
                let index=0;
                for(const appearance of catalog.characters) for(const perspective of ['top-down','adventure']) {
                    const source=new Image();source.src=perspective==='top-down'?appearance.topDownImageUrl:appearance.imageUrl;await source.decode();
                    const c={position:{x:128,y:160},name:'',facing:'down',state:'idle',walkFrame:0,appearance};
                    renderer.drawCharacter(ctx,c,true,{actorHeight:128,perspective},false);
                    await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));
                    ctx.clearRect(0,0,256,180);renderer.drawCharacter(ctx,c,true,{actorHeight:128,perspective},false);
                    const expected=document.createElement('canvas');expected.width=128;expected.height=128;
                    expected.getContext('2d').drawImage(source,0,256,128,128,0,0,128,128);
                    const a=ctx.getImageData(64,32,128,128).data,b=expected.getContext('2d').getImageData(0,0,128,128).data;
                    let count=0;for(let i=0;i<a.length;i++)if(a[i]!==b[i])count++;
                    results.push(count);sheet.getContext('2d').drawImage(canvas,(index%2)*256,Math.floor(index/2)*180);index++;
                }
                return results;
            }
            """);
        Assert.That(differences, Is.EqualTo(new[] { 0, 0, 0, 0 }));
        await page.Locator("#overhead").ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "depth-reference-overhead.png") });
    }

    private static async Task Walk(IPage page, int x, int y)
    {
        Assert.That(await page.EvaluateAsync<bool>("async p => (await window.__ADVENTURE_GAME__.setTarget(p)).accepted", new { x, y }), Is.True);
        await page.WaitForFunctionAsync("p => { const c=window.__ADVENTURE_GAME__.character; return c.state==='idle' && Math.hypot(c.position.x-p.x,c.position.y-p.y)<2; }", new { x, y });
    }

    [Test]
    public async Task TwoBrowsers_SeeSameDepthAcrossCameraOffsetsAndWindowSizes()
    {
        await using var server = CreateDemoServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var a = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1200, Height = 800 } });
        var b = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 800, Height = 700 } });
        foreach (var page in new[] { a, b })
        {
            await page.GotoAsync(server.BaseUrl); await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus==='connected'");
        }
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        await Walk(a, 960, 140);
        await b.WaitForFunctionAsync("[...window.__ADVENTURE_GAME__.remotePlayers.values()][0].state==='idle' && [...window.__ADVENTURE_GAME__.remotePlayers.values()][0].position.x>950");
        Assert.That(await a.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset"), Is.GreaterThan(300));
        Assert.That(await b.EvaluateAsync<double>("window.__ADVENTURE_GAME__.renderer.camera.offset"), Is.EqualTo(0));
        const string measure = "async remote => {const {actorHeightAt}=await import('/js/rendering/depth-scale.js');const g=window.__ADVENTURE_GAME__;return actorHeightAt(g.room,(remote?[...g.remotePlayers.values()][0]:g.character).position.y)}";
        Assert.That(await a.EvaluateAsync<double>(measure, false), Is.EqualTo(32));
        Assert.That(await b.EvaluateAsync<double>(measure, true), Is.EqualTo(32));
        await a.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "depth-combined-back.png") });
        await Walk(a, 1180, 325);
        Assert.That(await a.EvaluateAsync<double>(measure, false), Is.EqualTo(64));
        await a.FillAsync("#chat-input", "Auf dem Hügel"); await a.PressAsync("#chat-input", "Enter");
        await b.WaitForFunctionAsync("window.__ADVENTURE_GAME__.chatMessages.some(m=>m.text==='Auf dem Hügel')");
        await a.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "depth-combined-front.png") });
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('next')"), Is.True);
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id==='tour-map'");
        await Walk(a, 1180, 325);
        Assert.That(await a.EvaluateAsync<bool>("window.__ADVENTURE_GAME__.changeRoom('next')"), Is.True);
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.room.id==='tour-depth'");
        await Walk(a, 320, 140); await Walk(a, 540, 325);
        await a.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "depth-room.png") });
    }

    [Test]
    public async Task Editor_DepthSettingsRoundtripAndPreview()
    {
        await using var server = CreateDemoServer(); await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl + "/editor.html?worldId=scroll");
        await page.WaitForFunctionAsync("document.querySelector('#input-id').value==='gallery'");
        await page.SelectOptionAsync("#room-select", "tour-depth");
        await page.WaitForFunctionAsync("document.querySelector('#input-id').value==='tour-depth'");
        await page.FillAsync("#input-depth-back-factor", "0.75"); await page.Locator("#input-depth-back-factor").BlurAsync();
        await page.ClickAsync("#btn-save");
        await page.WaitForFunctionAsync("document.querySelector('#status-banner').textContent.includes('atomar gespeichert')");
        await page.ReloadAsync(); await page.WaitForFunctionAsync("document.querySelector('#input-id').value==='gallery'");
        await page.SelectOptionAsync("#room-select", "tour-depth");
        await page.WaitForFunctionAsync("document.querySelector('#input-depth-back-factor').value==='0.75'");
        Assert.That(await page.IsCheckedAsync("#input-depth-enabled"), Is.True);
        await page.ClickAsync("#btn-toggle-mode");
        var response = page.WaitForResponseAsync(r => r.Url.Contains("preview-path") && r.Request.Method == "POST");
        var canvas = page.Locator("#editor-canvas"); var bounds = (await canvas.BoundingBoxAsync())!;
        await canvas.ClickAsync(new() { Position = new() { X = bounds.Width / 2, Y = bounds.Height * 140 / 360 } });
        Assert.That((await response).Ok, Is.True);
        await page.ScreenshotAsync(new() { Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "depth-editor.png") });
    }

    [Test]
    public async Task Renderer_InterpolatesContinuouslyAndClampsAtRoomFootpoint()
    {
        await using var server = new TestServerFixture();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        var heights = await page.EvaluateAsync<double[]>("""
            async () => {
                const { actorHeightAt } = await import('/js/rendering/depth-scale.js');
                const { CharacterRenderer } = await import('/js/rendering/character-renderer.js');
                const room = {actorHeight:64, depthScale:{back:{y:100,factor:0.5},front:{y:300,factor:1.5}}};
                const canvas = document.createElement('canvas'); canvas.width=400; canvas.height=400;
                const ctx = canvas.getContext('2d');
                const renderer = new CharacterRenderer();
                const scales = [];
                const originalScale = ctx.scale.bind(ctx);
                ctx.scale = (x,y) => { scales.push(y*64); originalScale(x,y); };
                for (const y of [0,100,200,200.5,300,350]) {
                    renderer.drawCharacter(ctx,{position:{x:200,y},facing:'right',state:'idle',walkFrame:0,name:''},true,room,false);
                }
                if (actorHeightAt(undefined,200)!==64 || actorHeightAt({actorHeight:32},200)!==32) throw Error('Legacy size changed');
                return scales;
            }
            """);
        Assert.That(heights, Is.EqualTo(new[] { 32d, 32, 64, 64.16, 96, 96 }).Within(0.00001));
    }
}

using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

namespace AdventureChat.E2ETests;

public class ForegroundObjectTests
{
    [Test]
    public async Task TwoBrowsers_ReceiveObjectsAndCanWalkAcrossDepthLine()
    {
        await using var server = new TestServerFixture(foregroundWorld:true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless=true });
        var a=await browser.NewPageAsync(); var b=await browser.NewPageAsync();
        foreach(var page in new[]{a,b})
        {
            await page.GotoAsync(server.BaseUrl);
            await page.ClickAsync("#world-enter");
            await page.WaitForFunctionAsync("window.__ADVENTURE_GAME__?.connectionStatus==='connected'");
        }
        await a.WaitForFunctionAsync("window.__ADVENTURE_GAME__.remotePlayers.size===1");
        foreach(var page in new[]{a,b})
        {
            Assert.That(await page.EvaluateAsync<string>("window.__ADVENTURE_GAME__.room.objects[0].image"),Is.EqualTo("/world-assets/foreground/images/table.png"));
        }
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:230,y:260})");
        await a.WaitForFunctionAsync("Math.abs(window.__ADVENTURE_GAME__.character.position.y-260)<2");
        await b.WaitForFunctionAsync("Math.abs([...window.__ADVENTURE_GAME__.remotePlayers.values()][0].position.y-260)<2");
        await a.EvaluateAsync("window.__ADVENTURE_GAME__.setTarget({x:230,y:310})");
        await a.WaitForFunctionAsync("Math.abs(window.__ADVENTURE_GAME__.character.position.y-310)<2");
    }
    [Test]
    public async Task Renderer_TransparentGapDepthEqualityAndCameraUseRoomCoordinates()
    {
        await using var server = new TestServerFixture(foregroundWorld: true, multipleWorlds:true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(); await page.GotoAsync(server.BaseUrl);
        var otherImage = await page.EvaluateAsync<string>("(async()=> (await (await fetch('/api/worlds/other/rooms/hall')).json()).objects[0].image)()");
        Assert.That(otherImage,Is.EqualTo("/world-assets/other/images/table.png"));
        var samples = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const {RoomRenderer}=await import('/js/rendering/renderer.js');
              const data=await (await fetch('/api/worlds/foreground/rooms/hall')).json();
              const catalog=await (await fetch('/api/worlds/foreground/characters')).json();
              const canvas=document.createElement('canvas'); document.body.append(canvas);
              const results=[];
              for(const height of [32,64]) for(const y of [279,280,281]) for(const offset of [false,true]) {
                const room={...data,actorHeight:height,viewport:offset?{width:320,height:360,scrollMode:'horizontal'}:null};
                const renderer=new RoomRenderer(canvas,room);
                const char={id:'a',name:'A',position:{x:230,y},facing:'down',state:'idle',walkFrame:0,appearance:catalog.characters[0]};
                const other={...char,id:'b',position:{x:260,y}};
                renderer.render(char,[other]);
                await new Promise(r=>setTimeout(r,100));
                renderer.render(char,[other]);
                const ctx=canvas.getContext('2d'), x=renderer.camera.offset;
                results.push({height,y,offset,solid:[...ctx.getImageData(230-x,y-10,1,1).data],gap:[...ctx.getImageData(260-x,y-10,1,1).data]});
              }
              return results;
            }
            """);
        foreach (var sample in samples.EnumerateArray())
        {
            var solid = sample.GetProperty("solid").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
            var gap = sample.GetProperty("gap").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
            Assert.That(solid, Is.EqualTo(sample.GetProperty("y").GetInt32() < 280 ? new[]{240,20,40,255} : new[]{30,160,200,255}), sample.ToString());
            Assert.That(gap, Is.EqualTo(new[]{30,160,200,255}), sample.ToString());
        }
        var equalDepth = await page.EvaluateAsync<int[]>("""
            async () => {
              const {RoomRenderer}=await import('/js/rendering/renderer.js');
              const room=await (await fetch('/api/worlds/foreground/rooms/hall')).json();
              const tile=document.createElement('canvas'); tile.width=1;tile.height=1;
              tile.getContext('2d').fillStyle='#0000ff';tile.getContext('2d').fillRect(0,0,1,1);
              const object=room.objects[0];
              room.objects=[{...object,id:'z',image:tile.toDataURL()},{...object,id:'a'}];
              const canvas=document.createElement('canvas'), renderer=new RoomRenderer(canvas,room);
              const char={id:'x',name:'',position:{x:100,y:300},facing:'down',state:'idle',walkFrame:0};
              renderer.render(char); await new Promise(r=>setTimeout(r,100)); renderer.render(char);
              return [...canvas.getContext('2d').getImageData(230,250,1,1).data];
            }
            """);
        Assert.That(equalDepth,Is.EqualTo(new[]{0,0,255,255}));
    }

    [Test]
    public async Task Editor_ObjectsCanBeCreatedAssignedSavedReloadedAndDeleted()
    {
        await using var server = new TestServerFixture(authoring:true,foregroundWorld:true);
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page=await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl+"/editor.html?worldId=foreground");
        await page.WaitForFunctionAsync("window.__ROOM_EDITOR__?.getState().getRoom().id==='hall'");
        await page.SelectOptionAsync("#object-list","table");
        await page.EvaluateAsync("""
            () => {
              const canvas=document.querySelector('canvas'), r=canvas.getBoundingClientRect();
              const event=new MouseEvent('mousedown',{bubbles:true,button:0,altKey:true,clientX:r.left+230*r.width/640,clientY:r.top+250*r.height/360});
              canvas.dispatchEvent(event); window.dispatchEvent(new MouseEvent('mouseup'));
            }
            """);
        Assert.That(await page.EvaluateAsync<string>("window.__ROOM_EDITOR__.getState().getSelection().type"),Is.EqualTo("polygon"));
        await page.SelectOptionAsync("#object-list","table");
        await page.FillAsync("#object-depthY","290");
        await page.Locator("canvas").HoverAsync();
        Assert.That(await page.InputValueAsync("#object-depthY"), Is.EqualTo("290"));
        await page.Locator("#object-depthY").BlurAsync();
        Assert.That(await page.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"),Is.True);
        await page.EvaluateAsync("window.__ROOM_EDITOR__.loadRoom('hall')");
        await page.SelectOptionAsync("#object-list","table");
        Assert.That(await page.InputValueAsync("#object-depthY"),Is.EqualTo("290"));
        await page.ClickAsync("#object-add");
        await page.SetInputFilesAsync("#object-upload", new FilePayload {
            Name="uploaded-object.png", MimeType="image/png",
            Buffer=await File.ReadAllBytesAsync(Path.Combine(server.TempRoot,"world/assets/images/table.png"))
        });
        await page.WaitForFunctionAsync("document.querySelector('#object-image').value==='assets/images/uploaded-object.png'");
        Assert.That(await page.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"),Is.True);
        await page.ClickAsync("#object-delete");
        Assert.That(await page.EvaluateAsync<bool>("window.__ROOM_EDITOR__.saveRoom()"),Is.True);
        await page.EvaluateAsync("window.__ROOM_EDITOR__.loadRoom('hall')");
        Assert.That(await page.Locator("#object-list option").CountAsync(),Is.EqualTo(2));
        await page.SelectOptionAsync("#object-list","table");
        await page.ScreenshotAsync(new(){Path=Path.Combine(TestContext.CurrentContext.WorkDirectory,"foreground-editor.png")});
        await page.EvaluateAsync("window.__ROOM_EDITOR__.toggleMode()");
        await page.WaitForTimeoutAsync(200);
        await page.ScreenshotAsync(new(){Path=Path.Combine(TestContext.CurrentContext.WorkDirectory,"foreground-preview.png")});
    }
}

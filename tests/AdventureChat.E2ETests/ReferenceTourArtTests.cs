using Microsoft.Playwright;
using NUnit.Framework;
using System.Text.Json;

namespace AdventureChat.E2ETests;

public class ReferenceTourArtTests
{
    [Test]
    public async Task OverheadArt_KeepsFootpointAndReadableMotionAtMapScale()
    {
        await using var server = new TestServerFixture(authoring: true, externalWorld: true);
        var solution = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../"));
        var source = Path.Combine(solution, "worlds/reference");
        var target = Path.Combine(server.TempRoot, "world");
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);
        }
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        await using var measure = await page.EvaluateHandleAsync("""
            async () => {
                const {CharacterRenderer}=await import('/js/rendering/character-renderer.js');
                const catalog=await (await fetch('/api/worlds/reference/characters')).json();
                const room=await (await fetch('/api/worlds/reference/rooms/tour-map')).json();
                const canvas=document.createElement('canvas');canvas.width=128;canvas.height=180;
                const ctx=canvas.getContext('2d'),renderer=new CharacterRenderer(),results=[];
                // Keep this renderer alive while its real image requests complete.
                for(const appearance of catalog.characters)
                    renderer.drawCharacter(ctx,{position:{x:64,y:160},facing:'down',state:'idle',walkFrame:0,name:'',appearance},true,room,false);
                return () => {
                const contact=document.createElement('canvas');contact.id='tour-poses';contact.width=640;contact.height=288;
                const out=contact.getContext('2d');out.imageSmoothingEnabled=false;out.fillStyle='#dfe9eb';out.fillRect(0,0,640,288);
                let row=0;
                for(const appearance of catalog.characters) for(const facing of ['right','left','up','down']) {
                    const fingerprints=[];
                    for(let phase=0;phase<5;phase++) {
                        ctx.clearRect(0,0,128,180);ctx.imageSmoothingEnabled=false;
                        renderer.drawCharacter(ctx,{position:{x:64,y:160},facing,state:phase===0?'idle':'walk',walkFrame:Math.max(0,phase-1),name:'',appearance},true,room,false);
                        const pixels=ctx.getImageData(48,128,32,32).data;
                        let minY=32,maxY=-1,partialAlpha=0,ink=0;
                        for(let y=0;y<32;y++)for(let x=0;x<32;x++) {
                            const alpha=pixels[(y*32+x)*4+3];
                            if(alpha>0){minY=Math.min(minY,y);maxY=Math.max(maxY,y);ink++;}
                            if(alpha!==0&&alpha!==255)partialAlpha++;
                        }
                        fingerprints.push(Array.from(pixels).join(','));
                        results.push({id:appearance.id,facing,phase,minY,maxY,partialAlpha,ink});
                        out.drawImage(canvas,48,128,32,32,phase*128,row*36,32,32);
                    }
                    results.push({id:appearance.id,facing,idleMatchesContact:fingerprints[0]===fingerprints[1],distinctWalk:new Set(fingerprints.slice(1)).size});
                    row++;
                }
                document.body.replaceChildren(contact);return results;
                };
            }
            """);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var measurements = await measure.EvaluateAsync<JsonElement>("run => run()");
        foreach (var sample in measurements.EnumerateArray())
        {
            if (sample.TryGetProperty("phase", out _))
            {
                Assert.That(sample.GetProperty("minY").GetInt32(), Is.EqualTo(2), sample.ToString());
                Assert.That(sample.GetProperty("maxY").GetInt32(), Is.EqualTo(31), sample.ToString());
                Assert.That(sample.GetProperty("partialAlpha").GetInt32(), Is.Zero, sample.ToString());
                Assert.That(sample.GetProperty("ink").GetInt32(), Is.GreaterThan(60), sample.ToString());
            }
            else
            {
                Assert.That(sample.GetProperty("idleMatchesContact").GetBoolean(), Is.True, sample.ToString());
                Assert.That(sample.GetProperty("distinctWalk").GetInt32(), Is.GreaterThanOrEqualTo(3), sample.ToString());
            }
        }
        await page.Locator("#tour-poses").ScreenshotAsync(new()
        {
            Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "reference-tour-poses.png")
        });
    }
}

using Microsoft.Playwright;
using NUnit.Framework;

namespace AdventureChat.E2ETests;

public class EdgeSpeechBubbleTests
{
    [Test]
    public async Task OffscreenSpeaker_BubblePointsToEitherEdgeAndReturnsToFigure()
    {
        await using var server = new TestServerFixture();
        await server.StartAsync();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new(){Headless=true});
        var page = await browser.NewPageAsync();
        await page.GotoAsync(server.BaseUrl);
        var pixels = await page.EvaluateAsync<bool[]>("""
            async () => {
                const { SpeechBubbleRenderer } = await import('/js/rendering/speech-bubble-renderer.js');
                const renderer = new SpeechBubbleRenderer();
                const canvas = document.createElement('canvas'); canvas.width=640; canvas.height=360;
                const ctx = canvas.getContext('2d');
                const character = {position:{x:100,y:300},speechBubble:{text:'Hallo',startTime:100,duration:6000}};
                const white = (x,y) => { const p=ctx.getImageData(x,y,1,1).data; return p[0]>240 && p[1]>240 && p[2]>240 && p[3]>240; };
                const results=[];
                for (const x of [100,1500,960]) {
                    ctx.clearRect(0,0,640,360); ctx.save(); ctx.translate(-640,0);
                    character.position.x=x;
                    renderer.drawSpeechBubble(ctx,character,1280,200,64,640);
                    ctx.restore();
                    results.push(white(4,201),white(635,201),white(320,217));
                }
                ctx.clearRect(0,0,640,360);
                renderer.drawSpeechBubble(ctx,character,1280,6200,64,640);
                results.push([...ctx.getImageData(0,0,640,360).data].every(v=>v===0));
                return results;
            }
            """);
        Assert.That(pixels,Is.EqualTo(new[]{true,false,false,false,true,false,false,false,true,true}));
    }
}

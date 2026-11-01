using AdventureChat.Server.Services;

namespace AdventureChat.Server;

internal static class WorldAssetAccess
{
    public static void UseWorldAssets(this WebApplication app, WorldPackage world)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/assets/images", out var rest) && rest.HasValue)
            {
                try
                {
                    var relative = "assets/images/" + rest.Value!.TrimStart('/');
                    var file = WorldPackage.SafePath(world.Root, relative, true);
                    if (!string.Equals(Path.GetExtension(file), ".png", StringComparison.OrdinalIgnoreCase)) { context.Response.StatusCode = 404; return; }
                    context.Response.ContentType = "image/png";
                    context.Response.Headers.CacheControl = "no-store";
                    await context.Response.SendFileAsync(file);
                }
                catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException) { context.Response.StatusCode = 404; }
                return;
            }
            await next();
        });
    }
}

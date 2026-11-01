using System.Net;
using Microsoft.AspNetCore.Antiforgery;

namespace AdventureChat.Server;

internal static class AuthoringAccess
{
    public static void UseAuthoringAccess(this WebApplication app, bool enabled)
    {
        app.Use(async (context, next) =>
        {
            var page = context.Request.Path.Equals("/editor.html", StringComparison.OrdinalIgnoreCase);
            var api = context.Request.Path.StartsWithSegments("/api/editor");
            if (page || api)
            {
                var ip = context.Connection.RemoteIpAddress;
                if (!enabled || ip is null || !IPAddress.IsLoopback(ip) || !IsLocalHostAndOrigin(context))
                {
                    context.Response.StatusCode = page ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden;
                    return;
                }
                if (api && !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                {
                    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                    if (!await antiforgery.IsRequestValidAsync(context))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }
            }
            await next();
        });
    }

    private static bool IsLocalHostAndOrigin(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !(IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address))) return false;
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return true;
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(uri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }
}

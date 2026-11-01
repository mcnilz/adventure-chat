using System.Text.Json.Serialization;
using AdventureChat.Server.Hubs;
using AdventureChat.Server.Models;
using AdventureChat.Server.Services;

namespace AdventureChat.Server;

public static class AppFactory
{
    public static WebApplication CreateApp(string[] args, WebApplicationOptions? options = null)
    {
        var builder = options is not null 
            ? WebApplication.CreateBuilder(options) 
            : WebApplication.CreateBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(opt =>
        {
            opt.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
        });

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IPathfindingService, PathfindingService>();
        builder.Services.AddSingleton<IRoomService, RoomService>();
        builder.Services.AddSingleton<IRoomSessionManager, RoomSessionManager>();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        });

        app.MapHub<RoomHub>("/hub/room");

        app.MapGet("/api/rooms/{id}", (string id, IRoomService roomService) =>
        {
            var room = roomService.GetRoom(id);
            if (room is null)
            {
                return Results.NotFound(new { error = $"Room '{id}' not found." });
            }

            return Results.Ok(new
            {
                id = room.Id,
                name = room.Name,
                width = room.Width,
                height = room.Height,
                backgroundUrl = room.BackgroundUrl,
                spawnPoint = new { x = room.SpawnPoint.X, y = room.SpawnPoint.Y },
                geometry = new
                {
                    outerBoundary = room.Geometry.OuterBoundary.Vertices.Select(v => new { x = v.X, y = v.Y }),
                    obstacles = room.Geometry.Obstacles.Select(obs => obs.Vertices.Select(v => new { x = v.X, y = v.Y }))
                }
            });
        });

        app.MapPost("/api/rooms/{id}/path", (string id, PathRequestDto request, IRoomService roomService, IPathfindingService pathfinder) =>
        {
            var room = roomService.GetRoom(id);
            if (room is null)
            {
                return Results.NotFound(new { success = false, error = $"Room '{id}' not found." });
            }

            var start = new Point2D(request.Start.X, request.Start.Y);
            var target = new Point2D(request.Target.X, request.Target.Y);

            var result = pathfinder.CalculatePath(room.Geometry, start, target);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    error = result.Error ?? "Path calculation failed."
                });
            }

            return Results.Ok(new
            {
                success = true,
                waypoints = result.Waypoints!.Select(w => new { x = w.X, y = w.Y })
            });
        });

        return app;
    }
}

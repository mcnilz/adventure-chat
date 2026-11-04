using AdventureChat.Server.Services;
using AdventureChat.Server.Models;

namespace AdventureChat.Server;

internal static class WorldContentEndpoints
{
    public static void MapWorldContent(this WebApplication app, List<WorldPackage> worlds)
    {
        app.MapGet("/world-assets/{id}/images/{**path}", (string id, string? path, HttpContext context) =>
        {
            var selected = worlds.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            if (selected is null || string.IsNullOrEmpty(path) || !string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)) return Results.NotFound();
            try
            {
                var file = WorldPackage.SafePath(selected.Root, "assets/images/" + path, true);
                context.Response.Headers.CacheControl = "no-store";
                return Results.File(file, "image/png");
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException) { return Results.NotFound(); }
        });
        app.MapGet("/api/worlds", () => Results.Ok(worlds.Select(w => new { id = w.Id, name = w.Name })));
        app.MapGet("/api/worlds/{id}/characters", (string id) =>
        {
            var world = worlds.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            return world is null ? Results.NotFound() : Results.Ok(new
            {
                world.DefaultCharacterId,
                characters = world.Characters.Select(c => new { c.Id, c.Name, imageUrl = c.ImageUrl(world.Id), c.FrameWidth, c.FrameHeight })
            });
        });
        app.MapGet("/api/worlds/{id}/start", (string id) =>
        {
            var selected = worlds.Find(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
            return selected is null ? Results.NotFound() : Results.Ok(new { roomId = selected.StartRoom });
        });
        app.MapGet("/api/worlds/{worldId}/rooms/{id}", (string worldId, string id, WorldRuntimeCatalog catalog) =>
            RoomContent(catalog.Find(worldId)?.Rooms.GetRoom(id)));

    }
    internal static IResult RoomContent(RoomDefinition? room)
    {
        if (room is null)
        {
            return Results.NotFound(new { error = "Room not found." });
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
            },
            spawns = room.Spawns.Select(s => new { id = s.Id, position = new { x = s.Position.X, y = s.Position.Y } }),
            exits = room.Exits.Select(e => new
            {
                id = e.Id,
                position = new { x = e.Position.X, y = e.Position.Y },
                radius = e.Radius,
                bounds = e.Bounds != null ? new { x = e.Bounds.X, y = e.Bounds.Y, width = e.Bounds.Width, height = e.Bounds.Height } : null,
                label = e.Label,
                target = new { room = e.TargetRoomId, spawn = e.TargetSpawnId }
            }),
            notes = room.Notes
        });
    }

}

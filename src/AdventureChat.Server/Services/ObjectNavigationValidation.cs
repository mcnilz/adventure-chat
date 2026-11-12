using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class ObjectNavigationValidation
{
    public static void Validate(RoomYamlDto dto, bool supported, IPathfindingService paths, List<string> errors)
    {
        var states = (dto.Objects ?? []).SelectMany(o => o.Interaction?.States ?? []).ToList();
        if (!states.Any(s => s.Obstacles is not null || s.BlockedExits is not null)) return;
        if (!supported) { errors.Add("State obstacles and blockedExits require format 1.8.0."); return; }
        if (states.Sum(s => (s.Obstacles ?? []).Sum(p => p?.Points?.Count ?? 0)) > 256)
            errors.Add("At most 256 state obstacle vertices may be declared per room (all states combined).");
        foreach (var state in states)
        {
            if (state.Obstacles is { } polygons)
            {
                if (polygons.Count > 4 || polygons.Any(p => p is null) || polygons.Select(p => p.Id).Distinct().Count() != polygons.Count)
                { errors.Add($"State '{state.Id}': at most four obstacles with unique IDs required."); continue; }
                foreach (var polygon in polygons)
                {
                    if (polygon.Points is null || polygon.Points.Count is < 3 or > 16 || polygon.Points.Any(p => p is null))
                    { errors.Add($"State '{state.Id}': obstacle needs 3–16 vertices."); continue; }
                    RoomValidator.ValidatePolygon(polygon.Points, $"State '{state.Id}' obstacle '{polygon.Id}'", errors);
                    if (polygon.Points.Any(p => p.X < 0 || p.Y < 0 || p.X > dto.Size.Width || p.Y > dto.Size.Height))
                        errors.Add($"State '{state.Id}': obstacle must fit room coordinates.");
                    ValidateSimpleBoundary(polygon, state.Id, errors);
                }
            }
            if (state.BlockedExits is { } exits && (exits.Count > 32 || exits.Distinct(StringComparer.OrdinalIgnoreCase).Count() != exits.Count ||
                exits.Any(id => dto.Exits?.Any(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) != true)))
                errors.Add($"State '{state.Id}': blockedExits must reference distinct exits of this room (maximum 32).");
        }
        if (errors.Count != 0) return;
        var room = dto.ToRoomDefinition();
        var rooms = new PreviewRoomService([room]);
        var navigation = new ObjectNavigation(rooms, _ => []);
        var geometry = navigation.Geometry(room);
        foreach (var spawn in room.Spawns)
            if (!geometry.IsWalkable(spawn.Position)) errors.Add($"Spawn '{spawn.Id}' is blocked in the initial state.");
        foreach (var exit in room.Exits.Where(e => navigation.ExitEnabled(room, e)))
            if (!room.Spawns.Any(s => paths.CalculatePath(geometry, s.Position, exit.Position).Success))
                errors.Add($"Enabled exit '{exit.Id}' is unreachable in the initial state. The base floor must allow an open passage.");
    }

    private static void ValidateSimpleBoundary(RoomPolygonYamlDto polygon, string state, List<string> errors)
    {
        var points = polygon.Points.Select(p => new Point2D(p.X, p.Y)).ToList();
        for (var i = 0; i < points.Count; i++)
        for (var j = 0; j < points.Count; j++)
        {
            if (j == i || j == (i + 1) % points.Count) continue;
            if (RoomGeometry.DistanceToSegment(points[j], points[i], points[(i + 1) % points.Count]) <= 1e-6)
            { errors.Add($"State '{state}': obstacle boundary touches itself."); return; }
        }
    }
}

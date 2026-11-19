using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class ObjectNavigationValidation
{
    public static void Validate(RoomYamlDto dto, bool supported, IPathfindingService paths, List<string> errors)
    {
        var states = (dto.Objects ?? []).SelectMany(obj => (obj.Interaction?.States ?? [])
            .Select(state => (ObjectId: obj.Id, State: state))).ToList();
        if (!states.Any(item => item.State.Obstacles is not null || item.State.BlockedExits is not null)) return;
        if (!supported) { errors.Add("State obstacles and blockedExits require format 1.8.0."); return; }
        if (states.Sum(item => (item.State.Obstacles ?? []).Sum(p => p?.Points?.Count ?? 0)) > 256)
            errors.Add("At most 256 state obstacle vertices may be declared per room (all states combined).");
        foreach (var (objectId, state) in states)
        {
            var scope = $"Object '{objectId}', state '{state.Id}'";
            if (state.Obstacles is { } polygons)
            {
                if (polygons.Count > 4 || polygons.Any(p => p is null) || polygons.Select(p => p.Id).Distinct().Count() != polygons.Count)
                { errors.Add($"{scope}: at most four obstacles with unique IDs required."); continue; }
                foreach (var polygon in polygons)
                {
                    var polygonScope = $"{scope}, obstacle '{polygon.Id}'";
                    if (polygon.Points is null || polygon.Points.Count is < 3 or > 16 || polygon.Points.Any(p => p is null))
                    { errors.Add($"{polygonScope}: obstacle needs 3–16 vertices."); continue; }
                    RoomValidator.ValidatePolygon(polygon.Points, polygonScope, errors);
                    if (polygon.Points.Any(p => p.X < 0 || p.Y < 0 || p.X > dto.Size.Width || p.Y > dto.Size.Height))
                        errors.Add($"{polygonScope}: obstacle must fit room coordinates.");
                    ValidateSimpleBoundary(polygon, polygonScope, errors);
                }
            }
            if (state.BlockedExits is { } exits && (exits.Count > 32 || exits.Distinct(StringComparer.OrdinalIgnoreCase).Count() != exits.Count ||
                exits.Any(id => dto.Exits?.Any(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase)) != true)))
                errors.Add($"{scope}: blockedExits must reference distinct exits of this room (maximum 32).");
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

    private static void ValidateSimpleBoundary(RoomPolygonYamlDto polygon, string scope, List<string> errors)
    {
        var points = polygon.Points.Select(p => new Point2D(p.X, p.Y)).ToList();
        for (var i = 0; i < points.Count; i++)
        for (var j = 0; j < points.Count; j++)
        {
            if (j == i || j == (i + 1) % points.Count) continue;
            if (RoomGeometry.DistanceToSegment(points[j], points[i], points[(i + 1) % points.Count]) <= 1e-6)
            { errors.Add($"{scope}: obstacle boundary touches itself."); return; }
        }
    }
}

using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal static class NpcTourValidation
{
    internal static void ValidateRoom(RoomDefinition room, bool supported, List<string> errors)
    {
        foreach (var npc in (room.Npcs ?? []).Where(npc => npc is not null))
        {
            if (npc.FollowDistance is { } distance &&
                (!supported || npc.Tour is null || !double.IsFinite(distance) || distance is < 20 or > 96))
                errors.Add($"NPC '{npc.Id}': followDistance must be between 20 and 96 and requires a tour in format 1.14.0.");
            if (npc.Tour is not { } tour) continue;
            if (!supported)
                errors.Add($"NPC '{npc.Id}': tour requires world format 1.14.0.");
            else if (npc.Waypoints is not null)
                errors.Add($"NPC '{npc.Id}': a tour guide cannot also have waypoints.");
            else if (tour.Count is < 3 or > 32 || tour.Any(step => !ValidStep(step)) ||
                tour[0].Position is null || tour[^1].Position is null || !tour.Any(step => step.Text is not null))
                errors.Add($"NPC '{npc.Id}': tour must start and end at stations and include an explanation.");
        }
    }

    private static bool ValidStep(NpcTourStep? step) =>
        step is { Position: { } point, Exit: null } && point.IsFinite() &&
            (step.Text is null || (!string.IsNullOrWhiteSpace(step.Text) && step.Text.Length <= 300)) ||
        step is { Position: null, Text: null } && !string.IsNullOrWhiteSpace(step.Exit);

    internal static void ValidateWorld(IReadOnlyList<RoomYamlDto> dtos, IPathfindingService pathfinder, List<string> errors)
    {
        if (!dtos.Any(dto => dto.Npcs?.Any(npc => npc.Tour is not null) == true)) return;
        var rooms = dtos.Select(dto => dto.ToRoomDefinition()).ToDictionary(room => room.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var room in rooms.Values)
        foreach (var npc in (room.Npcs ?? []).Where(npc => npc.Tour is not null))
        {
            var error = ValidateRoute(room, npc, rooms, pathfinder);
            if (error is not null) errors.Add($"Room '{room.Id}', NPC '{npc.Id}': {error}");
        }
    }

    private static string? ValidateRoute(RoomDefinition home, NpcDefinition npc,
        IReadOnlyDictionary<string, RoomDefinition> rooms, IPathfindingService pathfinder)
    {
        var room = home;
        var from = npc.Position;
        var hasStationInAnotherRoom = false;
        foreach (var step in npc.Tour!)
        {
            if (step.Position is { } station)
            {
                if (!pathfinder.CalculatePath(room.Geometry, from, station).Success)
                    return $"tour station ({station.X}, {station.Y}) is unreachable in room '{room.Id}'.";
                hasStationInAnotherRoom |= room.Id != home.Id;
                from = station;
                continue;
            }

            var exit = room.Exits.FirstOrDefault(exit => string.Equals(exit.Id, step.Exit, StringComparison.OrdinalIgnoreCase));
            if (exit is null || !rooms.TryGetValue(exit.TargetRoomId, out var target) ||
                NpcGuideRouteHome.FindLanding(target, exit) is not { } landing)
                return $"tour exit '{step.Exit}' in room '{room.Id}' has no valid destination.";
            if (!pathfinder.CalculatePath(room.Geometry, from, exit.Position).Success)
                return $"tour exit '{exit.Id}' is unreachable in room '{room.Id}'.";
            if (NpcGuideRouteHome.Find(target, landing, home.Id, npc.Position, id => rooms.GetValueOrDefault(id), pathfinder) is null)
                return $"tour has no walkable return route from '{target.Id}' to '{home.Id}'.";
            room = target;
            from = landing;
        }
        return hasStationInAnotherRoom ? null : "tour requires a station in another room.";
    }
}

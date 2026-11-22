using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

internal sealed record NpcGuideRouteHome(Point2D Point, string? ExitId = null)
{
    private sealed record Arrival(RoomDefinition Room, Point2D Point, RoomExit? FirstExit = null);

    internal static NpcGuideRouteHome? Find(RoomDefinition start, Point2D position, string homeId, Point2D homePoint,
        Func<string, RoomDefinition?> getRoom, IPathfindingService pathfinder, ObjectNavigation? navigation = null)
    {
        var arrivals = new Queue<Arrival>();
        var visited = new HashSet<(string Room, Point2D Position)> { (start.Id, position) };
        arrivals.Enqueue(new(start, position));
        while (arrivals.TryDequeue(out var arrival))
        {
            var room = arrival.Room;
            var geometry = navigation?.Geometry(room) ?? room.Geometry;
            if (room.Id == homeId && pathfinder.CalculatePath(geometry, arrival.Point, homePoint).Success)
                return arrival.FirstExit is { } first ? new(first.Position, first.Id) : new(homePoint);

            foreach (var exit in room.Exits)
            {
                if (navigation?.ExitUsable(room, exit) == false ||
                    getRoom(exit.TargetRoomId) is not { } target ||
                    FindLanding(target, exit) is not { } landing ||
                    visited.Contains((target.Id, landing)) ||
                    !(navigation?.Geometry(target) ?? target.Geometry).IsWalkable(landing) ||
                    !pathfinder.CalculatePath(geometry, arrival.Point, exit.Position).Success)
                    continue;
                visited.Add((target.Id, landing));
                arrivals.Enqueue(new(target, landing, arrival.FirstExit ?? exit));
            }
        }
        return null;
    }

    internal static Point2D? FindLanding(RoomDefinition destination, RoomExit exit) =>
        destination.Spawns.FirstOrDefault(spawn =>
            string.Equals(spawn.Id, exit.TargetSpawnId, StringComparison.OrdinalIgnoreCase))?.Position;
}

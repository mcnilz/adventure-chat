using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

// State polygons use absolute room coordinates. No alpha collision or implicit transform.
internal sealed class ObjectNavigation(IRoomService rooms, Func<RoomDefinition, IReadOnlyList<ObjectStateDto>> states)
{
    public RoomGeometry Geometry(RoomDefinition room)
    {
        var polygons = CurrentStates(room).SelectMany(s => s.Obstacles ?? [])
            .Select(p => new Polygon(p.Points.Select(v => new Point2D(v.X, v.Y)).ToList(), ClosedBoundary: true)).ToList();
        return polygons.Count == 0 ? room.Geometry
            : new(room.Geometry.OuterBoundary, room.Geometry.Obstacles.Concat(polygons).ToList());
    }

    public bool ExitEnabled(RoomDefinition room, RoomExit exit) =>
        !CurrentStates(room).Any(s => s.BlockedExits?.Contains(exit.Id, StringComparer.OrdinalIgnoreCase) == true);

    public bool ExitUsable(RoomDefinition room, RoomExit exit)
    {
        var target = rooms.GetRoom(exit.TargetRoomId);
        var spawn = target?.Spawns.FirstOrDefault(s => string.Equals(s.Id, exit.TargetSpawnId, StringComparison.OrdinalIgnoreCase));
        return ExitEnabled(room, exit) && Geometry(room).IsWalkable(exit.Position) &&
            target is not null && spawn is not null && Geometry(target).IsWalkable(spawn.Position);
    }

    public RoomDefinition EffectiveRoom(RoomDefinition room) => room with
    {
        Geometry = Geometry(room),
        Exits = room.Exits.Where(e => ExitUsable(room, e)).ToList()
    };

    private IEnumerable<ObjectState> CurrentStates(RoomDefinition room)
    {
        var selected = states(room).ToDictionary(s => s.ObjectId, s => s.StateId);
        return (room.Objects ?? []).Where(o => o.Interaction is not null)
            .Select(o => o.Interaction!.States.Single(s => s.Id == selected.GetValueOrDefault(o.Id, o.Interaction.InitialState)));
    }
}

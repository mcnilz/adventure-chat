using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    internal void RestorePreview(IReadOnlyList<PreviewObjectStates> states) => _interactions.RestorePreview(states);
    internal IReadOnlyList<ObjectStateDto> PreviewStates(RoomDefinition room) => _interactions.States(room);

    private ObjectActionResult ExecuteObjectAction(PlayerSession actor, ObjectActionRequestDto request)
    {
        var now = _clock.GetUtcNow();
        var projected = _sessionsByConnection.Values.Select(p => ProjectMovement(p, now)).ToList();
        var navigationRooms = new List<string>();
        var result = _interactions.Execute(projected.Single(p => p.ConnectionId == actor.ConnectionId), request,
            (proposed, assignedRooms) =>
            {
                foreach (var room in _roomService.GetAllRooms())
                {
                    if (!assignedRooms.Contains(room.Id, StringComparer.OrdinalIgnoreCase) &&
                        !room.Exits.Any(e => assignedRooms.Contains(e.TargetRoomId, StringComparer.OrdinalIgnoreCase))) continue;
                    if (SameNavigation(room, proposed)) continue;
                    navigationRooms.Add(room.Id);
                    foreach (var player in projected.Where(p => p.RoomId == room.Id))
                    {
                        var after = proposed.Geometry(room);
                        if (!after.IsWalkable(player.Position)) return "Diese Änderung würde eine Figur überdecken.";
                        var beforeAnchors = Anchors(room, _navigation)
                            .Where(a => _pathfinder.CalculatePath(_navigation.Geometry(room), player.Position, a.Point).Success).ToList();
                        var afterAnchors = Anchors(room, proposed).ToDictionary(a => a.Id, a => a.Point);
                        if (!beforeAnchors.Any(a => afterAnchors.TryGetValue(a.Id, out var point) &&
                            _pathfinder.CalculatePath(after, player.Position, point).Success))
                            return "Diese Änderung würde eine Figur ohne erreichbaren Sicherheitsanker einschließen.";
                    }
                }
                return null;
            });
        if (!result.Success) return result;

        // Commit movement at the exact timestamp used for every safety check.
        foreach (var player in _sessionsByConnection.Values) AdvanceMovement(player, now);
        foreach (var player in _sessionsByConnection.Values.Where(p => navigationRooms.Contains(p.RoomId)))
            RepairRoute(player, now);

        // A later visual-only revision can overtake a previous navigation revision in transit.
        // Every event therefore subsumes the entire current room/navigation/movement state.
        var changes = new List<ObjectStatesChangedDto>();
        foreach (var roomId in result.Changes.Select(c => c.RoomId).Concat(navigationRooms).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _interactions.SetRevision(roomId, result.Changes[0].Revision);
            var room = _roomService.GetRoom(roomId)!;
            var dto = _navigation.EffectiveRoom(room).ToDto();
            var players = _sessionsByConnection.Values.Where(p => p.RoomId == roomId).ToList();
            var change = new ObjectStatesChangedDto(roomId, result.Changes[0].Revision, _interactions.States(room),
                dto.Geometry, dto.Exits, players.Select(p => p.ToDto()).ToList(),
                players.Where(p => p.NavigationStopped).Select(p => p.SessionId).ToList());
            changes.Add(change);
        }
        return result with { Changes = changes };
    }

    private bool SameNavigation(RoomDefinition room, ObjectNavigation proposed)
    {
        var before = _navigation.Geometry(room).Obstacles;
        var after = proposed.Geometry(room).Obstacles;
        return before.Count == after.Count && before.Zip(after).All(pair => pair.First.Vertices.SequenceEqual(pair.Second.Vertices)) &&
            room.Exits.All(e => _navigation.ExitUsable(room, e) == proposed.ExitUsable(room, e));
    }

    private IEnumerable<(string Id, Point2D Point)> Anchors(RoomDefinition room, ObjectNavigation navigation)
    {
        var geometry = navigation.Geometry(room);
        foreach (var spawn in room.Spawns.Where(s => geometry.IsWalkable(s.Position))) yield return ($"spawn:{spawn.Id}", spawn.Position);
        foreach (var exit in room.Exits.Where(e => navigation.ExitUsable(room, e))) yield return ($"exit:{exit.Id}", exit.Position);
    }

    private PlayerSession ProjectMovement(PlayerSession player, DateTimeOffset now)
    {
        var copy = new PlayerSession
        {
            ConnectionId = player.ConnectionId, SessionId = player.SessionId, RoomId = player.RoomId,
            Position = player.Position, State = player.State, Target = player.Target,
            Waypoints = player.Waypoints.ToList(), MovementUpdatedAt = player.MovementUpdatedAt
        };
        AdvanceMovement(copy, now);
        return copy;
    }

    private void RepairRoute(PlayerSession player, DateTimeOffset now)
    {
        if (player.Target is not { } target || player.State != "walk") return;
        var geometry = _navigation.Geometry(_roomService.GetRoom(player.RoomId)!);
        if (player.Waypoints.Zip(player.Waypoints.Skip(1)).All(pair => geometry.HasLineOfSight(pair.First, pair.Second))) return;
        var route = _pathfinder.CalculatePath(geometry, player.Position, target);
        player.MovementRevision++;
        player.MovementUpdatedAt = now;
        player.Waypoints = route.Success ? route.Waypoints!.ToList() : [];
        if (route.Success) return;
        player.Target = null;
        player.State = "idle";
        player.NavigationStopped = true;
    }
}

using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    internal void RestorePreview(IReadOnlyList<PreviewObjectStates> states) => _interactions.RestorePreview(states);
    internal IReadOnlyList<ObjectStateDto> PreviewStates(RoomDefinition room) => _interactions.States(room);

    private ObjectActionResult ExecuteObjectAction(PlayerSession actor, ObjectActionRequestDto request,
        Action? onApplied = null, Point2D? actionPoint = null)
    {
        var now = _clock.GetUtcNow();
        var npcNow = (double)now.ToUnixTimeMilliseconds();
        var projected = _sessionsByConnection.Values.Select(p => ProjectMovement(p, now)).ToList();
        var projectedActor = projected.Single(p => p.ConnectionId == actor.ConnectionId);
        if (actionPoint is { } point) projectedActor.Position = point;
        var navigationRooms = new List<string>();
        var result = _interactions.Execute(projectedActor, request,
            (proposed, assignedRooms) =>
            {
                foreach (var room in _roomService.GetAllRooms())
                {
                    if (!assignedRooms.Contains(room.Id, StringComparer.OrdinalIgnoreCase) &&
                        !room.Exits.Any(e => assignedRooms.Contains(e.TargetRoomId, StringComparer.OrdinalIgnoreCase))) continue;
                    if (SameNavigation(room, proposed)) continue;
                    navigationRooms.Add(room.Id);
                    var before = _navigation.Geometry(room);
                    var after = proposed.Geometry(room);
                    foreach (var patrol in UpdateNpcPatrols(room, npcNow))
                    {
                        if (!after.IsWalkable(patrol.Position)) return "Diese Änderung würde eine Figur überdecken.";
                        if (!KeepsSafetyAnchor(room, patrol.Position, before, after, proposed))
                            return "Diese Änderung würde eine Figur ohne erreichbaren Sicherheitsanker einschließen.";
                    }
                    var guides = TourGuides().Where(guide => guide.Actor.RoomId == room.Id)
                        .Select(guide => ProjectMovement(guide.Actor, now));
                    foreach (var player in projected.Where(p => p.RoomId == room.Id).Concat(guides))
                    {
                        if (!after.IsWalkable(player.Position)) return "Diese Änderung würde eine Figur überdecken.";
                        if (!KeepsSafetyAnchor(room, player.Position, before, after, proposed))
                            return "Diese Änderung würde eine Figur ohne erreichbaren Sicherheitsanker einschließen.";
                    }
                }
                return null;
            });
        if (!result.Success) return result;

        // Commit movement at the exact timestamp used for every safety check.
        foreach (var player in _sessionsByConnection.Values) AdvanceMovement(player, now);
        foreach (var player in _sessionsByConnection.Values.Where(p => navigationRooms.Contains(p.RoomId) &&
            !_npcTours.ContainsKey(p.ConnectionId)))
            RepairRoute(player, now);
        foreach (var roomId in navigationRooms)
        {
            var geometry = _navigation.Geometry(_roomService.GetRoom(roomId)!);
            foreach (var tour in _npcTours.Values.Where(tour => tour.Guide.Actor.RoomId == roomId))
                RepairTourOnNavigationChange(tour, geometry, now);
            foreach (var guide in _returningGuides.Values.Where(guide => guide.Actor.RoomId == roomId))
            {
                AdvanceMovement(guide.Actor, now);
                RepairRoute(guide.Actor, now);
                guide.RetryAt = now;
            }
            if (_npcPatrols.TryGetValue(roomId, out var patrols))
                foreach (var patrol in patrols) patrol.NavigationChanged(geometry);
        }
        onApplied?.Invoke();
        EndInvalidObjectAnimations();

        // A later visual-only revision can overtake a previous navigation revision in transit.
        // Every event therefore subsumes the entire current room/navigation/movement state.
        var changes = new List<ObjectStatesChangedDto>();
        foreach (var roomId in result.Changes.Select(c => c.RoomId).Concat(navigationRooms).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _interactions.SetRevision(roomId, result.Changes[0].Revision);
            changes.Add(CurrentObjectView(roomId));
        }
        return result with { Changes = changes };
    }

    private ObjectStatesChangedDto CurrentObjectView(string roomId)
    {
        var room = _roomService.GetRoom(roomId)!;
        var dto = _navigation.EffectiveRoom(room).ToDto();
        var players = _sessionsByConnection.Values.Where(p => p.RoomId == roomId).ToList();
        var now = _clock.GetUtcNow();
        return new ObjectStatesChangedDto(roomId, _interactions.Revision(roomId), _interactions.States(room),
            dto.Geometry, dto.Exits, players.Select(p => p.ToDto(now)).ToList(),
            players.Where(p => p.NavigationStopped).Select(p => p.SessionId).ToList(),
            _npcPatrols.ContainsKey(roomId) || TourGuides().Any(guide => guide.Actor.RoomId == roomId) ? new(roomId, ++_npcSample,
                NpcPresences(room, now.ToUnixTimeMilliseconds())!) : null);
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

    private bool KeepsSafetyAnchor(RoomDefinition room, Point2D position, RoomGeometry before, RoomGeometry after,
        ObjectNavigation proposed)
    {
        var afterAnchors = Anchors(room, proposed).ToDictionary(anchor => anchor.Id, anchor => anchor.Point);
        return Anchors(room, _navigation).Any(anchor => afterAnchors.TryGetValue(anchor.Id, out var point) &&
            _pathfinder.CalculatePath(before, position, anchor.Point).Success &&
            _pathfinder.CalculatePath(after, position, point).Success);
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
        var room = _roomService.GetRoom(player.RoomId)!;
        var geometry = _navigation.Geometry(room);
        if (RouteIsUsable(player.Waypoints, geometry)) return;
        var route = _pathfinder.CalculatePath(geometry, player.Position, target);
        player.MovementRevision++;
        player.MovementUpdatedAt = now;
        player.Waypoints = route.Success ? route.Waypoints!.ToList() : [];
        if (route.Success)
        {
            ExtendNpcApproach(player, room);
            if (player.AnimationApproach is { } approach)
                player.AnimationApproach = approach with { ArrivesAt = ArrivalAt(player, room) };
            return;
        }
        player.AnimationApproach = null;
        CancelNpcApproach(player.ConnectionId);
        player.Target = null;
        player.State = "idle";
        player.NavigationStopped = true;
    }

    private static bool RouteIsUsable(IReadOnlyList<Point2D> route, RoomGeometry geometry) =>
        route.All(geometry.IsWalkable) &&
        route.Zip(route.Skip(1)).All(pair => geometry.HasLineOfSight(pair.First, pair.Second));
}

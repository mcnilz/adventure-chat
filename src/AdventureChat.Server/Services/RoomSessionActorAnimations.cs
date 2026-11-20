using AdventureChat.Server.Models;

namespace AdventureChat.Server.Services;

public partial class RoomSessionManager
{
    private readonly List<(string ConnectionId, string RoomId, ObjectActionResult Result)> _actorActivityUpdates = [];
    private long _approachSequence;

    private ObjectActionResult StartObjectAction(PlayerSession actor, ObjectActionRequestDto request, bool previewComplete = false)
    {
        if (!string.Equals(request.RoomId, actor.RoomId, StringComparison.OrdinalIgnoreCase))
            return new(false, "Der Raum hat sich geändert. Bitte erneut versuchen.", []);
        var room = _roomService.GetRoom(actor.RoomId);
        var obj = room?.Objects?.FirstOrDefault(item => item.Id == request.ObjectId && item.Interaction is not null);
        if (actor.AnimationApproach is { } approach && approach.ObjectId == request.ObjectId && approach.ActionId == request.ActionId &&
            obj is not null && _interactions.State(actor.RoomId, obj) == approach.RequiredState)
            return new(true, null, [], Pending: true);
        actor.AnimationApproach = null;
        if (actor.ObjectAnimation is { } active)
        {
            if (active.ObjectId == request.ObjectId && active.ActionId == request.ActionId)
                return new(true, null, [], Pending: active.DurationMs is not null);
            if (!request.Approach) return new(false, "Beende zuerst die aktuelle Benutzung.", []);
        }

        var action = obj is null ? null : _interactions.FindAction(actor.RoomId, obj, request.ActionId);
        if (room is null || obj is null || action?.ActorAnimation is not { } animation)
        {
            if (actor.ObjectAnimation is not null) return new(false, "Beende zuerst die aktuelle Benutzung.", []);
            return ExecuteObjectAction(actor, request);
        }
        if (_interactions.Revision(room.Id) != request.ExpectedRevision)
            return new(false, "Der Objektzustand hat sich geändert. Bitte erneut versuchen.", []);

        var now = _clock.GetUtcNow();
        AdvanceMovement(actor, now);
        var point = ObjectInteractionRuntime.AnimationPoint(obj, animation);
        if (!_navigation.Geometry(room).IsWalkable(point))
            return new(false, "Der Bedienpunkt ist derzeit gesperrt.", []);
        if (!ObjectInteractionRuntime.AtAnimationPoint(actor.Position, point) || actor.State == "walk")
        {
            if (request.Approach)
            {
                var move = MovePlayer(actor, room, point, now);
                if (!move.Success)
                    return new(false, "Der Weg zum Objekt ist nicht erreichbar.", move.AnimationChange is { } change ? [change] : []);
                actor.AnimationApproach = new(obj.Id, action.Id, action.From, ArrivalAt(actor, room), ++_approachSequence);
                return new(true, null, [], Pending: true, Move: move);
            }
            return new(false, "Zu weit entfernt. Bitte zum Bedienpunkt gehen.", [], "too_far");
        }
        if (actor.ObjectAnimation is not null) return new(false, "Beende zuerst die aktuelle Benutzung.", []);
        if (animation.DurationMs is null && _sessionsByConnection.Values.Any(player =>
                player.RoomId == room.Id && player.Position == point && player.ObjectAnimation is { DurationMs: null }))
            return new(false, "Dieser Platz ist bereits belegt.", []);

        if (previewComplete && animation.DurationMs is not null) return ExecuteObjectAction(actor, request);
        var image = actor.CharacterId is { } id ? animation.Sprites?.GetValueOrDefault(id) : null;
        void SetAnimation(string requiredState)
        {
            actor.Position = point;
            actor.Direction = animation.Direction;
            actor.ObjectAnimation = new(obj.Id, action.Id, requiredState, image, now, animation.DurationMs);
            actor.MovementRevision++;
        }

        if (animation.DurationMs is null) return ExecuteObjectAction(actor, request, () => SetAnimation(action.To), point);

        SetAnimation(action.From);
        return new(true, null, [PublishAnimationView(room.Id)], Pending: true);
    }

    internal ObjectActionResult CompletePreviewObjectAction(string connectionId, ObjectActionRequestDto request)
    {
        lock (_lock)
        {
            return _sessionsByConnection.TryGetValue(connectionId, out var session)
                ? StartObjectAction(session, request, previewComplete: true)
                : new(false, "Sitzung nicht gefunden.", []);
        }
    }

    internal IReadOnlyList<(string ConnectionId, string RoomId, ObjectActionResult Result)> CollectActorActivityUpdates()
    {
        lock (_lock)
        {
            ProcessActorActivities();
            var results = _actorActivityUpdates.ToArray();
            _actorActivityUpdates.Clear();
            return results;
        }
    }

    private void ProcessActorActivities()
    {
        var now = _clock.GetUtcNow();
        var due = _sessionsByConnection.Values
            .Select(player => (Player: player, At: DueAt(player), Sequence: player.AnimationApproach?.Sequence ?? 0))
            .Where(activity => activity.At <= now)
            .OrderBy(activity => activity.At).ThenBy(activity => activity.Sequence).ToList();
        foreach (var (player, _, _) in due)
        {
            // An earlier action may have canceled this one or changed its route.
            if (DueAt(player) is not { } dueAt || dueAt > now) continue;
            var result = player.AnimationApproach is { } approach
                ? CompleteApproach(player, approach, now)
                : CompleteGesture(player, player.ObjectAnimation!);
            _actorActivityUpdates.Add((player.ConnectionId, player.RoomId, result));
        }
    }

    private static DateTimeOffset? DueAt(PlayerSession player) => player.AnimationApproach?.ArrivesAt ??
        (player.ObjectAnimation is { DurationMs: int duration } active ? active.StartedAt.AddMilliseconds(duration) : null);

    private ObjectActionResult CompleteApproach(PlayerSession player, ObjectAnimationApproach approach, DateTimeOffset now)
    {
        player.AnimationApproach = null;
        AdvanceMovement(player, now);
        var room = _roomService.GetRoom(player.RoomId);
        var obj = room?.Objects?.FirstOrDefault(item => item.Id == approach.ObjectId && item.Interaction is not null);
        if (room is null || obj is null || _interactions.State(room.Id, obj) != approach.RequiredState)
            return new(false, "Der Objektzustand hat sich geändert. Bitte erneut versuchen.", []);
        return StartObjectAction(player, new(room.Id, obj.Id, approach.ActionId, _interactions.Revision(room.Id)));
    }

    private ObjectActionResult CompleteGesture(PlayerSession player, ActiveObjectAnimation active)
    {
        EndObjectAnimation(player);
        var result = ExecuteObjectAction(player, new(player.RoomId, active.ObjectId, active.ActionId,
            _interactions.Revision(player.RoomId)));
        return result.Success ? result : result with { Changes = [PublishAnimationView(player.RoomId)] };
    }

    private static DateTimeOffset ArrivalAt(PlayerSession player, RoomDefinition room) =>
        player.MovementUpdatedAt.AddSeconds(player.Waypoints.Zip(player.Waypoints.Skip(1))
            .Sum(segment => segment.First.DistanceTo(segment.Second)) / room.WalkSpeed).AddTicks(1);

    public void CancelObjectApproach(string connectionId)
    {
        lock (_lock)
        {
            ProcessActorActivities();
            if (_sessionsByConnection.TryGetValue(connectionId, out var player)) player.AnimationApproach = null;
        }
    }

    private ObjectStatesChangedDto PublishAnimationView(string roomId)
    {
        var now = _clock.GetUtcNow();
        foreach (var player in _sessionsByConnection.Values.Where(player => player.RoomId == roomId))
            AdvanceMovement(player, now);
        _interactions.AdvanceRevision(roomId);
        return CurrentObjectView(roomId);
    }

    private void EndInvalidObjectAnimations()
    {
        foreach (var player in _sessionsByConnection.Values)
        {
            if (player.ObjectAnimation is not { } animation) continue;
            var room = _roomService.GetRoom(player.RoomId);
            var obj = room?.Objects?.FirstOrDefault(item => item.Id == animation.ObjectId && item.Interaction is not null);
            if (room is null || obj is null || _interactions.State(room.Id, obj) != animation.RequiredState)
                EndObjectAnimation(player);
        }
    }

    private static bool EndObjectAnimation(PlayerSession player)
    {
        if (player.ObjectAnimation is null) return false;
        player.ObjectAnimation = null;
        player.MovementRevision++;
        return true;
    }
}
